namespace FlowerKit.Core.Kafka;

/// <summary>
/// Defines where a consumer starts reading a partition when the group
/// has no committed offset for it yet.
/// </summary>
public enum OffsetReset
{
    /// <summary>Start from the beginning of the log (offset 0).</summary>
    Earliest,

    /// <summary>Start from the end of the log (only new records).</summary>
    Latest
}