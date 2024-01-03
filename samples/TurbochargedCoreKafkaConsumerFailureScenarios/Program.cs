using System.Collections.Concurrent;
using System.Text;
using Spectre.Console;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer;

const int MessagesToHandle = 1000;
var parallelismStrategy = ParallelismStrategy.PerKey;
var timeProvider = TimeProvider.System;

int handledCount = 0;
int thrownExceptionCount = 0;
int totalExceptions = 0;
var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cancellationTokenSource.Cancel();

var handled = new ConcurrentDictionary<string, string>();

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
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(50, 250)), ct);

            var incrementedHandled = Interlocked.Increment(ref handledCount);

            if (incrementedHandled % 100 == 0)
            {
                AnsiConsole.MarkupLine("[green]Handled {0}[/]", incrementedHandled);
            }

            var messageValue = Encoding.UTF8.GetString(record.Message.Value);
            _ = handled.TryAdd(messageValue, messageValue);

            if (handledCount > 100 && Random.Shared.Next(1, 1000) % 400 == 0)
            {
                _ = Interlocked.Increment(ref thrownExceptionCount);
                throw new Exception("BOOM!");
            }

            if (MessagesToHandle == handled.Count)
            {
                await cancellationTokenSource.CancelAsync();
            }
        }
        catch (Exception)
        {
            _ = Interlocked.Increment(ref totalExceptions);
            throw;
        }
    });

await consumer.RunAsync(cancellationTokenSource.Token);

AnsiConsole.MarkupLine("[blue]Handled {0}[/]", handledCount);
AnsiConsole.MarkupLine("[blue]Distinct handled {0}[/]", handled.Count);
AnsiConsole.MarkupLine("[blue]Thrown exceptions {0}[/]", thrownExceptionCount);
AnsiConsole.MarkupLine("[blue]Total exceptions {0}[/]", totalExceptions);