using System;
using System.Collections.Generic;

namespace FlowerKit.Core.Planners;

/// <summary>
/// The default planner used by FlowerKit.
/// </summary>
public class DefaultPlanner : IPlanner
{
    Dictionary<string, Flow> flowMap = [];

    public void AddPlan(Flow flow, Type eventType)
    {
        flowMap.Add(eventType.ToString(), flow);    
    }

    public void OnReceiveEvent(object ev)
    {
        var flow = flowMap[ev.GetType().ToString()];
        flow.Run(null);
    }
}