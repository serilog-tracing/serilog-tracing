using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// Configuration for a <see cref="HttpRequestInActivityInstrumentor"/>.
/// </summary>
public sealed class HttpRequestInActivityInstrumentorOptions
{
    const string DefaultRequestCompletionMessageTemplate =
        "HTTP {RequestMethod} {RequestPath}";

    static IEnumerable<LogEventProperty> DefaultGetRequestProperties(HttpRequest request) =>
        new[]
        {
            new LogEventProperty("RequestMethod", new ScalarValue(request.Method)),
            new LogEventProperty("RequestPath", new ScalarValue(request.Path)),
        };
    
    static IEnumerable<LogEventProperty> DefaultGetResponseProperties(HttpResponse response) =>
        new[]
        {
            new LogEventProperty("StatusCode", new ScalarValue(response.StatusCode)),
        };

    /// <summary>
    /// Construct a default set of options.
    /// </summary>
    public HttpRequestInActivityInstrumentorOptions()
    {
        MessageTemplate = DefaultRequestCompletionMessageTemplate;
        GetRequestProperties = DefaultGetRequestProperties;
        GetResponseProperties = DefaultGetResponseProperties;
    }
    
    /// <summary>
    /// The message template to associate with request activities.
    /// </summary>
    public string MessageTemplate { get; set; }

    /// <summary>
    /// A function to populate properties on the activity from an incoming request.
    ///
    /// This closure will be invoked at the start of the request pipeline.
    /// </summary>
    public Func<HttpRequest, IEnumerable<LogEventProperty>> GetRequestProperties { get; set; }
    
    /// <summary>
    /// A function to populate properties on the activity from an outgoing response.
    ///
    /// This closure will be invoked at the end of the request pipeline.
    /// </summary>
    public Func<HttpResponse, IEnumerable<LogEventProperty>> GetResponseProperties { get; set; }
}