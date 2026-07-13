using System.Collections.Generic;

namespace FlowerKit.Core.Graph;

/// <summary>
/// A single flow in the graph: how it was defined (its <see cref="FlowAnchor"/>),
/// the event that triggers it (when the anchor is a trigger), and every event it
/// may publish, resolved in depth across the user's own methods.
/// </summary>
public sealed record FlowNode(
    FlowAnchor Anchor,
    string? TriggerEvent,
    IReadOnlyCollection<PublishEdge> Publishes
);