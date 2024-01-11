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
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode}";

    static LogEventLevel DefaultGetLevel(HttpContext httpContext) =>
        httpContext.Response.StatusCode > 499
            ? LogEventLevel.Error
            : LogEventLevel.Information;

    static IEnumerable<LogEventProperty> DefaultGetMessageTemplateProperties(HttpContext httpContext) =>
        new[]
        {
            new LogEventProperty("RequestMethod", new ScalarValue(httpContext.Request.Method)),
            new LogEventProperty("RequestPath", new ScalarValue(httpContext.Request.Path)),
            new LogEventProperty("StatusCode", new ScalarValue(httpContext.Response.StatusCode)),
        };
    
    /// <summary>
    /// 
    /// </summary>
    public HttpRequestInActivityEnricherOptions()
    {
        GetLevel = DefaultGetLevel;
        MessageTemplate = DefaultRequestCompletionMessageTemplate;
        GetMessageTemplateProperties = DefaultGetMessageTemplateProperties;
    }
    
     /// <summary>
    /// 
    /// </summary>
    /// <value>
    /// The message template.
    /// </value>
    public string MessageTemplate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <value>
    /// A function returning the <see cref="LogEventLevel"/>.
    /// </value>
    public Func<HttpContext, LogEventLevel> GetLevel { get; set; }

    /// <summary>
    /// Include the full URL query string in the <c>RequestPath</c> property
    /// that is attached to request log events. The default is <c>false</c>.
    /// </summary>
    public bool IncludeQueryInRequestPath { get; set; }

    /// <summary>
    /// A function to specify the values of the MessageTemplateProperties.
    /// </summary>
    public Func<HttpContext, IEnumerable<LogEventProperty>> GetMessageTemplateProperties { get; set; }
}