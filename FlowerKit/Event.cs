namespace FlowerKit;

using Core;

/// <summary>
/// A event object keep the event metadata.
/// </summary>
public record Event<T>
    where T : Event<T>
{
    public static dynamic Publish { get; set; } = new Publisher<T>();
}