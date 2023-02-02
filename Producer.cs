using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

public class Producer {
    //private static readonly ActivitySource ActivitySource = new(nameof(Producer));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    public MessageRack SendMessages(MessageRack messages, IConfiguration config)
    {
        var kafkaSettings = config.Get<KafkaSettings>();

        Console.WriteLine(kafkaSettings.BootstrapServers);

        ProducerConfig producerConfig = new ProducerConfig {
                BootstrapServers = "[insert Confluent Cloud Cluster URL]",
                SaslUsername = "[insert Confluent Cloud API key]",
                SaslPassword = "[insert Confluent Cloud API key secret]",
                SecurityProtocol = Confluent.Kafka.SecurityProtocol.SaslSsl,
                SaslMechanism = Confluent.Kafka.SaslMechanism.Plain,
                AllowAutoCreateTopics = true
            };

        string topic = messages.Topic;

        string[] users = { "Task1", "Task2", "Task3", "Task4", "Task5", "Task6" };
        string[] items = { "coca-cola", "alarm clock", "t-shirts", "gift card", "batteries" };

        var activityName = "SendMessages";

        ActivitySource aSource = new ActivitySource("kafka-otel-dotnet-example");
        using var activity = aSource.StartActivity(activityName, ActivityKind.Producer);

        using (var producer = new ProducerBuilder<string, string>(
            producerConfig.AsEnumerable()).Build())
        {
            var numProduced = 0;
            Random rnd = new Random();
            const int numMessages = 10;
            for (int i = 0; i < numMessages; ++i)
            {
                var user = users[rnd.Next(users.Length)];
                var item = items[rnd.Next(items.Length)];

                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }

                var header = new Headers();        

                // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), header, this.InjectTraceContextIntoHeaders);

                producer.Produce(topic, new Message<string, string> { Key = user, Value = item, Headers = header },
                    (deliveryReport) =>
                    {
                        if (deliveryReport.Error.Code != ErrorCode.NoError) {
                            Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");
                        }
                        else {
                            Console.WriteLine($"Produced event to topic {topic}: key = {user,-10} value = {item}");
                            numProduced += 1;
                        }
                    });
            }

            producer.Flush(TimeSpan.FromSeconds(10));
            Console.WriteLine($"{numProduced} messages were produced to topic {topic}");

            // kick off consumer automatically to empty the queue
            HttpClient client = new HttpClient();
            client.GetAsync("http://localhost:8083/consumer");
        }

        return messages;
    }

    private void InjectTraceContextIntoHeaders(Headers headers, string key, string value)
    {
        try
        {
            if (headers == null)
            {
                headers = new Headers(); 
            }

            // byte array conversion
            byte[] bValue = Encoding.UTF8.GetBytes(value);
            Console.WriteLine("provider context before header " + value);

            headers.Add(key, bValue);
        }
        catch (Exception ex)
        {
            throw ex;
            //logger.LogError(ex, "Failed to inject trace context.");
        }
    }
}