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
    /// <summary>
    /// Create an instance of the instrumentor.
    /// </summary>
    public HttpRequestInActivityInstrumentor(HttpRequestInActivityInstrumentationOptions options)
    {
        _getRequestProperties = options.GetRequestProperties;
        _getResponseProperties = options.GetResponseProperties;
        _messageTemplateOverride = new MessageTemplateParser().Parse(options.MessageTemplate);
        _incomingTraceParent = options.IncomingTraceParent;
    }

    readonly Func<HttpRequest, IEnumerable<LogEventProperty>> _getRequestProperties;
    readonly Func<HttpResponse, IEnumerable<LogEventProperty>> _getResponseProperties;
    readonly MessageTemplate _messageTemplateOverride;
    readonly PropertyAccessor<Exception> _exceptionAccessor = new("exception");
    readonly PropertyAccessor<HttpContext> _httpContextAccessor = new("httpContext");
    readonly IncomingTraceParent _incomingTraceParent;

    const string ReplacedActivityPropertyName = "SerilogTracing.Instrumentation.AspNetCore.Replaced";
    internal const string ReplacementActivitySourceName = "SerilogTracing.Instrumentation.AspNetCore";
    const string TargetDiagnosticListenerName = "Microsoft.AspNetCore";
    const string DefaultActivityName = "SerilogTracing.Instrumentation.AspNetCore.HttpRequestIn";

    static readonly ActivitySource ReplacementActivitySource = new(ReplacementActivitySourceName);

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

        // Here we make a hard assumption that this is from the Microsoft.AspNetCore activity
        // source; it's not possible to check decisively, since although the incoming source
        // will usually be named Microsoft.AspNetCore, on some paths the source name will be the
        // default "" empty string.
        var incoming = Activity.Current;
        
        // Important to do this first, otherwise our activity source will consult the inherited
        // activity when making sampling decisions.
        Activity.Current = incoming?.Parent;

        var replacement = CreateReplacementActivity(incoming, _incomingTraceParent);

        if (incoming != null)
        {
            // Suppress the activity created by ASP.NET Core. Important to do this last, because
            // we use the incoming flags in the preceding code.
            incoming.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            incoming.IsAllDataRequested = false;
        }

        if (replacement != null)
        {
            ActivityInstrumentation.SetMessageTemplateOverride(replacement, _messageTemplateOverride);
            replacement.DisplayName = _messageTemplateOverride.Text;

            ActivityInstrumentation.SetLogEventProperties(replacement,
                _getRequestProperties(start.Request).ToArray());

            replacement.Start();
        }
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

                ActivityInstrumentation.SetLogEventProperties(activity,
                    _getResponseProperties(stop.Response).ToArray());

                if (stop.Response.StatusCode >= 500)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }

                if (activity.GetCustomProperty(ReplacedActivityPropertyName) is Activity original)
                {
                    activity.Stop();
                    Activity.Current = original;
                }

                break;
        }
    }

    internal static Activity? CreateReplacementActivity(Activity? incoming, IncomingTraceParent incomingTraceParent)
    {
        return incomingTraceParent switch
        {
            // Don't trust the incoming traceparent
            IncomingTraceParent.Ignore =>
                // Generate a new root activity, using no information from the traceparent
                CreateReplacementActivity(incoming, false, false, false, false),
            
            // Partially trust the incoming traceparent
            IncomingTraceParent.Accept =>
                // Use the propagated trace and parent ids, but ignore any flags or baggage
                CreateReplacementActivity(incoming, true, true, false, false),
            
            // Fully trust the incoming traceparent
            IncomingTraceParent.Trust =>
                // The incoming activity is still replaced, so that:
                // 1. Sampling is properly applied, even if the activity was manually created by ASP.NET Core
                // 2. Clients that don't send any traceparent header may still produce a recorded activity
                CreateReplacementActivity(incoming, true, true, true, true),
            
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    static Activity? CreateReplacementActivity(Activity? incoming, bool inheritTags, bool inheritParent, bool inheritFlags, bool inheritBaggage)
    {
        var replacement = ReplacementActivitySource.CreateActivity(DefaultActivityName, ActivityKind.Server);

        if (incoming == null)
        {
            return replacement;
        }

        if (replacement != null)
        {
            replacement.SetCustomProperty(ReplacedActivityPropertyName, incoming);

            if (inheritTags)
            {
                foreach (var (name, value) in incoming.EnumerateTagObjects())
                {
                    replacement.SetTag(name, value);
                }
            }

            if (inheritParent)
            {
                var flags = inheritFlags ? incoming.ActivityTraceFlags : replacement.ActivityTraceFlags;
                replacement.SetParentId(incoming.TraceId, incoming.ParentSpanId, flags);
            }

            if (inheritBaggage)
            {
                foreach (var (k, v) in incoming.Baggage)
                {
                    replacement.SetBaggage(k, v);
                }
            }
        }
        
        return replacement;
    }
}
