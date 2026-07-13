namespace FlowerKit.Core.Graph;

/// <summary>
/// A possible publication a flow can make: the event type it may publish and
/// the publish operation used (e.g. <c>Emit</c>). The action is captured from
/// the member invoked on <see cref="Publish{T}"/>, so new publish operations are
/// picked up automatically.
/// </summary>
public readonly record struct PublishEdge(string EventType, string Action)
{
    public override string ToString() => $"{Action} {EventType}";
}