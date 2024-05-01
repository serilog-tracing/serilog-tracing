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
using Microsoft.Data.SqlClient;
using Serilog.Events;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SerilogTracing.Instrumentation.SqlClient;

/// <summary>
/// Configuration for <see cref="SqlCommand"/> instrumentation.
/// </summary>
public sealed class SqlCommandActivityInstrumentationOptions
{
    const string DefaultCommandMessageTemplate = "SQL {Operation} {Database}";

    /// <summary>
    /// Construct a <see cref="SqlCommandActivityInstrumentationOptions"/>.
    /// </summary>
    public SqlCommandActivityInstrumentationOptions()
    {
        GetCommandProperties = DefaultGetCommandProperties;
    }

    IEnumerable<LogEventProperty> DefaultGetCommandProperties(SqlCommand command)
    {
        var database = new LogEventProperty("Database", new ScalarValue(command.Connection.Database));
        var operation = new LogEventProperty("Operation", new ScalarValue(SqlCommandInspector.GetOperation(command, InferOperation)));

        return IncludeCommandText
            ? [database, operation, new LogEventProperty("CommandText", new ScalarValue(command.CommandText))]
            : [database, operation];
    }

    static IEnumerable<LogEventProperty> DefaultGetStatisticsProperties(IDictionary statistics)
    {
        // See https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/provider-statistics-for-sql-server
        var networkServerTimeMilliseconds = statistics["NetworkServerTime"];
        if (networkServerTimeMilliseconds != null)
        {
            return [new LogEventProperty("NetworkServerTime", new ScalarValue(networkServerTimeMilliseconds))];
        }

        return [];
    }
    
    /// <summary>
    /// The message template to associate with command activities.
    /// </summary>
    public string MessageTemplate { get; set; } = DefaultCommandMessageTemplate;

    /// <summary>
    /// A function to populate properties on the activity from an executing <see cref="SqlCommand"/>. The callback
    /// runs before the command is executed.
    /// </summary>
    /// <remarks>
    /// When providing this setting, if the default properties are still required, keep a reference to the original
    /// (default) property value and call it from within the new replacement callback.
    /// </remarks>
    public Func<SqlCommand, IEnumerable<LogEventProperty>> GetCommandProperties { get; set; }

    /// <summary>
    /// A function to populate properties on the activity from command statistics. The callback
    /// runs after the command is executed.
    /// </summary>
    public Func<IDictionary, IEnumerable<LogEventProperty>> GetStatisticsProperties { get; set; } = DefaultGetStatisticsProperties;

    /// <summary>
    /// Include the command text (i.e. the stored procedure name, SQL statement, or table name) in spans. The risk
    /// of exposing sensitive data, and bloating span size, means that the default value is <c langword="false"/>.
    /// Ignored if <see cref="GetCommandProperties"/> is specified and does not chain calls to the default value.
    /// </summary>
    public bool IncludeCommandText { get; set; } = false;

    /// <summary>
    /// Attempt to infer the operation (<c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c>, or <c>EXEC</c>)
    /// by inspecting the command text. The inferred value may be incorrect in some cases as only very limited command
    /// parsing is performed. The default is <c langword="true"/>.
    /// Ignored if <see cref="GetCommandProperties"/> is specified and does not chain calls to the default value.
    /// </summary>
    public bool InferOperation { get; set; } = true;
}
