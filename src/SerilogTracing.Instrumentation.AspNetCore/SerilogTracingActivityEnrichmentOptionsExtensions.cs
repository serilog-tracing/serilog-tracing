namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// 
/// </summary>
public static class InstrumentationOptionsExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ActivityListenerOptions WithAspNetCoreRequests(this InstrumentationOptions options)
    {
        return options.WithAspNetCoreRequests(_ => { });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="with"></param>
    /// <returns></returns>
    public static ActivityListenerOptions WithAspNetCoreRequests(
        this InstrumentationOptions options, Action<HttpRequestInActivityEnricherOptions> with)
    {
        var httpOptions = new HttpRequestInActivityEnricherOptions();
        with.Invoke(httpOptions);
        
        return options.With(new HttpRequestInActivityInstrumentor(httpOptions));
    }
}