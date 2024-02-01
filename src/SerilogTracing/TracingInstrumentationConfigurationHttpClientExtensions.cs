using SerilogTracing.Configuration;
using SerilogTracing.Instrumentation.HttpClient;

namespace SerilogTracing;

/// <summary>
/// Support ASP.NET Core instrumentation.
/// </summary>
public static class TracingInstrumentationConfigurationHttpClientExtensions
{
    /// <summary>
    /// Add instrumentation for ASP.NET Core requests.
    /// </summary>
    public static TracingConfiguration HttpClientRequests(this TracingInstrumentationConfiguration configuration)
    {
        return configuration.With(new HttpRequestOutActivityInstrumentor());
    }
}
