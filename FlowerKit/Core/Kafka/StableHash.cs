using System.Text;

namespace FlowerKit.Core.Kafka;

/// <summary>
/// A deterministic hash used to map a record key to a partition.
/// Unlike <see cref="object.GetHashCode"/>, it is stable across runs so that
/// the same key always lands on the same partition, keeping simulations reproducible.
/// </summary>
public static class StableHash
{
    /// <summary>
    /// Computes a stable FNV-1a hash of a record key.
    /// </summary>
    public static uint Of(object key)
    {
        var bytes = key switch
        {
            byte[] raw => raw,
            string text => Encoding.UTF8.GetBytes(text),
            _ => Encoding.UTF8.GetBytes(key.ToString() ?? string.Empty)
        };

        uint hash = 2166136261;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= 16777619;
        }
        return hash;
    }
}
