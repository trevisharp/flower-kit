using System.Collections.Generic;

namespace FlowerKit.Core.Graph;

/// <summary>
/// The event graph of an application: which events exist, which events trigger
/// which flows, and which events each flow can publish. Built at startup from the
/// user's source and later used to wire the architecture (Executor, Kafka).
/// </summary>
public sealed record FlowGraph(
    IReadOnlyCollection<string> Events,
    IReadOnlyCollection<FlowNode> Flows
);