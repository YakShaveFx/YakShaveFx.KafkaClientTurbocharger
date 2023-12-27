using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.Pattern;
using Confluent.Kafka;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer.OffsetManagement;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Orchestration;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer;

internal sealed class TurbochargedKafkaConsumer : ITurbochargedKafkaConsumer
{
    private readonly TurbochargedConsumerOptions _options;
    private readonly Action<ConsumerConfig>? _configure;
    private readonly Action<ConsumerBuilder<byte[], byte[]>>? _configureBuilder;
    private readonly Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task> _handler;
    private readonly TimeProvider _timeProvider;

    internal TurbochargedKafkaConsumer(
        TurbochargedConsumerOptions options,
        Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task> handler,
        TimeProvider timeProvider,
        Action<ConsumerConfig>? configure,
        Action<ConsumerBuilder<byte[], byte[]>>? configureBuilder)
    {
        _options = options;
        _configure = configure;
        _configureBuilder = configureBuilder;
        _handler = handler;
        _timeProvider = timeProvider;
    }

    public Task RunAsync(CancellationToken ct)
    {
        var actorSystem = ActorSystem.Create(
            $"TurbochargedKafkaConsumer-{SanitizeSuffix(_options.Name ?? _options.Topics.First())}",
            ActorSystemSetup.Create(CreateSystemBootstrap()));
        actorSystem.ActorOf(ComposeActorHierarchy(ct), "Root");
        ct.Register(() => actorSystem.Terminate());
        return actorSystem.WhenTerminated;
    }

    private static BootstrapSetup CreateSystemBootstrap()
    {
        var bootstrap = BootstrapSetup.Create().WithConfig(
            ConfigurationFactory.ParseString(""""
                                             akka{
                                                loglevel = "ERROR"
                                                stdout-loglevel = "ERROR"
                                                log-config-on-start = on
                                                # when there are issues, we completely restart everything, so we're safe ignoring dead letter logs
                                                log-dead-letters = off
                                                actor{
                                                    debug{
                                                        receive = off
                                                        autoreceive = off
                                                        lifecycle = on
                                                        event-stream = off
                                                        unhandled = off
                                                    }
                                                }
                                             }
                                             client-proxy-mailbox {
                                                mailbox-type = "YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying.ClientProxyMailbox, YakShaveFx.KafkaClientTurbocharger.Core"
                                             }
                                             """"
            ));
        return bootstrap;
    }

    /// <summary>
    /// Keeps any letters, numbers, and dashes, and replaces everything else with a dash.
    /// It also removes any trailing dashes.
    /// </summary>
    /// <param name="name">The name to clean up.</param>
    /// <returns>The cleaned up name.</returns>
    private string SanitizeSuffix(string name)
    {
        var regex = new Regex("[^a-zA-Z0-9-]");
        var sanitized = regex.Replace(name, "-");
        sanitized = sanitized.Trim('-');
        return sanitized;
    }

    // this is the composition root of the turbocharged consumer, where all the dependencies are wired up
    // avoiding hard dependency on a DI container
    private Props ComposeActorHierarchy(CancellationToken ct)
    {
        var builder = InitializeConsumerBuilder();
        var confluentConsumerFactory = () => builder.Build();

        var clientProxyProps = Props.Create(() => new ClientProxy(
                confluentConsumerFactory,
                _options.Topics,
                _timeProvider))
            .WithMailbox("client-proxy-mailbox");

        var offsetControllerProps = Props.Create(() => new OffsetController());
        var runnerProps = Props.Create(() => new Runner(_handler, ct));
        var parallelismCoordinatorProps
            = Props.Create(() => new ParallelismCoordinator(
                _options.MaximumDegreeOfParallelism,
                _options.ParallelismStrategy,
                runnerProps));


        // var runnerCoordinatorProps =
        //     Props.Create(() => new RunnerCoordinator(runnerProps, _options.MaximumDegreeOfParallelism));

        var consumerOrchestratorProps = Props.Create(() => new ConsumerOrchestrator(
            clientProxyProps,
            offsetControllerProps,
            parallelismCoordinatorProps));

        /*
         * Using the BackoffSupervisor, is important for two reasons:
         * - The most obvious one, is that it will not immediately restart the actor if it fails, but will wait for a backoff period
         * - The much less obvious one, is that given it's not a traditional restart, it actually stops the actor for the backoff period,
         *   which means that the mailbox is cleared, and we're back to a clean state, as it's the goal here
         *  - If the BackoffSupervisor didn't work like this, we'd need to somehow clear the mailbox
         *
         * Alternatively, we could implement a discard behavior when restarting an actor.
         * This would be the preferred approach, as it's more explicit. However, it would be more complex, so for now, sticking with the BackoffSupervisor.
         * For reference, here's the code that would be needed to implement the discard behavior: https://stackoverflow.com/a/77698552/4923902
         */

        return BackoffSupervisor.Props(
            Backoff.OnFailure(
                consumerOrchestratorProps,
                childName: $"{nameof(ConsumerOrchestrator)}",
                minBackoff: TimeSpan.FromSeconds(5),
                maxBackoff: TimeSpan.FromMinutes(5),
                randomFactor: 0.2,
                -1)); // TODO: review is retry forever, or eventually blow everything up
    }

    private ConsumerBuilder<byte[], byte[]> InitializeConsumerBuilder()
    {
        var config = new ConsumerConfig();
        _configure?.Invoke(config);
        config.BootstrapServers = _options.BootstrapServers;
        config.GroupId = _options.GroupId;
        config.AutoOffsetReset = _options.AutoOffsetReset;
        config.EnableAutoCommit = false;

        var builder = new ConsumerBuilder<byte[], byte[]>(config);
        _configureBuilder?.Invoke(builder);
        return builder;
    }
}