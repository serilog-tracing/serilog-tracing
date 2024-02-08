using SerilogTracing.Configuration;
using SerilogTracing.Instrumentation.HttpClient;

namespace SerilogTracing;

/// <summary>
/// Support ASP.NET Core instrumentation.
/// </summary>
public static class ActivityListenerInstrumentationConfigurationHttpClientExtensions
{
    /// <summary>
    /// Add instrumentation for ASP.NET Core requests.
    /// </summary>
    public static ActivityListenerConfiguration HttpClientRequests(this ActivityListenerInstrumentationConfiguration configuration)
    {
        return configuration.With(new HttpRequestOutActivityInstrumentor());
    }
}
