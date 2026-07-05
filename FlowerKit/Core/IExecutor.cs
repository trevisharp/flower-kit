namespace FlowerKit.Core;

/// <summary>
/// A engine executor implementation.
/// </summary>
public interface IExecutor
{
    void Publish(Event ev);
    void Run();
}