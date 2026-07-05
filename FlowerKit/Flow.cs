using System;

namespace FlowerKit;

using Core;
using Core.Flows;

/// <summary>
/// A basic node of event processing.
/// </summary>
public abstract class Flow
{
    /// <summary>
    /// Run the flow based on a context object.
    /// </summary>
    public abstract void Run(object ctx);

    /// <summary>
    /// Add a flow to plan.
    /// </summary>
    public abstract void Plan();

    /// <summary>
    /// Create a new flow.
    /// </summary>
    public static Flow New<T>(Action<FlowContext<T>> func)
        where T : Event
    {
        var flow = new DelegateFlow<T>(func);
        return flow;
    }

    /// <summary>
    /// Create a flow and register the flow on planner.
    /// </summary>
    public static Flow On<T>(Action<FlowContext<T>> func)
        where T : Event
    {
        var flow = New(func);
        flow.Plan();
        return flow;
    }
}