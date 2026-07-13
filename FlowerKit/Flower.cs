namespace FlowerKit;

/// <summary>
/// A object that can modify a build new flows.
/// </summary>
public abstract class Flower
{
    /// <summary>
    /// Build a new flow.
    /// </summary>
    public abstract Flow Apply(Flow flow);
}