using System;
using System.Collections.Generic;

namespace FlowerKit;

/// <summary>
/// Manual Kafka configuration, per environment. Each named environment
/// (<see cref="Production"/>, <see cref="Staging"/>, <see cref="Development"/>,
/// <see cref="Test"/>) has its own instance, so settings applied to one do not
/// leak into another; <see cref="Current"/> resolves to the instance matching
/// <see cref="Runtime.Environment"/>.
/// </summary>
public class KafkaConfig
{
    static readonly Dictionary<string, KafkaConfig> instances =
        new(StringComparer.OrdinalIgnoreCase);

    public static KafkaConfig Production => For(Environments.Production);
    public static KafkaConfig Staging => For(Environments.Staging);
    public static KafkaConfig Development => For(Environments.Development);
    public static KafkaConfig Test => For(Environments.Test);

    /// <summary>
    /// The configuration for the current <see cref="Runtime.Environment"/>.
    /// </summary>
    public static KafkaConfig Current => For(Runtime.Environment);

    /// <summary>
    /// Gets (creating if absent) the configuration for a named environment.
    /// </summary>
    public static KafkaConfig For(string environment)
    {
        if (!instances.TryGetValue(environment, out var config))
        {
            config = new KafkaConfig();
            instances[environment] = config;
        }

        return config;
    }

    readonly Dictionary<string, int> partitionsByWorkflow = [];

    /// <summary>
    /// The partition count used for a workflow's topic when not explicitly set
    /// via <see cref="Set{TWorkflow}"/>.
    /// </summary>
    public int DefaultPartitions { get; private set; } = 1;

    /// <summary>
    /// How long records are retained. Stored for future use; the local broker
    /// keeps the full log, and enforcement belongs to a real Kafka broker.
    /// </summary>
    public TimeSpan? Retention { get; private set; }

    /// <summary>
    /// Sets the partition count for the topic backing <typeparamref name="TWorkflow"/>.
    /// </summary>
    public KafkaConfig Set<TWorkflow>(int partitions)
        where TWorkflow : Workflow
    {
        partitionsByWorkflow[typeof(TWorkflow).Name] = partitions;
        return this;
    }

    /// <summary>
    /// Sets the partition count used for workflows without an explicit
    /// <see cref="Set{TWorkflow}"/> entry.
    /// </summary>
    public KafkaConfig SetDefaultPartitions(int partitions)
    {
        DefaultPartitions = partitions;
        return this;
    }

    /// <summary>
    /// Sets the record retention window.
    /// </summary>
    public KafkaConfig SetRetention(TimeSpan retention)
    {
        Retention = retention;
        return this;
    }

    /// <summary>
    /// The partition count to use for a workflow's topic, by type name.
    /// </summary>
    public int PartitionsFor(string workflowName)
        => partitionsByWorkflow.TryGetValue(workflowName, out var partitions)
            ? partitions
            : DefaultPartitions;
}