namespace FlowerKit.Core.EventOperators;

/// <summary>
/// Represents a spread operator event. 
/// </summary>
public class SpreadEvent(Event ev)
{
    public readonly Event Event = ev;
}