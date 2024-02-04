using System.Collections;
using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing.Instrumentation.SqlClient;

class SqlCommandActivityInstrumentor(SqlCommandActivityInstrumentationOptions options): IActivityInstrumentor
{
    const string DiagnosticListenerName = "SqlClientDiagnosticListener";

    static readonly ActivitySource ActivitySource = new(typeof(SqlCommandActivityInstrumentor).Assembly.GetName().Name!);
    
    readonly MessageTemplate _messageTemplateOverride = new MessageTemplateParser().Parse("SQL {Operation}");
    readonly PropertyAccessor<SqlCommand> _getCommand = new("Command");
    readonly PropertyAccessor<Exception> _getException = new("Exception");
    readonly PropertyAccessor<IDictionary> _getStatistics = new("Statistics");
    
    public bool ShouldSubscribeTo(string diagnosticListenerName)
    {
        return diagnosticListenerName == DiagnosticListenerName;
    }

    public void InstrumentActivity(Activity activity, string eventName, object eventArgs)
    {
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
                ActivityInstrumentation.SetMessageTemplateOverride(child, _messageTemplateOverride);
                
                if (_getCommand.TryGetValue(eventArgs, out var command) && command is not null)
                {
                    // Here we'll need to quickly, heuristically, try to spot the command type based on the command text.
                    var operation = GetOperationType(command);
                    ActivityInstrumentation.SetLogEventProperties(child, new LogEventProperty("Operation", new ScalarValue(operation)));

                    if (options.IncludeCommandText)
                    {
                        ActivityInstrumentation.SetLogEventProperty(child, new LogEventProperty("CommandText", new ScalarValue(command.CommandText)));
                    }
                }

                break;
            }
            case "Microsoft.Data.SqlClient.WriteCommandAfter":
            {
                // Unlikely, but possible if an additional child activity started during the command and was not
                // stopped correctly, or conversely, if someone else stopped our activity before we did.
                if (activity.Source != ActivitySource)
                    return;

                if (_getStatistics.TryGetValue(eventArgs, out var statistics) && statistics is not null)
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
                if (activity.Source != ActivitySource)
                    return;

                if (_getException.TryGetValue(eventArgs, out var exception) && exception is not null)
                {
                    ActivityInstrumentation.TrySetException(activity, exception);
                    activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                }
                else
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }

                activity.Stop();
                break;
            }
        }
    }

    static string GetOperationType(SqlCommand command)
    {
        if (command.CommandType == CommandType.StoredProcedure)
            return "EXEC";

        if (command.CommandType == CommandType.TableDirect)
            return "DIRECT";

        // Here we'll need to, for better or for worse, attempt to heuristically detect the operation type.
        
        return "BATCH";
    }
}