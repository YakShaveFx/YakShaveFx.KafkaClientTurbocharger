using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying;

/*
 * using a priority mailbox, given fetch may block for a bit,
 * we don't want to leave commit messages in the mailbox for too long
 */

// ReSharper disable once UnusedType.Global - automagically discovered by Akka.NET via HOCON config
internal sealed class ClientProxyMailbox(Settings settings, Config config)
    : UnboundedPriorityMailbox(settings, config)
{
    protected override int PriorityGenerator(object message)
        => message switch
        {
            ClientProxyMessages.Commands.Commit => 0,
            ClientProxyMessages.Commands.ScheduledCommit => 1,
            _ => 2
        };
}