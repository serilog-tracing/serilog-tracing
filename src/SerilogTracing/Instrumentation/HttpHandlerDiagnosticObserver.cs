using System.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Interop;

namespace SerilogTracing.Instrumentation;

sealed class HttpHandlerDiagnosticObserver : IObserver<KeyValuePair<string,object?>>
{
    static readonly Func<object, HttpRequestMessage?> GetRequest = CreateAccessor<HttpRequestMessage>(
        "System.Net.Http.DiagnosticsHandler+ActivityStartData, System.Net.Http", "Request");
    
    static readonly Func<object, TaskStatus> GetRequestTaskStatus = CreateAccessor<TaskStatus>(
        "System.Net.Http.DiagnosticsHandler+ActivityStopData, System.Net.Http", "RequestTaskStatus");
    
    static readonly Func<object, HttpResponseMessage?> GetResponse = CreateAccessor<HttpResponseMessage>(
        "System.Net.Http.DiagnosticsHandler+ActivityStopData, System.Net.Http", "Response");

    static readonly MessageTemplate MessageTemplateOverride =
        new MessageTemplateParser().Parse("HTTP {RequestMethod} {RequestUri}");
    
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Value == null || Activity.Current == null) return;
        var activity = Activity.Current;
        
        if (value.Key == "System.Net.Http.HttpRequestOut.Start" &&
            activity.OperationName == "System.Net.Http.HttpRequestOut")
        {
            var request = GetRequest(value.Value);
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

            ActivityUtil.SetMessageTemplateOverride(activity, MessageTemplateOverride);
            activity.DisplayName = MessageTemplateOverride.Text;
            activity.AddTag("RequestUri", uriBuilder.Uri);
            activity.AddTag("RequestMethod", request.Method);
        }
        else if (value.Key == "System.Net.Http.HttpRequestOut.Stop")
        {
            var response = GetResponse(value.Value);
            activity.AddTag("StatusCode", response != null ? (int)response.StatusCode : null);

            if (activity.Status == ActivityStatusCode.Unset)
            {
                var requestTaskStatus = GetRequestTaskStatus(value.Value);
                if (requestTaskStatus == TaskStatus.Faulted || response is { IsSuccessStatusCode: false })
                    activity.SetStatus(ActivityStatusCode.Error);
            }
        }
    }

    static Func<object, T?> CreateAccessor<T>(string typeName, string propertyName) where T: notnull
    {
        var type = Type.GetType(typeName, throwOnError: true)!;
        var propertyInfo = type.GetProperty(propertyName)!;
        return receiver => (T?)propertyInfo.GetValue(receiver);
    }
}
