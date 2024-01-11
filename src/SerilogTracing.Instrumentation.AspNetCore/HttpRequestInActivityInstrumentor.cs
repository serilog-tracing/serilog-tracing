using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Interop;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// An activity enricher that populates the current activity with context from incoming HTTP requests.
/// </summary>
public sealed class HttpRequestInActivityInstrumentor: IActivityInstrumentor
{
    /// <summary>
    /// Create an instance of the enricher.
    /// </summary>
    public HttpRequestInActivityInstrumentor(HttpRequestInActivityEnricherOptions options)
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
                
                activity.SetMessageTemplateOverride(_messageTemplateOverride);
                activity.DisplayName = _messageTemplateOverride.Text;

                foreach (var property in _getRequestProperties(start.Request))
                {
                    activity.SetTag(property.Name, property.Value);
                }

                break;
            case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
                var eventType = eventArgs.GetType();

                var ex = eventType.GetProperty("exception")?.GetValue(eventArgs) as Exception;
                var ctxt = eventType.GetProperty("httpContext")?.GetValue(eventArgs) as HttpContext;

                if (ex is null || ctxt is null) return;

                activity.TrySetException(ex);
                
                break;
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                if (eventArgs is not HttpContext stop) return;

                foreach (var property in _getResponseProperties(stop.Response))
                {
                    activity.SetTag(property.Name, property.Value);
                }

                break;
        }
    }
}