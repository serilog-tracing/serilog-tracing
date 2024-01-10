using Serilog;
using Serilog.Events;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Formatting;
using SerilogTracing.Sinks.Seq;
using SerilogTracing.Sinks.Zipkin;

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", typeof(Program).Assembly.GetName().Name)
    .WriteTo.Console(DefaultFormatting.CreateTextFormatter(TemplateTheme.Code))
    .WriteTo.SeqTracing("http://localhost:5341")
    .WriteTo.Zipkin("http://localhost:9411")
    .CreateTracingLogger();

if (args.Length != 1)
{
    Console.WriteLine("Usage: weather <POSTCODE>");
    return 1;
}

var postcode = args[0];

using var activity = Log.Logger.StartActivity("Request weather for postcode {Postcode}", postcode);

try
{
    var weatherClient = new HttpClient { BaseAddress = new("http://localhost:5133") };
    var forecast = await weatherClient.GetStringAsync(postcode);
    Console.WriteLine(forecast);

    activity.Complete();
    return 0;
}
catch (Exception ex)
{
    activity.Complete(LogEventLevel.Fatal, ex);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

