using Example;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Templates;
using Serilog.Templates.Themes;
using SerilogTracing;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("System.Net.Http", LevelAlias.Off)
    .WriteTo.Console(
        formatter: new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] {@m}{#if @p['@st'] is not null} ({duration(@p['@st'], @t):0.000} ms){#end}\n{@x}",
            theme: TemplateTheme.Grayscale,
            nameResolver: new StaticMemberNameResolver(typeof(TemplateFunctions))))
    .WriteTo.Seq(
        "http://localhost:5341",
        payloadFormatter: new ExpressionTemplate(
            "{ {@t, @mt, @l: if @l = 'Information' then undefined() else @l, @x, @sp, @tr, @ps: @p['@ps'], @st: @p['@st'], @ra: {service: {name: 'Example'}}, ..rest()} }\n"))
    .CreateLogger();

using var _ = new ActivityListenerConfiguration()
    .SetLogger(Log.Logger)
    .CreateActivityListener();

var log = Log.ForContext(typeof(Program));

const int a = 1, b = 2;
using var activity = log.StartActivity("Compute sum of {A} and {B}", a, b);
try
{
    Log.Information("Hello, world!");

    activity.Activity?.AddTag("org.serilog-tracing.example", 123);

    int sum;
    using (log.StartActivity("Do the maths"))
    {
        sum = a + b;
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