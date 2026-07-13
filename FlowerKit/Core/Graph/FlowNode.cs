using System.Collections.Generic;

namespace FlowerKit.Core.Graph;

/// <summary>
/// A single flow in the graph: how it was defined (its <see cref="FlowAnchor"/>),
/// the event that triggers it (when the anchor is a trigger), every event it
/// may publish, resolved in depth across the user's own methods, and the
/// <see cref="Workflow"/> record that declared it (null when declared outside
/// of any workflow).
/// </summary>
public sealed record FlowNode(
    FlowAnchor Anchor,
    string? TriggerEvent,
    IReadOnlyCollection<PublishEdge> Publishes,
    string? Workflow
);