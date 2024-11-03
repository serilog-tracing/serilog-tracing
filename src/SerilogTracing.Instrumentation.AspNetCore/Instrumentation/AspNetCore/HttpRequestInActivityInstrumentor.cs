// Copyright © SerilogTracing Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing.Instrumentation.AspNetCore;

/// <summary>
/// An activity instrumentor that populates the current activity with context from incoming HTTP requests.
/// </summary>
sealed class HttpRequestInActivityInstrumentor : IActivityInstrumentor, IInstrumentationEventObserver
{
    readonly Func<HttpRequest, IEnumerable<LogEventProperty>> _getRequestProperties;
    readonly Func<HttpResponse, IEnumerable<LogEventProperty>> _getResponseProperties;
    readonly MessageTemplate _messageTemplateOverride;
    readonly Func<HttpResponse,bool> _isErrorResponse;
    readonly IncomingTraceParent _incomingTraceParent;
    readonly Func<HttpContext,bool>? _postSamplingFilter;

    readonly PropertyAccessor<Exception> _exceptionAccessor = new("exception");
    readonly PropertyAccessor<HttpContext> _httpContextAccessor = new("httpContext");

    internal const string ReplacementActivitySourceName = "SerilogTracing.Instrumentation.AspNetCore";
    const string TargetDiagnosticListenerName = "Microsoft.AspNetCore";

    /// <summary>
    /// Create an instance of the instrumentor.
    /// </summary>
    public HttpRequestInActivityInstrumentor(HttpRequestInActivityInstrumentationOptions options)
    {
        _getRequestProperties = options.GetRequestProperties;
        _getResponseProperties = options.GetResponseProperties;
        _messageTemplateOverride = new MessageTemplateParser().Parse(options.MessageTemplate);
        _incomingTraceParent = options.IncomingTraceParent;
        _isErrorResponse = options.IsErrorResponse;
        _postSamplingFilter = options.PostSamplingFilter;
    }

    /// <inheritdoc />
    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == TargetDiagnosticListenerName;
    }
    
    /// <inheritdoc />
    public void OnNext(string eventName, object? eventArgs)
    {
        if (eventName !=  "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start" || 
            eventArgs is not HttpContext start) return;

        var (inheritTags, inheritParent, inheritFlags, inheritBaggage) = InheritFlags(_incomingTraceParent);
        
        ActivityInstrumentation.StartReplacementActivity(
            _ => _postSamplingFilter?.Invoke(start) ?? true,
            replacement =>
            {
                ActivityInstrumentation.SetMessageTemplateOverride(replacement, _messageTemplateOverride);
                replacement.DisplayName = _messageTemplateOverride.Text;

                var props = _getRequestProperties(start.Request);
                ActivityInstrumentation.SetLogEventProperties(replacement,
                    props as LogEventProperty[] ?? props.ToArray());
            },
            inheritTags,
            inheritParent,
            inheritFlags,
            inheritBaggage
        );
    }
    
    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        switch (eventName)
        {
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

                var props = _getResponseProperties(stop.Response);
                ActivityInstrumentation.SetLogEventProperties(activity, props as LogEventProperty[] ?? props.ToArray());

                if (_isErrorResponse(stop.Response))
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }

                // This event is triggered before the activity itself is stopped
                // We stop our replacement activity and restore the original for ASP.NET to complete
                if (ActivityInstrumentation.TryGetReplacedActivity(activity, out var original))
                {
                    activity.Stop();
                    Activity.Current = original;
                }

                break;
        }
    }

    static (bool, bool, bool, bool) InheritFlags(IncomingTraceParent incomingTraceParent)
    {
        return incomingTraceParent switch
        {
            // Don't trust the incoming traceparent
            IncomingTraceParent.Ignore =>
                // Generate a new root activity, using no information from the traceparent
                (false, false, false, false),

            // Partially trust the incoming traceparent
            IncomingTraceParent.Accept =>
                // Use the propagated trace and parent ids, but ignore any flags or baggage
                (true, true, false, false),

            // Fully trust the incoming traceparent
            IncomingTraceParent.Trust =>
                // The incoming activity is still replaced, so that:
                // 1. Sampling is properly applied, even if the activity was manually created by ASP.NET Core
                // 2. Clients that don't send any traceparent header may still produce a recorded activity
                (true, true, true, true),

            _ => throw new ArgumentOutOfRangeException(nameof(incomingTraceParent))
        };
    }
}
