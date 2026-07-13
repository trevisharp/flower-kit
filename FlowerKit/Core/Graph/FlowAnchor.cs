namespace FlowerKit.Core.Graph;

/// <summary>
/// Describes a static factory method on <see cref="Flow"/> that defines a flow
/// (e.g. <c>Flow.On&lt;T&gt;</c>, <c>Flow.New&lt;T&gt;</c>). Adding support for a
/// new anchor is a single entry in the analyzer's anchor list.
/// </summary>
/// <param name="Method">The method name on <see cref="Flow"/> (e.g. "On", "New").</param>
/// <param name="IsTrigger">
/// Whether this anchor registers the generic event as a trigger for the flow
/// (true for <c>On</c>), producing an event -> flow edge.
/// </param>
public sealed record FlowAnchor(string Method, bool IsTrigger);