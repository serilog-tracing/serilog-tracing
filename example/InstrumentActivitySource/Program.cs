﻿using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Expressions;
using SerilogTracing.Instrumentation;

DistributedContextPropagator.Current = DistributedContextPropagator.CreateDefaultPropagator();

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", typeof(Program).Assembly.GetName().Name)
    .WriteTo.Console(Formatters.CreateConsoleTextFormatter(TemplateTheme.Code))
    .WriteTo.Seq("http://localhost:5341")
    .WriteTo.Zipkin("http://localhost:9411")
    .WriteTo.OpenTelemetry("http://localhost:4318", OtlpProtocol.HttpProtobuf, null, new Dictionary<string, object>
    {
        { "service.name", typeof(Program).Assembly.GetName().Name ?? "unknown_service" }
    })
    .CreateLogger();

using var _ = new ActivityListenerConfiguration()
    .Instrument.With(new RabbitProducerInstrumentor())
    .TraceToSharedLogger();

var receiver = Task.Run(async () =>
{
    var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
    await using var connection = await factory.CreateConnectionAsync();
    await using var channel = await connection.CreateChannelAsync();
    
    await channel.QueueDeclareAsync(queue: "hello",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var received = new TaskCompletionSource();
    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += (_, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        
        received.SetResult();
        return Task.CompletedTask;
    };
    
    await channel.BasicConsumeAsync(queue: "hello",
        autoAck: true,
        consumer: consumer);
    await received.Task;
});

var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "hello",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);
    
const string message = "Hello World!";
var body = Encoding.UTF8.GetBytes(message);

await channel.BasicPublishAsync(
    string.Empty,
    "hello",
    true,
    new BasicProperties(),
    body,
    CancellationToken.None);

await receiver;

await Log.CloseAndFlushAsync();

class RabbitProducerInstrumentor : ActivitySourceInstrumentor
{
    public RabbitProducerInstrumentor()
    {
    }

    readonly ReplacementActivitySource _source = new("Rabbit");

    protected override void InstrumentActivity(Activity incoming)
    {
        _source.StartReplacementActivity(
            activity =>
            {
                ActivityInstrumentation.SetMessageTemplateOverride(activity, new MessageTemplateParser().Parse("RabbitMQ {Role}"));
                ActivityInstrumentation.SetLogEventProperty(activity, "Role", new ScalarValue(incoming.Source.Name.Split('.').Last()));
            }
        );
    }

    protected override bool ShouldInstrument(ActivitySource source)
    {
        return source.Name.StartsWith("RabbitMQ");
    }
}