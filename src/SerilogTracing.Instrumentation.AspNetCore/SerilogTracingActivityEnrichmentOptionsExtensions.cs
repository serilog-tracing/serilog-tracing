using SerilogTracing.Instrumentation.AspNetCore;

namespace SerilogTracing;

/// <summary>
/// Support ASP.NET Core instrumentation.
/// </summary>
public static class InstrumentationOptionsExtensions
{
    /// <summary>
    /// Add instrumentation for ASP.NET Core requests.
    /// </summary>
    /// <returns></returns>
    public static TracingConfiguration AspNetCoreRequests(this TracingInstrumentationConfiguration configuration)
    {
        return configuration.AspNetCoreRequests(_ => { });
    }

    /// <summary>
    /// Add instrumentation for ASP.NET Core requests.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="with">A callback to configure the instrumentation.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static TracingConfiguration AspNetCoreRequests(
        this TracingInstrumentationConfiguration configuration, Action<HttpRequestInActivityInstrumentorOptions> with)
    {
        var httpOptions = new HttpRequestInActivityInstrumentorOptions();
        with.Invoke(httpOptions);
        
        return configuration.With(new HttpRequestInActivityInstrumentor(httpOptions));
    }
}