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
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing.Instrumentation.HttpClient;

/// <summary>
/// An activity instrumentor that populates the current activity with context from outgoing HTTP requests.
/// </summary>
sealed class HttpRequestOutActivityInstrumentor(HttpRequestOutActivityInstrumentationOptions options) : IActivityInstrumentor
{
    readonly PropertyAccessor<HttpRequestMessage> _requestAccessor = new("Request");
    readonly PropertyAccessor<TaskStatus> _requestTaskStatusAccessor = new("RequestTaskStatus");
    readonly PropertyAccessor<HttpResponseMessage> _responseAccessor = new("Response");

    readonly MessageTemplate _messageTemplateOverride = new MessageTemplateParser().Parse(options.MessageTemplate);

    /// <inheritdoc cref="IActivityInstrumentor.ShouldSubscribeTo"/>
    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == "HttpHandlerDiagnosticListener";
    }

    /// <inheritdoc cref="IActivityInstrumentor.ShouldSubscribeTo"/>
    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        switch (eventName)
        {
            case "System.Net.Http.HttpRequestOut.Start" when
                activity.OperationName == "System.Net.Http.HttpRequestOut":
                {
                    if (!_requestAccessor.TryGetValue(eventArgs, out var request) ||
                        request?.RequestUri == null)
                    {
                        return;
                    }
                    
                    ActivityInstrumentation.SetMessageTemplateOverride(activity, _messageTemplateOverride);
                    activity.DisplayName = _messageTemplateOverride.Text;

                    var props = options.GetRequestProperties(request);
                    ActivityInstrumentation.SetLogEventProperties(activity, props as LogEventProperty[] ?? props.ToArray());
                    break;
                }
            case "System.Net.Http.HttpRequestOut.Stop":
                {
                    if (!_responseAccessor.TryGetValue(eventArgs, out var response))
                    {
                        return;
                    }

                    var props = options.GetResponseProperties(response);
                    ActivityInstrumentation.SetLogEventProperties(activity, props as LogEventProperty[] ?? props.ToArray());

                    if (activity.Status == ActivityStatusCode.Unset)
                    {
                        if (response != null && options.IsErrorResponse(response) ||
                            _requestTaskStatusAccessor.TryGetValue(eventArgs, out var requestTaskStatus) && requestTaskStatus == TaskStatus.Faulted)
                        {
                            activity.SetStatus(ActivityStatusCode.Error);
                        }
                    }

                    break;
                }
        }
    }
}