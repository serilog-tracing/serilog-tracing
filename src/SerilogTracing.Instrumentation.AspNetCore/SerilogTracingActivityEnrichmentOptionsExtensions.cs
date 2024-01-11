using SerilogTracing.Options;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// 
/// </summary>
public static class SerilogTracingActivityEnrichmentOptionsExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static SerilogTracingOptions WithAspNetCoreInstrumentation(this SerilogTracingActivityInstrumentationOptions options)
    {
        return options.WithAspNetCoreInstrumentation(_ => { });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="with"></param>
    /// <returns></returns>
    public static SerilogTracingOptions WithAspNetCoreInstrumentation(
        this SerilogTracingActivityInstrumentationOptions options, Action<HttpRequestInActivityEnricherOptions> with)
    {
        var httpOptions = new HttpRequestInActivityEnricherOptions();
        with.Invoke(httpOptions);
        
        return options.With(new HttpRequestInActivityInstrumentor(httpOptions));
    }
}