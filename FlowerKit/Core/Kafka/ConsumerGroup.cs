using System.Collections.Generic;
using System.Collections.Concurrent;

namespace FlowerKit.Core.Kafka;

/// <summary>
/// Coordinates a set of consumers that share the work of reading topics.
/// Each partition of a subscribed topic is owned by exactly one member, and
/// committed offsets are tracked per group so progress survives rebalances.
/// </summary>
public sealed class ConsumerGroup
{
    readonly FakeKafka broker;
    readonly List<Consumer> members = [];
    readonly ConcurrentDictionary<TopicPartition, long> committed = new();

    internal ConsumerGroup(string groupId, FakeKafka broker)
    {
        GroupId = groupId;
        this.broker = broker;
    }

    /// <summary>
    /// The group identifier.
    /// </summary>
    public string GroupId { get; }

    /// <summary>
    /// Creates a new consumer and adds it as a group member.
    /// </summary>
    internal Consumer Join()
    {
        var consumer = new Consumer(this, broker);
        lock (members)
            members.Add(consumer);
        return consumer;
    }

    /// <summary>
    /// Removes a member and rebalances its partitions to the rest.
    /// </summary>
    internal void Leave(Consumer consumer)
    {
        lock (members)
            members.Remove(consumer);
        Rebalance();
    }

    /// <summary>
    /// The committed offset for a partition, or the offset dictated by the
    /// reset policy when the group has never committed it.
    /// </summary>
    internal long GetCommitted(TopicPartition topicPartition, OffsetReset reset)
    {
        if (committed.TryGetValue(topicPartition, out var offset))
            return offset;

        return reset == OffsetReset.Earliest
            ? 0
            : broker.GetTopic(topicPartition.Topic)[topicPartition.Partition].LogEndOffset;
    }

    /// <summary>Records the next offset to be read for a partition.</summary>
    internal void Commit(TopicPartition topicPartition, long nextOffset)
        => committed[topicPartition] = nextOffset;

    /// <summary>
    /// Rewinds the committed offset of every partition of a topic, so members
    /// reprocess the log from <paramref name="offset"/> after the next rebalance
    /// or on newly assigned partitions.
    /// </summary>
    public void ResetOffsets(string topic, long offset = 0)
    {
        var target = broker.GetTopic(topic);
        for (var p = 0; p < target.PartitionCount; p++)
            committed[new TopicPartition(topic, p)] = offset;
    }

    /// <summary>
    /// Distributes the partitions of every subscribed topic across the members
    /// that subscribe to it, round-robin. Called whenever membership or
    /// subscriptions change.
    /// </summary>
    internal void Rebalance()
    {
        lock (members)
        {
            var assignments = new Dictionary<Consumer, List<TopicPartition>>();
            foreach (var member in members)
                assignments[member] = [];

            var topics = new HashSet<string>();
            foreach (var member in members)
                foreach (var topic in member.Subscriptions)
                    topics.Add(topic);

            foreach (var topic in topics)
            {
                var subscribers = members.FindAll(m => m.Subscriptions.Contains(topic));
                if (subscribers.Count == 0)
                    continue;

                var partitionCount = broker.GetOrCreateTopic(topic).PartitionCount;
                for (var p = 0; p < partitionCount; p++)
                {
                    var owner = subscribers[p % subscribers.Count];
                    assignments[owner].Add(new TopicPartition(topic, p));
                }
            }

            foreach (var member in members)
                member.Assign(assignments[member]);
        }
    }
}
