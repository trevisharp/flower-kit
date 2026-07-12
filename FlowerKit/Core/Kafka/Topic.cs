using System;
using System.Collections.Generic;
using System.Threading;

namespace FlowerKit.Core.Kafka;

/// <summary>
/// A named stream of records split into a fixed number of partitions.
/// </summary>
public sealed class Topic
{
    readonly Partition[] partitions;
    int roundRobin = -1;

    public Topic(string name, int partitionCount)
    {
        if (partitionCount < 1)
            throw new ArgumentOutOfRangeException(nameof(partitionCount), "A topic needs at least one partition.");

        Name = name;
        partitions = new Partition[partitionCount];
        for (var i = 0; i < partitionCount; i++)
            partitions[i] = new Partition(i);
    }

    /// <summary>
    /// The topic name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// How many partitions this topic has.
    /// </summary>
    public int PartitionCount => partitions.Length;

    /// <summary>
    /// All partitions of this topic.
    /// </summary>
    public IReadOnlyList<Partition> Partitions => partitions;

    /// <summary>
    /// Gets a partition by index.
    /// </summary>
    public Partition this[int index] => partitions[index];

    /// <summary>
    /// Chooses the partition for a record. Keyed records are hashed so the same
    /// key always maps to the same partition; keyless records are spread
    /// round-robin across partitions.
    /// </summary>
    public int PartitionFor(object? key)
    {
        if (key is null)
            return (int)((uint)Interlocked.Increment(ref roundRobin) % (uint)PartitionCount);

        return (int)(StableHash.Of(key) % (uint)PartitionCount);
    }
}
