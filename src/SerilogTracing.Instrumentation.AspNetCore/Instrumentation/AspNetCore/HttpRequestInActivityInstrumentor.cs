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

                switch (_incomingTraceParent)
                {
                    // Don't trust the incoming traceparent
                    // Generate a new root activity, using no information from the one populated by traceparent
                    case IncomingTraceParent.Ignore:
                        Activity.Current = activity.Parent;

                        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                        
                        var regenerated = activity.Source.CreateActivity(activity.DisplayName, activity.Kind);
                        if (regenerated != null)
                        {
                            regenerated.ActivityTraceFlags = ActivityTraceFlags.Recorded;
                            
                            foreach (var (name, value) in activity.EnumerateTagObjects())
                            {
                                regenerated.SetTag(name, value);
                            }
                            
                            // NOTE: Baggage is ignored
                            
                            regenerated.SetCustomProperty(RegeneratedActivityPropertyName, activity);
                            
                            regenerated.Start();

                            activity = regenerated;
                        }
                        else
                        {
                            Activity.Current = activity;
                        }

                        break;
                    
                    // Partially trust the incoming traceparent
                    // Use the propagated trace and parent ids, but ignore any flags
                    case IncomingTraceParent.Accept:
                        // NOTE: This intentionally replaces all flags with just `Recorded`
                        // It's `=` and not `|=` intentionally
                        activity.ActivityTraceFlags = ActivityTraceFlags.Recorded;
                        
                        break;
                    
                    // Fully trust the incoming traceparent
                    case IncomingTraceParent.Trust:
                        // If the incoming request has no traceparent at all then
                        // treat it as recorded
                        if (start.Request.Headers.TraceParent.Count == 0)
                        {
                            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
                        }
                        
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }

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

                if (activity.GetCustomProperty(RegeneratedActivityPropertyName) is Activity original)
                {
                    activity.Stop();
                    Activity.Current = original;
                }

                break;
        }
    }
}