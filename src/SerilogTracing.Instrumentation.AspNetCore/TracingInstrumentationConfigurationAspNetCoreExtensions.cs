using SerilogTracing.Configuration;
using SerilogTracing.Instrumentation.AspNetCore;

namespace SerilogTracing;

/// <summary>
/// Extends <see cref="TracingInstrumentationConfiguration"/> with methods to support ASP.NET
/// Core instrumentation.
/// </summary>
public static class TracingInstrumentationConfigurationAspNetCoreExtensions
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
    /// <param name="configure">A callback to configure the instrumentation.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static TracingConfiguration AspNetCoreRequests(
        this TracingInstrumentationConfiguration configuration, Action<HttpRequestInActivityInstrumentationOptions> configure)
    {
        var httpOptions = new HttpRequestInActivityInstrumentationOptions();
        configure.Invoke(httpOptions);
        
        return configuration.With(new HttpRequestInActivityInstrumentor(httpOptions));
    }
}