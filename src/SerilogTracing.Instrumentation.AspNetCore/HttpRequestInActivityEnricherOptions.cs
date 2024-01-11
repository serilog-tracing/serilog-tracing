using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// 
/// </summary>
public sealed class HttpRequestInActivityEnricherOptions
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
    /// 
    /// </summary>
    public HttpRequestInActivityEnricherOptions()
    {
        MessageTemplate = DefaultRequestCompletionMessageTemplate;
        GetRequestProperties = DefaultGetRequestProperties;
        GetResponseProperties = DefaultGetResponseProperties;
    }
    
     /// <summary>
    /// 
    /// </summary>
    /// <value>
    /// The message template.
    /// </value>
    public string MessageTemplate { get; set; }

    /// <summary>
    /// A function to specify the values of the MessageTemplateProperties.
    /// </summary>
    public Func<HttpRequest, IEnumerable<LogEventProperty>> GetRequestProperties { get; set; }
    
    /// <summary>
    /// A function to specify the values of the MessageTemplateProperties.
    /// </summary>
    public Func<HttpResponse, IEnumerable<LogEventProperty>> GetResponseProperties { get; set; }
}