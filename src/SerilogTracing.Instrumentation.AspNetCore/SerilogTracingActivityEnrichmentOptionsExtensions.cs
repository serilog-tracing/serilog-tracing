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
    public static SerilogTracingOptions WithAspNetCoreInstrumentation(this SerilogTracingActivityEnrichmentOptions options)
    {
        return options.With<HttpRequestInActivityEnricher>();
    }
}