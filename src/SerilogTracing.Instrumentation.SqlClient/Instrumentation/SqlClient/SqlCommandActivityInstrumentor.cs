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
using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing.Instrumentation.SqlClient;

sealed class SqlCommandActivityInstrumentor(SqlCommandActivityInstrumentationOptions options) : IActivityInstrumentor, IInstrumentationEventObserver
{
    const string DiagnosticListenerName = "SqlClientDiagnosticListener";

    static readonly ActivitySource ActivitySource = new("SerilogTracing.Instrumentation.SqlClient");

    readonly MessageTemplate _messageTemplateOverride = new MessageTemplateParser().Parse("SQL {Operation} {Database}");
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
                        var database = command.Connection.Database;
                        var operation = GetOperation(command, options.InferOperation);
                        ActivityInstrumentation.SetLogEventProperties(child,
                            new LogEventProperty("Operation", new ScalarValue(operation)),
                            new LogEventProperty("Database", new ScalarValue(database)));

                        if (options.IncludeCommandText)
                        {
                            ActivityInstrumentation.SetLogEventProperty(child, new LogEventProperty("CommandText", new ScalarValue(command.CommandText)));
                        }
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
                        // See https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/provider-statistics-for-sql-server
                        var networkServerTimeMilliseconds = statistics["NetworkServerTime"];
                        if (networkServerTimeMilliseconds != null)
                        {
                            var property = new LogEventProperty("NetworkServerTime", new ScalarValue(networkServerTimeMilliseconds));
                            ActivityInstrumentation.SetLogEventProperty(activity, property);
                        }
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

    static string GetOperation(SqlCommand command, bool inferOperationFromCommandText)
    {
        if (command.CommandType == CommandType.StoredProcedure)
            return "EXEC";

        if (command.CommandType == CommandType.TableDirect)
            return "DIRECT";

        return inferOperationFromCommandText ?
            CommandTextTokenizer.FindFirstOperation(command.CommandText) ?? "BATCH" :
            "BATCH";
    }
}