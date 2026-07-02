namespace FlowerKit;

/// <summary>
/// Represents the context of current executing flow.
/// </summary>
public class FlowContext<T>
    where T : Event<T>
{
    public required T Body { get; set; }
}