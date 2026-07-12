namespace FlowerKit.Core.Kafka;

/// <summary>
/// Identifies a single partition inside a topic.
/// </summary>
public readonly record struct TopicPartition(string Topic, int Partition)
{
    public override string ToString() => $"{Topic}-{Partition}";
}