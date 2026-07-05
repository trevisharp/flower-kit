using System;
using System.Collections.Generic;

namespace FlowerKit.Core;

/// <summary>
/// The main orchestrator for all published events.
/// </summary>
public class Planner
{
    public static Planner Current { get; private set; } = new Planner();

    readonly Dictionary<string, Flow> flowMap = [];
    readonly List<PlanedFlow> plannedFlows = [];

    /// <summary>
    /// Add a plan to run a flow when a event is published.
    /// </summary>
    public void AddToPlan(Flow flow, Type eventType)
    {
        ArgumentNullException.ThrowIfNull(flow, nameof(flow));
        ArgumentNullException.ThrowIfNull(eventType, nameof(eventType));
        
        plannedFlows.Add(new(flow, eventType));
    }

    record PlanedFlow(Flow Flow, Type EventType);
}