using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Compact;
using SerilogTracing;

SelfLog.Enable(Console.Error);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateTracingLogger();

var a = 1;
var b = 2;
using var activity = Log.Logger.StartActivity("Compute sum of {A} and {B}", a, b);
try
{
    Log.Information("Hello, world!");
    
    activity.Activity?.AddTag("X", 123);
    
    Console.WriteLine(a + b);

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