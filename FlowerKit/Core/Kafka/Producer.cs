using System.Collections.Generic;

namespace FlowerKit.Core.Kafka;

/// <summary>
/// Publishes records to topics. Topics are created on demand, mirroring Kafka's
/// auto-topic-creation, which keeps quick tests concise.
/// </summary>
public sealed class Producer(FakeKafka broker)
{
    /// <summary>
    /// Sends a keyless record to a topic.
    /// </summary>
    public RecordMetadata Send(string topic, object? value)
        => Send(topic, null, value, null);

    /// <summary>
    /// Sends a keyed record to a topic.
    /// </summary>
    public RecordMetadata Send(string topic, object? key, object? value)
        => Send(topic, key, value, null);

    /// <summary>
    /// Sends a keyed record with headers to a topic.
    /// </summary>
    public RecordMetadata Send(
        string topic,
        object? key,
        object? value,
        IReadOnlyDictionary<string, object?>? headers)
    {
        var target = broker.GetOrCreateTopic(topic);
        var partition = target.PartitionFor(key);
        var offset = target[partition].Append(key, value, headers);
        return new RecordMetadata(topic, partition, offset);
    }
}
