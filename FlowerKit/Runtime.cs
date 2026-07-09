using System;
using System.Runtime.CompilerServices;

namespace FlowerKit;

using Core.Executors;
using FlowerKit.Core.Startup;

/// <summary>
/// The main runtime aplication.
/// </summary>
public static class Runtime
{
    public static IExecutor CurrentExecutor { get; set; } = new LocalExecutor();

    /// <summary>
    /// Publish a new event.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Publish(object ev)
        => CurrentExecutor.Publish(ev);

    /// <summary>
    /// Start application.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run()
    {
        ArgumentNullException.ThrowIfNull(CurrentExecutor, nameof(CurrentExecutor));

        var codeAnalizer = new FlowCodeAnalyzer();
        codeAnalizer.Analize();

        CurrentExecutor.Run();
    }
}