namespace FlowerKit.Core.Kafka;

/// <summary>
/// The acknowledgement returned to a producer after a successful send,
/// pointing to where the record was stored.
/// </summary>
public readonly record struct RecordMetadata(string Topic, int Partition, long Offset)
{
    public TopicPartition TopicPartition => new(Topic, Partition);
}