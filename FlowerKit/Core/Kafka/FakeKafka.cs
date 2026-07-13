using System.Collections.Generic;
using System.Collections.Concurrent;

namespace FlowerKit.Core.Kafka;

/// <summary>
/// A Kafka simulator for local runs. It is an in-memory, thread-safe broker that
/// speaks the common Kafka concepts — topics, partitions, producers, consumers,
/// consumer groups, committed offsets and replay — while staying fully agnostic
/// to any higher-level abstraction, so it can simulate Kafka anywhere.
/// </summary>
public sealed class FakeKafka
{
    readonly ConcurrentDictionary<string, Topic> topics = [];
    readonly ConcurrentDictionary<string, ConsumerGroup> groups = [];

    /// <summary>
    /// Partition count used when a topic is auto-created on demand.
    /// </summary>
    public int DefaultPartitions { get; set; } = 1;

    /// <summary>
    /// The names of every topic that currently exists.
    /// </summary>
    public IReadOnlyCollection<string> Topics => [ ..topics.Keys ];

    /// <summary>
    /// Creates a topic with an explicit partition count. Idempotent by name: if
    /// the topic already exists the existing one is returned unchanged.
    /// </summary>
    public Topic CreateTopic(string name, int partitions)
        => topics.GetOrAdd(name, n => new Topic(n, partitions));

    /// <summary>
    /// Gets an existing topic, creating it with <see cref="DefaultPartitions"/> if absent.
    /// </summary>
    public Topic GetOrCreateTopic(string name)
        => topics.GetOrAdd(name, n => new Topic(n, DefaultPartitions));

    /// <summary>
    /// Gets an existing topic or throws when it does not exist.
    /// </summary>
    public Topic GetTopic(string name)
        => topics.TryGetValue(name, out var topic)
            ? topic
            : throw new KeyNotFoundException($"The topic '{name}' does not exist.");

    /// <summary>
    /// Whether a topic exists.
    /// </summary>
    public bool TopicExists(string name) => topics.ContainsKey(name);

    /// <summary>
    /// Creates a producer bound to this broker.
    /// </summary>
    public Producer CreateProducer() => new(this);

    /// <summary>
    /// Creates a consumer that joins the given group, creating the group if it
    /// is the first member.
    /// </summary>
    public Consumer CreateConsumer(string groupId)
    {
        var group = groups.GetOrAdd(groupId, id => new ConsumerGroup(id, this));
        return group.Join();
    }

    /// <summary>
    /// Gets an existing consumer group or throws when it does not exist.
    /// </summary>
    public ConsumerGroup GetGroup(string groupId)
        => groups.TryGetValue(groupId, out var group)
            ? group
            : throw new KeyNotFoundException($"The consumer group '{groupId}' does not exist.");
}