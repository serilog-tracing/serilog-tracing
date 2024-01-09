using Serilog;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Formatting;
using SerilogTracing.Sinks.Zipkin;

// ReSharper disable RedundantSuppressNullableWarningExpression

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", typeof(Program).Assembly.GetName().Name)
    .WriteTo.Console(DefaultFormatting.CreateTextFormatter(TemplateTheme.Code))
    .WriteTo.Seq(
        "http://localhost:5341",
        payloadFormatter: DefaultFormatting.CreateJsonFormatter(),
        messageHandler: new SocketsHttpHandler { ActivityHeadersPropagator = null })
    .WriteTo.Zipkin("http://localhost:9411")
    .CreateTracingLogger();

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
