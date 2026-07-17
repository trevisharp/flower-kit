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
    /// Whether new flows can still be added to the plan. Set by <see cref="Freeze"/>
    /// once startup planning is done, so re-instantiating a workflow per consumed
    /// event (for DI) doesn't keep appending duplicate plans.
    /// </summary>
    public bool Frozen { get; private set; }

    /// <summary>
    /// Add a plan to run a flow when a event is published. A no-op once
    /// <see cref="Frozen"/>, since planning only happens once at startup (or
    /// once per HotReload generation).
    /// </summary>
    public void AddToPlan(Flow flow, Type eventType)
    {
        ArgumentNullException.ThrowIfNull(flow, nameof(flow));
        ArgumentNullException.ThrowIfNull(eventType, nameof(eventType));

        if (Frozen)
            return;

        PlannedFlows.Add(new(flow, eventType));
    }

    /// <summary>
    /// Stops accepting new plans. Called once startup (or HotReload) planning
    /// has built the dispatch map, so later per-event workflow instances
    /// (created for DI) don't re-register their flows.
    /// </summary>
    public void Freeze()
        => Frozen = true;

    /// <summary>
    /// Clears every planned flow and unfreezes. Used by HotReload to discard
    /// the previous generation's flows before the recompiled workflows
    /// re-register theirs.
    /// </summary>
    public void Reset()
    {
        PlannedFlows.Clear();
        Frozen = false;
    }

    public record PlanedFlow(
        Flow Flow,
        Type EventType
    );
}