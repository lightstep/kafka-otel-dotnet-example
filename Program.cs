using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// prep tracing

var serviceName = "kafka-otel-dotnet-example";
var serviceVersion = "0.0.1";

// Configure important OpenTelemetry settings, the console exporter, and instrumentation library
builder.Services.AddOpenTelemetryTracing(b =>
{
    b
    .AddOtlpExporter(opt =>
    {
        opt.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
		opt.Endpoint = new Uri("[insert Lightstep endpoint URL]");
        opt.Headers = "lightstep-access-token=[insert Lightstep access token here]";
    })
    .AddConsoleExporter()
    .AddSource(serviceName)
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation();
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var settings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
