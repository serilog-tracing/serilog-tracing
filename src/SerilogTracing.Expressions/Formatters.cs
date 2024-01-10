using Serilog.Formatting;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace SerilogTracing.Expressions;

/// <summary>
/// Provides some simple default formatters that incorporate properties added by SerilogTracing.
/// </summary>
public static class Formatters
{
    /// <summary>
    /// Produces a text format that includes span timings.
    /// </summary>
    /// <param name="theme">Optional template theme to apply, useful only for ANSI console output.</param>
    /// <returns>The formatter.</returns>
    public static ITextFormatter CreateConsoleTextFormatter(TemplateTheme? theme = null)
    {
        return new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] " +
            "{#if IsRootSpan()}\u2514\u2500 {#else if IsSpan()}\u251c {#else if @sp is not null}\u2502 {#else}\u250A {#end}" +
            "{@m}" +
            "{#if IsSpan()} ({Milliseconds(Elapsed()):0.###} ms){#end}" +
            "\n" +
            "{@x}",
            theme: theme,
            nameResolver: new TracingNameResolver());
    }
}