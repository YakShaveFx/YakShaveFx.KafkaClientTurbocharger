using Spectre.Console;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer;

const int messagesToHandle = 1000;
var parallelismStrategy = ParallelismStrategy.PerKey;
var timeProvider = TimeProvider.System;

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
                MaxValue = messagesToHandle
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
                async (record, ct) =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ct);

                    var incrementedHandled = Interlocked.Increment(ref handledCount);
                    if (incrementedHandled == 1)
                    {
                        task.StartTask();
                    }

                    task.Increment(1);

                    if (incrementedHandled == messagesToHandle)
                    {
                        await cancellationTokenSource.CancelAsync();
                    }
                });

            await consumer.RunAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // end!
        }

        task.StopTask();
    });