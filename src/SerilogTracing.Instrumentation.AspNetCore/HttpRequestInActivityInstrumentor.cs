using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// An activity instrumentor that populates the current activity with context from incoming HTTP requests.
/// </summary>
public sealed class HttpRequestInActivityInstrumentor: IActivityInstrumentor
{
    /// <summary>
    /// Create an instance of the instrumentor.
    /// </summary>
    public HttpRequestInActivityInstrumentor(HttpRequestInActivityInstrumentorOptions options)
    {
        _getRequestProperties = options.GetRequestProperties;
        _getResponseProperties = options.GetResponseProperties;
        _messageTemplateOverride = new MessageTemplateParser().Parse(options.MessageTemplate);
    }

    readonly Func<HttpRequest, IEnumerable<LogEventProperty>> _getRequestProperties;
    readonly Func<HttpResponse, IEnumerable<LogEventProperty>> _getResponseProperties;
    readonly MessageTemplate _messageTemplateOverride;

    /// <inheritdoc cref="IActivityInstrumentor.ShouldListenTo"/>
    public bool ShouldListenTo(string listenerName)
    {
        return listenerName == "Microsoft.AspNetCore";
    }

    /// <inheritdoc cref="IActivityInstrumentor.ShouldListenTo"/>
    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        switch (eventName)
        {
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                if (eventArgs is not HttpContext start) return;
                
                ActivityInstrumentation.SetMessageTemplateOverride(activity, _messageTemplateOverride);
                activity.DisplayName = _messageTemplateOverride.Text;
                
                ActivityInstrumentation.SetLogEventProperties(activity, _getRequestProperties(start.Request));

                break;
            case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
                var eventType = eventArgs.GetType();

                var exception = eventType.GetProperty("exception")?.GetValue(eventArgs) as Exception;
                var httpContext = eventType.GetProperty("httpContext")?.GetValue(eventArgs) as HttpContext;

                if (exception is null || httpContext is null) return;

                ActivityInstrumentation.TrySetException(activity, exception);
                
                break;
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                if (eventArgs is not HttpContext stop) return;
                
                ActivityInstrumentation.SetLogEventProperties(activity, _getResponseProperties(stop.Response));

                break;
        }
    }
}