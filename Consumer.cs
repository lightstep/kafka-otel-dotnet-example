using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

public class Consumer {

    private static readonly ActivitySource ActivitySource = new(nameof(Consumer));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    
    public Consumer()
    {

    }
    
    public async Task<MessageRack> ReadTopic(MessageRack messages, IConfiguration config, ILogger logger)
    {
        CancellationTokenSource cts = new CancellationTokenSource();

        ConsumerConfig consumerConfig = new ConsumerConfig {
                BootstrapServers = "[insert Confluent Cloud Cluster URL]",
                SaslUsername = "[insert Confluent Cloud API key]",
                SaslPassword = "[insert Confluent Cloud API key secret]",
                SecurityProtocol = Confluent.Kafka.SecurityProtocol.SaslSsl,
                SaslMechanism = Confluent.Kafka.SaslMechanism.Plain,
                AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,
                GroupId = "kafka-otel-dotnet-example",
                EnablePartitionEof = true
            };

        var activityName = "ReadTopic";

        ActivitySource aSource = new ActivitySource("kafka-otel-dotnet-example");

        using var activity = aSource.StartActivity(activityName, ActivityKind.Consumer);

        var crs = new List<ConsumeResult<string, string>>();

        using (var consumer = new ConsumerBuilder<string, string>(
            consumerConfig.AsEnumerable()).Build())
        {
            consumer.Subscribe(messages.Topic);
            try {
                while (true) {

                    var cr = consumer.Consume(cts.Token); 

                    if (cr.IsPartitionEOF)
                    {
                        Console.WriteLine(
                            $"Reached end of topic {cr.Topic}, partition {cr.Partition}, offset {cr.Offset}.");
                        consumer.Close();
                        break;
                    }
                    crs.Add(cr);
                }
            }
            catch (Exception ex) {
                logger.LogError(ex.ToString());
            }
        }

        Console.WriteLine("crs has " + crs.Count() + " records");

        foreach (var cr in crs)
        {
            var message = new Message();

            message.Result = cr.Message;

            var parentContext = Propagator.Extract(default, cr.Message.Headers, this.ExtractTraceContextFromHeaders);
            Baggage.Current = parentContext.Baggage;

            message = DoWork(message, parentContext, messages.Topic);

            Console.WriteLine($"Consumed event from topic {messages.Topic} with key {cr.Message.Key,-10} and value {cr.Message.Value}");

        };

        return messages;
    }

    private Message DoWork(Message message, PropagationContext parentContext, string? topic)
    {
        // this is where work can be done and additional messages sent to continue workflows 
        ActivitySource aSource = new ActivitySource("kafka-otel-dotnet-example");
        using var activity = aSource.StartActivity("DoWork - " + message.Result?.Key, ActivityKind.Producer, parentContext.ActivityContext);
        activity?.AddTag("product.sold", message.Result?.Value);
        activity?.AddTag("topic", topic);

        // this is purely a section to simulate an error to illustrate use of setting status and adding events
        if(message.Result?.Value == "coca-cola")
        {
            try
            {
                throw new KafkaException(ErrorCode.Local_BadMsg);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Kafka - Bad Message");
                var error = ex.Message;
                activity?.AddEvent(new ActivityEvent(error));
            }
        }
        Console.WriteLine($"Consumed event from topic {topic} with key {message.Result?.Key,-10} and value {message.Result?.Value}");
        
        // work simulation in the pause and replace with 
        Thread.Sleep(100);

        return message; 
    }

    private IEnumerable<string> ExtractTraceContextFromHeaders(Headers headers, string key)
    {
        // translate the value of the context back out of the header to be used in OTEL SDK forward
        try
        {
            byte[] headerOut = headers[0].GetValueBytes();
            if (headerOut != null)
            {
                var bytes = headerOut as byte[];
                var bytesOut = Encoding.UTF8.GetString(bytes);
                //Console.WriteLine("consumer context from header " + bytesOut);
                return new[] { bytesOut };
            }
        }
        catch (Exception ex)
        {
            throw ex;
            //this.logger.LogError(ex, "Failed to extract trace context.");
        }

        return Enumerable.Empty<string>();
    }
}