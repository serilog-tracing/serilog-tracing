using Serilog;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Expressions;
using SerilogTracing.Instrumentation.AspNetCore;
using SerilogTracing.Sinks.OpenTelemetry;
using SerilogTracing.Sinks.Seq;
using SerilogTracing.Sinks.Zipkin;

// ReSharper disable RedundantSuppressNullableWarningExpression

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", typeof(Program).Assembly.GetName().Name)
    .WriteTo.Console(Formatters.CreateConsoleTextFormatter(TemplateTheme.Code))
    .WriteTo.SeqTracing("http://localhost:5341")
    .WriteTo.Zipkin("http://localhost:9411")
    .WriteTo.OpenTelemetry("http://localhost:5341/ingest/otlp/v1/logs", "http://localhost:5341/ingest/otlp/v1/traces", OtlpProtocol.HttpProtobuf, null, new Dictionary<string, object>()
    {
        { "service.name", typeof(Program).Assembly.GetName().Name ?? "unknown_service" }
    })
    .CreateLogger();

using var _ = new TracingConfiguration()
    .Instrument.FromAspNetCoreRequests()
    .EnableTracing();

Log.Information("Weather service starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var app = builder.Build();

    var forecastByPostcode = Directory.GetFiles("./data")
        .ToDictionary(f => Path.GetFileNameWithoutExtension(f)!, f => File.ReadAllText(f).Trim());

    app.MapGet("/{postcode}", (string postcode) =>
    {
        using var activity = Log.Logger.StartActivity("Look up forecast for postcode {Postcode}", postcode);
        var forecast = forecastByPostcode[postcode];
        activity.AddProperty("Forecast", forecast);

        return forecast;
    });

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
