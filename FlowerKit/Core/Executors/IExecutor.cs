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
}