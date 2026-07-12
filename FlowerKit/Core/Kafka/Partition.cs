using System;
using System.Threading;
using System.Collections.Generic;

namespace FlowerKit.Core.Kafka;

/// <summary>
/// An ordered, append-only log of messages. This is the unit of parallelism
/// and ordering in the broker: records inside a partition keep their order and
/// each one gets a monotonically increasing offset.
/// </summary>
public sealed class Partition(int index)
{
    readonly List<Message> log = [];
    readonly Lock _lock = new();

    /// <summary>
    /// The zero-based index of this partition inside its topic.
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// The offset that will be assigned to the next appended record, i.e. the
    /// current length of the log.
    /// </summary>
    public long LogEndOffset
    {
        get { lock (_lock) return log.Count; }
    }

    /// <summary>
    /// Appends a record to the log and returns the offset it was written to.
    /// </summary>
    public long Append(object? key, object? value, IReadOnlyDictionary<string, object?>? headers)
    {
        lock (_lock)
        {
            var offset = log.Count;
            log.Add(new Message(
                key,
                value,
                offset,
                Index,
                DateTime.UtcNow,
                headers ?? Message.NoHeaders
            ));
            return offset;
        }
    }

    /// <summary>
    /// Tries to read the record stored at <paramref name="offset"/>.
    /// Returns false when the offset is past the end of the log.
    /// </summary>
    public bool TryRead(long offset, out Message message)
    {
        lock (_lock)
        {
            if (offset < 0 || offset >= log.Count)
            {
                message = null!;
                return false;
            }

            message = log[(int)offset];
            return true;
        }
    }
}