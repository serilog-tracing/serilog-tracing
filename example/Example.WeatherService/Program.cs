using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Expressions;

// ReSharper disable RedundantSuppressNullableWarningExpression

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
    .Instrument.AspNetCoreRequests(opts =>
    {
        opts.IncomingTraceParent = IncomingTraceParent.Trust;
        opts.PostSamplingFilter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
    })
    .TraceToSharedLogger();

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

    app.MapGet("/health", () => "Ok!");

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
