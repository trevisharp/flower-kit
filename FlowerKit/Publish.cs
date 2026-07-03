namespace FlowerKit;

using Core;

/// <summary>
/// The global class to publish events
/// </summary>
public static class Publish<T>
{
    public static readonly dynamic Emit = new Publisher(typeof(T));
}