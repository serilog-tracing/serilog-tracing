using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// An activity instrumentor that populates the current activity with context from incoming HTTP requests.
/// </summary>
sealed class HttpRequestInActivityInstrumentor: IActivityInstrumentor
{
    /// <summary>
    /// Create an instance of the instrumentor.
    /// </summary>
    public HttpRequestInActivityInstrumentor(HttpRequestInActivityInstrumentationOptions options)
    {
        _getRequestProperties = options.GetRequestProperties;
        _getResponseProperties = options.GetResponseProperties;
        _messageTemplateOverride = new MessageTemplateParser().Parse(options.MessageTemplate);
    }

    readonly Func<HttpRequest, IEnumerable<LogEventProperty>> _getRequestProperties;
    readonly Func<HttpResponse, IEnumerable<LogEventProperty>> _getResponseProperties;
    readonly MessageTemplate _messageTemplateOverride;
    readonly PropertyAccessor<Exception> _exceptionAccessor = new("exception");
    readonly PropertyAccessor<HttpContext> _httpContextAccessor = new("httpContext");

    /// <inheritdoc />
    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == "Microsoft.AspNetCore";
    }

    /// <inheritdoc />
    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        switch (eventName)
        {
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                if (eventArgs is not HttpContext start) return;
                
                ActivityInstrumentation.SetMessageTemplateOverride(activity, _messageTemplateOverride);
                activity.DisplayName = _messageTemplateOverride.Text;
                
                ActivityInstrumentation.SetLogEventProperties(activity, _getRequestProperties(start.Request).ToArray());

                break;
            case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
                if (_exceptionAccessor.TryGetValue(eventArgs, out var exception) &&
                    _httpContextAccessor.TryGetValue(eventArgs, out var httpContext) &&
                    exception is not null && httpContext is not null)
                {
                    ActivityInstrumentation.TrySetException(activity, exception);
                }
                break;
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                if (eventArgs is not HttpContext stop) return;
                
                ActivityInstrumentation.SetLogEventProperties(activity, _getResponseProperties(stop.Response).ToArray());

                if (stop.Response.StatusCode >= 500)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }

                break;
        }
    }
}