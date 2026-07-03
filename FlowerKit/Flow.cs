using System;

namespace FlowerKit;

using FlowerKit.Core;
using Flows;

/// <summary>
/// A basic node of event processing.
/// </summary>
public abstract class Flow
{
    public abstract void Run(object ctx);

    public static Flow On<T>(Action<FlowContext<T>> func)
        where T : Event
    {
        var flow = new DelegateFlow<T>(func);
        Planner.AddPlan(flow, typeof(T));
        return flow;
    }
}