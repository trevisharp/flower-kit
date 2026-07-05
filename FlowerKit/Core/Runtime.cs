using System;

namespace FlowerKit.Core;

using Executors;

/// <summary>
/// The main runtime aplication.
/// </summary>
public static class Runtime
{
    public static IExecutor CurrentExecutor { get; set; } = new LocalExecutor();

    /// <summary>
    /// Publish a new event.
    /// </summary>
    public static void Publish(object ev)
    {
        
    }

    /// <summary>
    /// Start application.
    /// </summary>
    public static void Run()
    {
        ArgumentNullException.ThrowIfNull(CurrentExecutor, nameof(CurrentExecutor));
    }
}