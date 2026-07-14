namespace FlowerKit.Core.Executors;

/// <summary>
/// A engine executor implementation.
/// </summary>
public interface IExecutor
{
    /// <summary>
    /// Publish a new event on executor engine.
    /// </summary>
    void Publish(object ev);

    /// <summary>
    /// Run the executor engine.
    /// </summary>
    void Run();

    /// <summary>
    /// Stop the executor engine, releasing any background resources (e.g.
    /// consumer threads). Used by HotReload before a new generation takes over.
    /// </summary>
    void Stop();
}