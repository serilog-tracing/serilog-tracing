using Serilog.Formatting;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace SerilogTracing.Formatting;

/// <summary>
/// Provides some simple default text and JSON formatters that incorporate properties added by SerilogTracing.
/// </summary>
public static class DefaultFormatting
{
    /// <summary>
    /// Produces a text format that includes span timings.
    /// </summary>
    /// <param name="theme">Optional template theme to apply, useful only for ANSI console output.</param>
    /// <returns>The formatter.</returns>
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
            nameResolver: new TracingFunctionsNameResolver());
    }

    /// <summary>
    /// Produces a JSON format based on the one used by Serilog.Formatting.Compact, which is also compatible with the format
    /// accepted by Seq.
    /// </summary>
    /// <param name="theme">Optional template theme to apply, useful only for ANSI console output.</param>
    /// <returns>The formatter.</returns>
    public static ITextFormatter CreateJsonFormatter(TemplateTheme? theme = null)
    {
        return new ExpressionTemplate(
            "{ {@t, @mt, @l: if @l = 'Information' then undefined() else @l, @x, @sp, @tr, @ps: ParentSpanId, @st: SpanStartTimestamp, ..rest()} }\n",
            theme: theme);
    }
}