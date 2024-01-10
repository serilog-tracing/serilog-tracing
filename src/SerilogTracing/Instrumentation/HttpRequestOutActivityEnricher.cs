using System.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Interop;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// An activity enricher that populates the current activity with context from outgoing HTTP requests.
/// </summary>
public sealed class HttpRequestOutActivityEnricher: IActivityEnricher
{
    /// <summary>
    /// Create an instance of the enricher.
    /// </summary>
    public HttpRequestOutActivityEnricher()
    {}
    
    /// <inheritdoc cref="IActivityEnricher.ShouldListenTo"/>
    public bool ShouldListenTo(string listenerName)
    {
        return listenerName == "HttpHandlerDiagnosticListener";
    }

    /// <inheritdoc cref="IActivityEnricher.ShouldListenTo"/>
    public void EnrichActivity(Activity activity, string eventName, object eventArgs)
    {
        switch (eventName)
        {
            case "System.Net.Http.HttpRequestOut.Start" when
                activity.OperationName == "System.Net.Http.HttpRequestOut":
            {
                var request = GetRequest(eventArgs);
                if (request == null || request.RequestUri == null) return;

                // The message template and properties will need to be set through a configurable enrichment
                // mechanism, since the detail/information-leakage trade-off will be different for different
                // consumers.
            
                // For now, stripping any user, query, and fragment should be a reasonable default.

                var uriBuilder = new UriBuilder(request.RequestUri)
                {
                    Query = null,
                    Fragment = null,
                    UserName = null,
                    Password = null
                };

                activity.SetMessageTemplateOverride(MessageTemplateOverride);
                activity.DisplayName = MessageTemplateOverride.Text;
                activity.AddTag("RequestUri", uriBuilder.Uri);
                activity.AddTag("RequestMethod", request.Method);
                break;
            }
            case "System.Net.Http.HttpRequestOut.Stop":
            {
                var response = GetResponse(eventArgs);
                activity.AddTag("StatusCode", response != null ? (int)response.StatusCode : null);

                if (activity.Status == ActivityStatusCode.Unset)
                {
                    var requestTaskStatus = GetRequestTaskStatus(eventArgs);
                    if (requestTaskStatus == TaskStatus.Faulted || response is { IsSuccessStatusCode: false })
                        activity.SetStatus(ActivityStatusCode.Error);
                }

                break;
            }
        }
    }
    
    static readonly Func<object, HttpRequestMessage?> GetRequest = CreateAccessor<HttpRequestMessage>(
        "System.Net.Http.DiagnosticsHandler+ActivityStartData, System.Net.Http", "Request");
    
    static readonly Func<object, TaskStatus> GetRequestTaskStatus = CreateAccessor<TaskStatus>(
        "System.Net.Http.DiagnosticsHandler+ActivityStopData, System.Net.Http", "RequestTaskStatus");
    
    static readonly Func<object, HttpResponseMessage?> GetResponse = CreateAccessor<HttpResponseMessage>(
        "System.Net.Http.DiagnosticsHandler+ActivityStopData, System.Net.Http", "Response");

    static readonly MessageTemplate MessageTemplateOverride =
        new MessageTemplateParser().Parse("HTTP {RequestMethod} {RequestUri}");
    
    static Func<object, T?> CreateAccessor<T>(string typeName, string propertyName) where T: notnull
    {
        var type = Type.GetType(typeName, throwOnError: true)!;
        var propertyInfo = type.GetProperty(propertyName)!;
        return receiver => (T?)propertyInfo.GetValue(receiver);
    }
}