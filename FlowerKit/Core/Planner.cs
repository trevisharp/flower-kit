using System;

namespace FlowerKit.Core;

using Planners;

/// <summary>
/// The main orchestrator for all published events.
/// </summary>
public static class Planner
{
    public static IPlanner PlannerImplementation { get; set; } = new DefaultPlanner();
    
    /// <summary>
    /// Send a new for the planner.
    /// </summary>
    public static void ReceiveEvent(object ev)
    {
        PlannerImplementation.OnReceiveEvent(ev);
    }

    /// <summary>
    /// Add a plan to run a flow when a event is published.
    /// </summary>
    public static void AddPlan(Flow flow, Type eventType)
    {
        PlannerImplementation.AddPlan(flow, eventType);
    }
}