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
sealed class HttpRequestInActivityInstrumentor : IActivityInstrumentor
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

    const string RegeneratedActivityPropertyName = "SerilogTracing.Instrumentation.AspNetCore.Regenerated";

    const string SourceName = "Microsoft.AspNetCore";
    const string DiagnosticListenerName = "Microsoft.AspNetCore";

    // NOTE: Using the same name as the source used by ASP.NET Core so filtering on it works
    static readonly ActivitySource ImpersonatedSource = new(SourceName);

    static ActivitySource GetSource(Activity activity) =>
        activity.Source.Name == SourceName ? activity.Source : ImpersonatedSource;

    /// <inheritdoc />
    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == DiagnosticListenerName;
    }

    /// <inheritdoc />
    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        switch (eventName)
        {
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                if (eventArgs is not HttpContext start) return;

                Activity? recreated;
                switch (_incomingTraceParent)
                {
                    // Don't trust the incoming traceparent
                    // Generate a new root activity, using no information from the traceparent
                    case IncomingTraceParent.Ignore:
                        recreated = RecreateActivity(activity);
                        recreated?.Start();

                        break;

                    // Partially trust the incoming traceparent
                    // Use the propagated trace and parent ids, but ignore any flags or baggage
                    case IncomingTraceParent.Accept:
                        recreated = RecreateActivity(activity);

                        if (recreated != null)
                        {
                            recreated.SetParentId(activity.TraceId, activity.ParentSpanId,
                                activity.ActivityTraceFlags | ActivityTraceFlags.Recorded);

                            // NOTE: Baggage is ignored here

                            recreated.Start();
                        }

                        recreated = activity;

                        break;

                    // Fully trust the incoming traceparent
                    // Only generate a new 
                    case IncomingTraceParent.Trust:
                        // If the incoming request has no traceparent at all or
                        // if the generated activity doesn't come from the expected source then recreate it
                        //
                        // This ensures:
                        // 1. Sampling is properly applied, even if the activity was manually created by ASP.NET Core
                        // 2. Clients that don't send any traceparent header may still produce a recorded activity
                        if (activity.Source.Name != SourceName || start.Request.Headers.TraceParent.Count == 0)
                        {
                            recreated = RecreateActivity(activity);

                            if (recreated != null)
                            {
                                // We don't really expect there to be a trace id or baggage here
                                // since the client never supplied a `traceparent` header, but if
                                // the server is configured with some alternative distributed context propagator
                                // then it could conceivably come from elsewhere. In these cases we still regenerate
                                // the activity, but retain any context pulled externally
                                recreated.SetParentId(activity.TraceId, activity.ParentSpanId,
                                    activity.ActivityTraceFlags | ActivityTraceFlags.Recorded);

                                foreach (var (k, v) in activity.Baggage)
                                {
                                    recreated.SetBaggage(k, v);
                                }

                                recreated.Start();
                            }
                        }
                        else
                        {
                            recreated = activity;
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (recreated != null)
                {
                    ActivityInstrumentation.SetMessageTemplateOverride(recreated, _messageTemplateOverride);
                    recreated.DisplayName = _messageTemplateOverride.Text;

                    ActivityInstrumentation.SetLogEventProperties(recreated,
                        _getRequestProperties(start.Request).ToArray());
                }

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

                if (activity.GetCustomProperty(RegeneratedActivityPropertyName) is Activity original)
                {
                    activity.Stop();
                    Activity.Current = original;
                }

                break;
        }
    }

    static Activity? RecreateActivity(Activity activity)
    {
        Activity.Current = activity.Parent;

        // Suppress the activity created by ASP.NET Core
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        activity.IsAllDataRequested = false;

        var regenerated = GetSource(activity).CreateActivity(activity.DisplayName, activity.Kind);
        if (regenerated != null)
        {
            regenerated.ActivityTraceFlags = ActivityTraceFlags.Recorded;

            foreach (var (name, value) in activity.EnumerateTagObjects())
            {
                regenerated.SetTag(name, value);
            }

            // NOTE: Baggage is ignored

            regenerated.SetCustomProperty(RegeneratedActivityPropertyName, activity);
        }
        else
        {
            Activity.Current = activity;
        }

        return regenerated;
    }
}