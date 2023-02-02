using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace kafka_otel_test.Controllers;

[ApiController]
[Route("[controller]")]
public class ProducerController : ControllerBase
{
    private readonly ILogger<ProducerController> _logger;
    private readonly IConfiguration _config;

    public ProducerController(ILogger<ProducerController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [HttpGet(Name = "LaunchMessageRack")]
    public IEnumerable<MessageRack> Get()
    {
        var producer = new Producer();

        var messageRack = new MessageRack();

        var messageRacks = new List<MessageRack>();

        // Send in name of State to get Stations and Forecasts from Weather.gov under a single trace

        List<string> topicsToUse = new List<string>{"train"};
        foreach (var topic in topicsToUse)
        {
            messageRack.Topic = topic;
            var state = new Message();

            ActivitySource aSource = new ActivitySource("kafka-otel-dotnet-example");
            using var activity = aSource.StartActivity("Producer Controller");
            activity?.AddTag("topic",topic);
            
            messageRack = producer.SendMessages(messageRack, _config);
            messageRacks.Add(messageRack);
        }

        return messageRacks;
    }
}
