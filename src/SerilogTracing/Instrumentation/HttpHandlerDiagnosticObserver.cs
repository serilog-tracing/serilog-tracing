using System.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Interop;

namespace SerilogTracing.Instrumentation;

sealed class HttpHandlerDiagnosticObserver : IObserver<KeyValuePair<string,object?>>
{
    static readonly Func<object, HttpRequestMessage?> GetRequest = CreateAccessor<HttpRequestMessage>(
        "System.Net.Http.DiagnosticsHandler+ActivityStartData, System.Net.Http", "Request");
    
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
            if (request == null) return;
            
            activity.DisplayName = $"HTTP {request.Method} {request.RequestUri}";
            
            // Need to consider how much of the URI is reasonable to emit.
            activity.AddTag("RequestUri", request.RequestUri);
            activity.AddTag("RequestMethod", request.Method);
            
            ActivityUtil.SetMessageTemplateOverride(activity, MessageTemplateOverride);
        }
        else if (value.Key == "System.Net.Http.HttpRequestOut.Stop")
        {
            var response = GetResponse(value.Value);
            if (response == null) return;
            
            activity.AddTag("StatusCode", (int)response.StatusCode);
        }
    }

    static Func<object, T?> CreateAccessor<T>(string typeName, string propertyName)
    {
        var type = Type.GetType(typeName, throwOnError: true)!;
        var propertyInfo = type.GetProperty(propertyName)!;
        return receiver => (T?)propertyInfo.GetValue(receiver);
    }
}
