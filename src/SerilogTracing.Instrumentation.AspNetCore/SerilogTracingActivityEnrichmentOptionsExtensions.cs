namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// Support ASP.NET Core instrumentation.
/// </summary>
public static class InstrumentationOptionsExtensions
{
    /// <summary>
    /// Add instrumentation for ASP.NET Core requests.
    /// </summary>
    /// <returns></returns>
    public static ActivityListenerOptions WithAspNetCoreRequests(this InstrumentationOptions options)
    {
        return options.WithAspNetCoreRequests(_ => { });
    }

    /// <summary>
    /// Add instrumentation for ASP.NET Core requests.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="with">A callback to configure the instrumentation.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static ActivityListenerOptions WithAspNetCoreRequests(
        this InstrumentationOptions options, Action<HttpRequestInActivityInstrumentorOptions> with)
    {
        var httpOptions = new HttpRequestInActivityInstrumentorOptions();
        with.Invoke(httpOptions);
        
        return options.With(new HttpRequestInActivityInstrumentor(httpOptions));
    }
}