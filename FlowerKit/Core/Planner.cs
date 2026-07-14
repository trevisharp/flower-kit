using System;
using System.Collections.Generic;

namespace FlowerKit.Core;

/// <summary>
/// The main orchestrator for all published events.
/// </summary>
public class Planner
{
    public static Planner Current { get; private set; } = new Planner();
    public List<PlanedFlow> PlannedFlows { get; private set; } = [];

    /// <summary>
    /// Add a plan to run a flow when a event is published.
    /// </summary>
    public void AddToPlan(Flow flow, Type eventType)
    {
        ArgumentNullException.ThrowIfNull(flow, nameof(flow));
        ArgumentNullException.ThrowIfNull(eventType, nameof(eventType));

        PlannedFlows.Add(new(flow, eventType));
    }

    /// <summary>
    /// Clears every planned flow. Used by HotReload to discard the previous
    /// generation's flows before the recompiled workflows re-register theirs.
    /// </summary>
    public void Reset()
        => PlannedFlows.Clear();

    public record PlanedFlow(
        Flow Flow,
        Type EventType
    );
}