namespace FlowerKit.Core.Executors;

/// <summary>
/// A engine executor implementation.
/// </summary>
public interface IExecutor
{
    void Publish(object ev);
    void Run();
}