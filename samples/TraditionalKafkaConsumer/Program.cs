using Confluent.Kafka;
using Spectre.Console;

const int messagesToHandle = 1000;

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
        var task = ctx.AddTask("Consuming messages (sequentially)",
            new ProgressTaskSettings
            {
                AutoStart = false,
                MaxValue = messagesToHandle
            });

        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group-" + Guid.NewGuid(),
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<byte[], byte[]>(config).Build();
        consumer.Subscribe("test-topic");

        var handledCount = 0;
        while (handledCount < messagesToHandle)
        {
            _ = consumer.Consume(CancellationToken.None);
            
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            ++handledCount;
            if (handledCount == 1)
            {
                task.StartTask();
            }

            task.Increment(1);
        }

        task.StopTask();
    });