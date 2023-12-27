using Confluent.Kafka;
using Confluent.Kafka.Admin;

var topicName = "test-topic";
var bootstrapServers = "localhost:9092";

using var producer = new ProducerBuilder<string, string>(
        new ProducerConfig
        {
            BootstrapServers = bootstrapServers
        })
    .SetKeySerializer(Serializers.Utf8)
    .SetValueSerializer(Serializers.Utf8)
    .Build();

await CreateTopicAsync(topicName, bootstrapServers);

var possibleKeys = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid().ToString()).ToArray();

const int numberOfMessagesToPublish = 1000;
for (var i = 0; i < numberOfMessagesToPublish; i++)
{
    producer.Produce(
        topicName,
        new Message<string, string>
        {
            Key = possibleKeys[Random.Shared.Next(0, possibleKeys.Length)],
            Value = Guid.NewGuid().ToString()
        });
    
    //await Task.Delay(TimeSpan.FromMilliseconds(50));
}

producer.Flush();

static async Task CreateTopicAsync(string topicName, string bootstrapServers)
{
    try
    {
        using var adminClient = new AdminClientBuilder(
                new AdminClientConfig
                {
                    BootstrapServers = bootstrapServers
                })
            .Build();

        await adminClient.CreateTopicsAsync(new TopicSpecification[]
        {
            new() { Name = topicName, ReplicationFactor = 1, NumPartitions = 10 }
        });
    }
    catch (CreateTopicsException)
    {
        // already exists, let's go
    }
}