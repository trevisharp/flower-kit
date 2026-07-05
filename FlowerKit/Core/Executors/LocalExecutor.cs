using System.Collections.Generic;

namespace FlowerKit.Core.Executors;

/// <summary>
/// A executor implementation that run all flows locally.
/// </summary>
public class LocalExecutor : IExecutor
{
    readonly Dictionary<string, Flow> flows = [];

    public void Publish(object ev)
    {
        var flow = flows[ev.GetType().Name];
        flow.Run(ev);
    }

    public void Run()
    {
        var planner = Planner.Current;

        foreach (var planned in planner.PlannedFlows)
        {
            flows.Add(
                planned.EventType.ToString(),
                planned.Flow
            );
        }
    }
}