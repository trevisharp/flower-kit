using System;

namespace FlowerKit.Core;

/// <summary>
/// The logic used by Planner class.
/// </summary>
public interface IPlanner
{
    void OnReceiveEvent(object ev);

    void AddPlan(Flow flow, Type eventType);
}