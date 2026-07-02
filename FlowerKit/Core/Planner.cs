using System;

namespace FlowerKit.Core;

using Planners;

/// <summary>
/// The main orchestrator for all published events.
/// </summary>
public static class Planner
{
    public static bool Verbose { get; set; } = false;
    public static IPlanner PlannerImplementation { get; set; } = new DefaultPlanner();
    
    /// <summary>
    /// Send a new for the planner.
    /// </summary>
    public static void ReceiveEvent(object ev)
    {
        try
        {
            if (Verbose)
                Console.WriteLine($"Event received: {ev}");
            
            PlannerImplementation.OnReceiveEvent(ev);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on trying to plane the follow event: {ev}.\nError: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Add a plan to run a flow when a event is published.
    /// </summary>
    public static void AddPlan(Flow flow, Type eventType)
    {
        try
        {
            if (Verbose)
                Console.WriteLine($"Flow added to receive a {eventType} event.");
            
            PlannerImplementation.AddPlan(flow, eventType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on trying to add a new plan for {eventType} event.\nError: {ex}");
        }
    }
}