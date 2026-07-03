namespace FlowerKit;

using Core.EventOperators;

/// <summary>
/// A event object keep the event metadata.
/// </summary>
public record Event
{
    public static SpreadEvent operator ~(Event ev) => new(ev);
}