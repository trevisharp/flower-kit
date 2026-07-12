using System.Collections.Generic;

namespace FlowerKit.Core.Kafka;

/// <summary>
/// Reads records from the partitions assigned to it by its consumer group.
/// A consumer keeps an in-memory read position per partition and only makes it
/// durable (visible to other members after a rebalance) when it commits.
/// </summary>
public sealed class Consumer
{
    readonly ConsumerGroup group;
    readonly FakeKafka broker;
    readonly HashSet<string> subscriptions = [];
    readonly Dictionary<TopicPartition, long> positions = [];
    IReadOnlyList<TopicPartition> assignment = [];

    internal Consumer(ConsumerGroup group, FakeKafka broker)
    {
        this.group = group;
        this.broker = broker;
    }

    /// <summary>
    /// Where this consumer starts a partition with no committed offset.
    /// </summary>
    public OffsetReset AutoOffsetReset { get; set; } = OffsetReset.Earliest;

    /// <summary>
    /// The group this consumer belongs to.
    /// </summary>
    public string GroupId => group.GroupId;

    /// <summary>
    /// The topics this consumer wants to read from.
    /// </summary>
    internal IReadOnlySet<string> Subscriptions => subscriptions;

    /// <summary>
    /// The partitions currently assigned to this consumer.
    /// </summary>
    public IReadOnlyList<TopicPartition> Assignment => assignment;

    /// <summary>
    /// Subscribes to one or more topics and triggers a group rebalance so
    /// partitions are (re)distributed among all members.
    /// </summary>
    public void Subscribe(params string[] topics)
    {
        foreach (var topic in topics)
        {
            subscriptions.Add(topic);
            broker.GetOrCreateTopic(topic);
        }
        group.Rebalance();
    }

    /// <summary>
    /// Leaves the group, releasing its partitions to the remaining members.
    /// </summary>
    public void Close() => group.Leave(this);

    /// <summary>
    /// Fetches up to <paramref name="maxRecords"/> records across the assigned
    /// partitions, advancing the read position. Returns an empty list when the
    /// consumer is caught up.
    /// </summary>
    public IReadOnlyList<Message> Poll(int maxRecords = int.MaxValue)
    {
        var batch = new List<Message>();

        foreach (var tp in assignment)
        {
            var position = positions[tp];
            var partition = broker.GetTopic(tp.Topic)[tp.Partition];

            while (batch.Count < maxRecords && partition.TryRead(position, out var message))
            {
                batch.Add(message);
                position++;
            }

            positions[tp] = position;
        }

        return batch;
    }

    /// <summary>
    /// Commits the current read position for every assigned partition.
    /// </summary>
    public void Commit()
    {
        foreach (var tp in assignment)
            group.Commit(tp, positions[tp]);
    }

    /// <summary>
    /// Moves the read position of a partition, e.g. back to offset 0 to replay
    /// the whole log for event sourcing.
    /// </summary>
    public void Seek(TopicPartition topicPartition, long offset)
        => positions[topicPartition] = offset;

    /// <summary>
    /// Applies a new partition assignment coming from a rebalance, initializing
    /// the read position of freshly assigned partitions from the group's
    /// committed offset (or the reset policy when none exists).
    /// </summary>
    internal void Assign(IReadOnlyList<TopicPartition> topicPartitions)
    {
        var next = new Dictionary<TopicPartition, long>();
        foreach (var tp in topicPartitions)
        {
            next[tp] = positions.TryGetValue(tp, out var kept)
                ? kept
                : group.GetCommitted(tp, AutoOffsetReset);
        }

        positions.Clear();
        foreach (var (tp, offset) in next)
            positions[tp] = offset;

        assignment = topicPartitions;
    }
}