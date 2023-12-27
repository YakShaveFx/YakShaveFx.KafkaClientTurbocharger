using System.Runtime.CompilerServices;
using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer;

/// <summary>
/// Minimal (at least right now) set of options to configure the turbocharged consumer.
/// </summary>
public sealed class TurbochargedConsumerOptions
{
    private const ushort DefaultMaximumDegreeOfParallelism = 10;

    /// <summary>
    /// Optional name for the consumer. For informational/troubleshooting purposes.
    /// </summary>
    /// <remarks>
    /// If a name is not provided, the first entry in <see cref="Topics"/> will be used.
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Maximum number of records that can be processed in parallel. 
    /// </summary>
    public ushort MaximumDegreeOfParallelism { get; init; } = DefaultMaximumDegreeOfParallelism;

    /// <summary>
    /// The strategy used to determine how to parallelize the processing of records.
    /// </summary>
    public ParallelismStrategy ParallelismStrategy { get; init; } = ParallelismStrategy.PerKey;

    /// <summary>
    /// The topics to consume records from.
    /// </summary>
    public required IReadOnlyCollection<string> Topics { get; init; }

    /// <summary>
    /// The consumer group id.
    /// </summary>
    public required string GroupId { get; init; }

    /// <summary>
    /// The bootstrap servers to connect to Kafka.
    /// </summary>
    public required string BootstrapServers { get; init; }

    /// <summary>
    /// The auto offset reset strategy to use, to indicate if the consumer should start consuming from the earliest or latest offset.
    /// </summary>
    public AutoOffsetReset AutoOffsetReset { get; init; } = AutoOffsetReset.Earliest;
}

internal static class OptionsValidationExtensions
{
    internal static TurbochargedConsumerOptions EnsureValidity(
        this TurbochargedConsumerOptions options,
        [CallerArgumentExpression(nameof(options))]
        string? argumentName = default)
    {
        var errors = new List<string>();

        if (options.MaximumDegreeOfParallelism == 0)
        {
            errors.Add($"{nameof(TurbochargedConsumerOptions.MaximumDegreeOfParallelism)} must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(options.GroupId))
        {
            errors.Add($"{nameof(TurbochargedConsumerOptions.GroupId)}  must be provided");
        }

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            errors.Add($"{nameof(TurbochargedConsumerOptions.BootstrapServers)}  must be provided");
        }

        if (options.Topics is not { Count: > 0 })
        {
            errors.Add($"{nameof(TurbochargedConsumerOptions.Topics)} is empty. At least one topic must be provided");
        }

        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, errors), argumentName);
        }

        return options;
    }
}