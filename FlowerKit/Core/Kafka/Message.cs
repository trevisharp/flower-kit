using System;
using System.Collections.Generic;

namespace FlowerKit.Core.Kafka;

/// <summary>
/// A single record stored in a partition log. Payloads are kept as
/// <see cref="object"/> so the broker stays agnostic to any serialization
/// or higher-level abstraction.
/// </summary>
public sealed record Message(
    object? Key,
    object? Value,
    long Offset,
    int Partition,
    DateTime Timestamp,
    IReadOnlyDictionary<string, object?> Headers
)
{
    /// <summary>An empty header set, shared to avoid allocations.</summary>
    public static readonly IReadOnlyDictionary<string, object?> NoHeaders = new Dictionary<string, object?>();
}
