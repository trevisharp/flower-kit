using System.Collections.Generic;

namespace FlowerKit;

/// <summary>
/// A set of flows.
/// </summary>
public record Workflow(params IEnumerable<Flow> Flows);