using System.Text;
using Example;
using Serilog;
using Serilog.Events;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Formatting;

Console.OutputEncoding = new UTF8Encoding(false);

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", "Example")
    .WriteTo.Console(formatter: DefaultFormatting.CreateTextFormatter(TemplateTheme.Code))
    .WriteTo.Seq("http://localhost:5341", payloadFormatter: DefaultFormatting.CreateJsonFormatter(), messageHandler: new SocketsHttpHandler { ActivityHeadersPropagator = null })
    .CreateTracingLogger();

var log = Log.ForContext(typeof(Program));

const int a = 1, b = 2;
using var activity = log.StartActivity("Compute sum of {A} and {B}", a, b);
try
{
    log.Warning("Applying fudge factor");
    
    const int fudgeFactor = 17;
    activity.AddProperty("FudgeFactor", fudgeFactor);

    int sum;
    using (log.StartActivity("Add the two numbers"))
    {
        sum = a + b + fudgeFactor;
    }

    using (log.StartActivity("Write the output"))
    {
        ConsoleWriter.WriteSomeInterestingInfo(sum);
    }
    
    throw new NotImplementedException();
}
catch (Exception ex)
{
    activity.Complete(LogEventLevel.Error, ex);
}
finally
{
    await Log.CloseAndFlushAsync();
}
