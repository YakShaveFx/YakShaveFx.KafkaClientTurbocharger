# YakShaveFx.KafkaClientTurbocharger

Proof of concept to create a library to encapsulate common Kafka client related functionality, that's not provided out of the box. The first (and only?) implemented feature is parallel processing of records.

The core is implemented using [Akka.NET](https://getakka.net), which made it relatively easy to implement the parallel processing feature.

Here's the tiniest of videos, showing the parallel processing in action:

https://github.com/YakShaveFx/YakShaveFx.KafkaClientTurbocharger/assets/3763454/dc12597e-8b89-41a7-824b-fd0cacee95bd

## Main things you'll find in this repository

- `src`
  - `YakShaveFx.KafkaClientTurbocharger.Core` - the core library, implementing the abstraction on top of Confluent's Kafka consumer
- `samples`
  - `docker-compose.yml` - a docker compose file to run a Kafka cluster locally
  - `TestPublisher` - a simple console application that publishes messages to a Kafka topic, to use in other samples
  - `TraditionalKafkaConsumer` - a simple console application that consumes messages from a Kafka topic, using the Confluent Kafka consumer
  - `TurbochargedCoreKafkaConsumer` - a simple console application that consumes messages from a Kafka topic, using the `YakShaveFx.KafkaClientTurbocharger.Core` library

Besides the above, there are also other tidbits laying around, that I used as I explored things.

## Will this ever be a real library?

Dunno ¯\_(ツ)_/¯

This proof of concept was motivated mainly by the parallel processing problem, which seemed something interesting to experiment with. I solved this, so regarding that, I'm happy with the result.

On my own personal time, it's unlikely that I'll continue to work on this, as the fun part is done, and I have more things to learn and explore.

If, however, I need something like this for work, and can't find an alternative that works for our use cases, then I might revisit this and build upon this core.
