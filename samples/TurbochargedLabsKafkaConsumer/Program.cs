using System.Collections.Concurrent;
using System.Text;
using Spectre.Console;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer;
using YakShaveFx.KafkaClientTurbocharger.Labs.Consumer.Pipelines;

const int MessagesToHandle = 5000;
var parallelismStrategy = ParallelismStrategy.PerKey;
var timeProvider = TimeProvider.System;

var handledMessages = new ConcurrentBag<(string, string)>();
int handledCount = 0;
var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cancellationTokenSource.Cancel();

await AnsiConsole
    .Progress()
    .Columns(
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new ElapsedTimeColumn(),
        new PercentageColumn(),
        new SpinnerColumn())
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask($"Consuming messages (parallelism strategy: {parallelismStrategy})",
            new ProgressTaskSettings
            {
                AutoStart = false,
                MaxValue = MessagesToHandle
            });

        try
        {
            var consumerFactory = new TurbochargedKafkaConsumerFactory(timeProvider);
            var consumer = consumerFactory.Create(
                new TurbochargedConsumerOptions
                {
                    MaximumDegreeOfParallelism = 10,
                    Topics = new[] { "test-topic" },
                    BootstrapServers = "localhost:9092",
                    GroupId = "test-group-" + Guid.NewGuid(),
                    ParallelismStrategy = parallelismStrategy
                },
                PipelineBuilder.Create()
                    .WithInitialStep(static (r, _) => Task.FromResult((r.Message.Key, r.Message.Value)))
                    .WithStep(static (kvp, _) =>
                        Task.FromResult((Encoding.UTF8.GetString(kvp.Key), Encoding.UTF8.GetString(kvp.Value))))
                    .WithFinalStep(async (kvp, ct) =>
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), ct);

                        var incrementedHandled = Interlocked.Increment(ref handledCount);
                        if (incrementedHandled == 1)
                        {
                            task.StartTask();
                        }

                        task.Increment(1);

                        handledMessages.Add(kvp);

                        if (incrementedHandled == MessagesToHandle)
                        {
                            await cancellationTokenSource.CancelAsync();
                        }
                    }));

            await consumer.RunAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // end!
        }

        task.StopTask();
    });

var grouped = handledMessages
    .GroupBy(x => x.Item1)
    .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Item2).ToArray());

const int sampleSize = 5;
var root = new Tree($"Handled messages sample ({Math.Min(sampleSize, grouped.Count)}/{grouped.Count} keys)");
foreach (var group in grouped.Take(sampleSize))
{
    var groupNode = root.AddNode($"{group.Key} ({Math.Min(sampleSize, group.Value.Length)}/{group.Value.Length} values)");
    foreach (var message in group.Value.Take(sampleSize))
    {
        groupNode.AddNode(message);
    }
}
AnsiConsole.Write(root);