using System.Text;
using Example;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Templates;
using Serilog.Templates.Themes;
using SerilogTracing;

Console.OutputEncoding = new UTF8Encoding(false);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("System.Net.Http", LevelAlias.Off)
    .WriteTo.Console(
        formatter: new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] " +
            "{#if @p['@ps'] is not null}\u251c {#else if @p['@st'] is not null}\u2514\u2500 {#else if @sp is not null}\u2502 {#end}" +
            "{@m}" +
            "{#if @p['@st'] is not null} ({duration(@p['@st'], @t):0.000} ms){#end}" +
            "\n" +
            "{@x}",
            theme: TemplateTheme.Code,
            nameResolver: new StaticMemberNameResolver(typeof(TemplateFunctions))))
    .WriteTo.Seq(
        "http://localhost:5341",
        payloadFormatter: new ExpressionTemplate(
            "{ {@t, @mt, @l: if @l = 'Information' then undefined() else @l, @x, @sp, @tr, @ps: @p['@ps'], @st: @p['@st'], @ra: {service: {name: 'Example'}}, ..rest()} }\n"))
    .CreateLogger();

using var _ = SerilogActivityListener.Create();

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