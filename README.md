# kafka-otel-dotnet-example
An example project on how to carry propagation context through Kafka topics.

This project allows the user to add the necessary connection information to a Kafka instance on Confluent Cloud and establish a running system in minutes.

OpenTelemetry is used throughout and no specific Lightstep libraries.

The goal of this project is to simply illustrate the pattern of development to inject the PropagationContext (sometimes referred to as the "traceparent") into a Kafka message header in a Producer to be used in transport through a topic and then to be extracted in the Consumer for use in then continuing the span creation and have it ultimately create a continuous trace.  The choice of the original parent is up to the developer.  This example ties all messages to the Producer and thus in the resulting trace they will be linked.  This allows the trace viewer to see the flow.  In addition the pattern could be continued taking the "DoWork" function of the consumer and injecting the traceparent from that into the next message thus creating a traced workflow.  This is more art than science.  Use your imagination and creativity to craft the trace of your choice.

One thing that currently is not solved by this project is a synthetic ultimate parent trace that covers start to end.  This is a future endeavour.

Confluent.Kafka and OpenTelemetry are the two dependencies for this project along with Swashbuckle for Swagger.

## Structure
1. API for both Producer and Consumer controllers as a .NET 6 WebApi project
2. Producer.cs and Consumer.cs are the main logic for patterns
3. Currently the project is only an API.  Normally the Consumer is run as a WorkerService. In this example the producer kicks off the consumer simply for illustration. Highly recommended that a WorkerService be created for production use.

## Prep to use the project
1. Replace all [insert ***] values throughout the code.  These will include the Confluent Cloud connection and token values in Producer.cs and Consumer.cs and also the Lightstep ingest in Program.cs
2. View the messages in the "train" topic in Confluent.  Create the "train" topic prior to use if AllowAutoTopicCreation is not allowed.
3. View the traces in Lightstep after running.  The ConsoleExporter is included to allow you to see all instrumentation and console messages for debug.
4. Once the app is running you can reach Swagger at http://localhost:8083 to kick off the Producer. That is all that is needed.  This is a single run of ten (10) messages into the topic and the consumer will retrieve and process them.  Click again to repeat traffic.




