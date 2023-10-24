using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using SerilogTracing;

SelfLog.Enable(Console.Error);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new ExpressionTemplate(
        "{ {@t, @mt, @l: if @l = 'Information' then undefined() else @l, @x, @sp, @tr, @ps, @st, @ra: {service: {name: 'Example'}}, ..rest()} }\n",
        theme: TemplateTheme.Code))
    .CreateLogger();

using var _ = new ActivityListenerConfiguration(Log.Logger)
    .CreateActivityListener();

const int a = 1, b = 2;
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