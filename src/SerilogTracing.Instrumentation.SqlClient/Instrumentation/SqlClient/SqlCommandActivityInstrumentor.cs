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

using System.Collections;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing.Instrumentation.SqlClient;

sealed class SqlCommandActivityInstrumentor(SqlCommandActivityInstrumentationOptions options) : IActivityInstrumentor, IInstrumentationEventObserver
{
    const string DiagnosticListenerName = "SqlClientDiagnosticListener";

    static readonly ActivitySource ActivitySource = new("SerilogTracing.Instrumentation.SqlClient");

    readonly MessageTemplate _messageTemplateOverride = new MessageTemplateParser().Parse(options.MessageTemplate);
    readonly PropertyAccessor<SqlCommand> _getCommand = new("Command");
    readonly PropertyAccessor<Exception> _getException = new("Exception");
    readonly PropertyAccessor<IDictionary> _getStatistics = new("Statistics");

    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == DiagnosticListenerName;
    }

    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
        // Instrumentation is applied in `OnDiagnosticEvent`.
    }

    public void OnNext(string eventName, object? eventArgs)
    {
        if (eventArgs == null) return;

        switch (eventName)
        {
            case "Microsoft.Data.SqlClient.WriteCommandBefore":
                {
                    // SqlClient doesn't start its own activities; we'll have to reconcile this with how the activity listener
                    // applies levelling...
                    var child = ActivitySource.StartActivity(_messageTemplateOverride.Text, ActivityKind.Client);
                    if (child == null)
                        return;
                    
                    child.DisplayName = _messageTemplateOverride.Text;

                    if (!child.IsAllDataRequested)
                    {
                        return;
                    }

                    ActivityInstrumentation.SetMessageTemplateOverride(child, _messageTemplateOverride);

                    if (_getCommand.TryGetValue(eventArgs, out var command) && command is not null)
                    {
                        var props = options.GetCommandProperties(command);
                        ActivityInstrumentation.SetLogEventProperties(child, props as LogEventProperty[] ?? props.ToArray());
                    }

                    break;
                }
            case "Microsoft.Data.SqlClient.WriteCommandAfter":
                {
                    var activity = Activity.Current;

                    // Unlikely, but possible if an additional child activity started during the command and was not
                    // stopped correctly, or conversely, if someone else stopped our activity before we did.
                    if (activity is null || activity.Source != ActivitySource)
                        return;

                    if (activity.IsAllDataRequested && _getStatistics.TryGetValue(eventArgs, out var statistics) && statistics is not null)
                    {
                        var props = options.GetStatisticsProperties(statistics);
                        ActivityInstrumentation.SetLogEventProperties(activity, props as LogEventProperty[] ?? props.ToArray());
                    }

                    activity.Stop();
                    break;
                }
            case "Microsoft.Data.SqlClient.WriteCommandError":
                {
                    var activity = Activity.Current;
                    if (activity is null || activity.Source != ActivitySource)
                        return;

                    if (activity.IsAllDataRequested)
                    {
                        if (_getException.TryGetValue(eventArgs, out var exception) && exception is not null)
                        {
                            ActivityInstrumentation.TrySetException(activity, exception);
                            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                        }
                        else
                        {
                            activity.SetStatus(ActivityStatusCode.Error);
                        }
                    }

                    activity.Stop();
                    break;
                }
        }
    }
}