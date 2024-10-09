using Serilog;
using SerilogTracing;
using SerilogTracing.Expressions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(Formatters.CreateConsoleTextFormatter())
    .CreateLogger();

using var _ = new ActivityListenerConfiguration()
    .Sample.OneTraceIn(7)
    .TraceToSharedLogger();

for (var i = 0; i < 10000; ++i)
{
    using var outer = Log.Logger.StartActivity("Outer {i}", i);
    using var inner = Log.Logger.StartActivity("Inner {i}", i);
    await Task.Delay(100);
}
