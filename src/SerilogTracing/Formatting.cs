using System.ComponentModel;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Formatting;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace SerilogTracing;

/// <summary>
/// 
/// </summary>
public static class Formatting
{
    // ReSharper disable once UnusedMember.Global
    /// <summary>
    /// 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never), Obsolete("Internal use only", error: true)]
    public static LogEventPropertyValue? ElapsedMilliseconds(LogEventPropertyValue? from, LogEventPropertyValue? to)
    {
        if (AsDateTimeOffset(from) is {} f && AsDateTimeOffset(to) is {} t)
        {
            return new ScalarValue((t - f).TotalMilliseconds);
        }

        return null;
    }

    static DateTimeOffset? AsDateTimeOffset(LogEventPropertyValue? value)
    {
        if (value is ScalarValue { Value: DateTime dt })
            return dt;
        
        if (value is ScalarValue { Value: DateTimeOffset dto })
            return dto;

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="theme"></param>
    /// <returns></returns>
    public static ITextFormatter CreateTextFormatter(TemplateTheme? theme = null)
    {
        return new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] " +
            "{#if ParentSpanId is not null}\u251c {#else if SpanStartTimestamp is not null}\u2514\u2500 {#else if @sp is not null}\u2502 {#end}" +
            "{@m}" +
            "{#if SpanStartTimestamp is not null} ({ElapsedMilliseconds(SpanStartTimestamp, @t):0.000} ms){#end}" +
            "\n" +
            "{@x}",
            theme: theme,
            nameResolver: new StaticMemberNameResolver(typeof(Formatting)));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static ITextFormatter CreateJsonFormatter()
    {
        return new ExpressionTemplate(
            "{ {@t, @mt, @l: if @l = 'Information' then undefined() else @l, @x, @sp, @tr, @ps: ParentSpanId, @st: SpanStartTimestamp, ..rest()} }\n");
    }
}
