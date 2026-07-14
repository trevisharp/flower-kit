using System;
using System.Threading;
using System.Collections.Generic;

namespace FlowerKit.Core.Executors;

using Kafka;

/// <summary>
/// A executor implementation that runs all flows locally, on top of the local
/// Kafka simulator (<see cref="FakeKafka"/>). Each workflow becomes a topic and
/// a consumer group; standalone flows (declared outside any workflow) share a
/// reserved topic. In <see cref="Environments.Test"/>, publishing drains the
/// broker synchronously so a cascade of events finishes before <c>Publish</c>
/// returns, which keeps test assertions deterministic. In every other
/// environment, a background thread polls each consumer, closer to how a real
/// Kafka deployment behaves.
/// </summary>
public class LocalExecutor : IExecutor
{
    const string StandaloneWorkflow = "_standalone";
    const string FixedKey = "0";

    readonly FakeKafka broker = new();
    readonly Dictionary<string, Consumer> consumersByWorkflow = [];
    readonly Dictionary<(string Workflow, string EventType), List<Flow>> flowsByKey = [];
    readonly Dictionary<string, HashSet<string>> topicsByEventType = [];
    readonly List<Thread> pumpThreads = [];

    Producer? producer;
    bool draining;
    volatile bool running = true;

    public void Run()
    {
        BuildDispatchMap();

        if (Runtime.Graph is null)
            throw new Exception(
                "Local executor need a Graph configure Kafka simulator." +
                "This erros may occurs when the Runtime.CurrentExecutor" +
                "is as LocalExecutor but the enviroment is Production."
            );

        var workflowNames = new HashSet<string>(Runtime.Graph?.Workflows ?? []);
        foreach (var topics in topicsByEventType.Values)
            workflowNames.UnionWith(topics);

        producer = broker.CreateProducer();

        foreach (var name in workflowNames)
        {
            broker.CreateTopic(name, KafkaConfig.Current.PartitionsFor(name));

            var consumer = broker.CreateConsumer(groupId: name);
            consumer.Subscribe(name);
            consumersByWorkflow[name] = consumer;
        }

        if (Runtime.Environment != Environments.Test)
            StartBackgroundPumps();
    }

    /// <summary>
    /// Stops every background pump thread and waits for them to exit, so a
    /// flow from the previous HotReload generation cannot run concurrently with
    /// the new one.
    /// </summary>
    public void Stop()
    {
        running = false;
        foreach (var thread in pumpThreads)
            thread.Join(TimeSpan.FromSeconds(1));

        pumpThreads.Clear();
    }

    public void Publish(object ev)
    {
        var eventType = ev.GetType().Name;
        if (topicsByEventType.TryGetValue(eventType, out var topics))
            foreach (var topic in topics)
                producer!.Send(topic, FixedKey, ev);

        if (Runtime.Environment == Environments.Test)
            DrainSynchronously();
    }

    /// <summary>
    /// Groups every planned flow by the workflow that declared it (or the
    /// reserved standalone bucket) and by its trigger event type, and records
    /// which topics need to exist for each event type.
    /// </summary>
    void BuildDispatchMap()
    {
        var ownerByFlow = new Dictionary<Flow, string>();
        foreach (var (name, workflow) in Runtime.Workflows)
            foreach (var flow in workflow.Flows)
                ownerByFlow[flow] = name;

        foreach (var planned in Planner.Current.PlannedFlows)
        {
            var workflowName = ownerByFlow.TryGetValue(planned.Flow, out var owner)
                ? owner
                : StandaloneWorkflow;
            var eventTypeName = planned.EventType.Name;

            var key = (workflowName, eventTypeName);
            if (!flowsByKey.TryGetValue(key, out var flows))
                flowsByKey[key] = flows = [];
            flows.Add(planned.Flow);

            if (!topicsByEventType.TryGetValue(eventTypeName, out var topics))
                topicsByEventType[eventTypeName] = topics = [];
            topics.Add(workflowName);
        }
    }

    /// <summary>
    /// Drains every consumer until no partition yields a new record. Reentrant
    /// publishes made by a flow (from inside this drain) only enqueue their
    /// record; the outer, originating call keeps draining until the whole
    /// cascade settles.
    /// </summary>
    void DrainSynchronously()
    {
        if (draining)
            return;

        draining = true;
        try
        {
            var progressed = true;
            while (progressed)
            {
                progressed = false;
                foreach (var (workflow, consumer) in consumersByWorkflow)
                {
                    var batch = consumer.Poll();
                    if (batch.Count == 0)
                        continue;

                    progressed = true;
                    foreach (var message in batch)
                        Dispatch(workflow, message.Value!);
                    consumer.Commit();
                }
            }
        }
        finally
        {
            draining = false;
        }
    }

    void StartBackgroundPumps()
    {
        foreach (var (workflow, consumer) in consumersByWorkflow)
        {
            var thread = new Thread(() => PumpLoop(workflow, consumer))
            {
                IsBackground = true,
                Name = $"flowerkit-{workflow}"
            };
            pumpThreads.Add(thread);
            thread.Start();
        }
    }

    void PumpLoop(string workflow, Consumer consumer)
    {
        while (running)
        {
            var batch = consumer.Poll();
            if (batch.Count == 0)
            {
                Thread.Sleep(10);
                continue;
            }

            foreach (var message in batch)
                Dispatch(workflow, message.Value!);
            consumer.Commit();
        }
    }

    void Dispatch(string workflow, object ev)
    {
        var key = (workflow, ev.GetType().Name);
        if (!flowsByKey.TryGetValue(key, out var flows))
            return;

        foreach (var flow in flows)
            flow.Run(ev);
    }
}
