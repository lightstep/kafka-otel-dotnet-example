using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace kafka_otel_test.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsumerController : ControllerBase
{
    private IConfiguration _config;

    private readonly ILogger<ConsumerController> _logger;

    public ConsumerController(ILogger<ConsumerController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [HttpGet(Name = "GetRequests")]
    public async Task<MessageRack> Get()
    {
        var messageRack = new MessageRack();
        var consumer = new Consumer();
        ActivitySource aSource = new ActivitySource("kafka-otel-dotnet-example");
        using var activity = aSource.StartActivity("Consumer Controller");

        try
        {
            Console.WriteLine("start activity");
            messageRack.Topic = "train";
            messageRack = await consumer.ReadTopic(messageRack,_config,_logger);

            return messageRack;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw ex;
        }
    }
}
