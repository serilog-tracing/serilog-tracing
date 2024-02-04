using Microsoft.Data.SqlClient;

namespace SerilogTracing.Instrumentation.SqlClient;

/// <summary>
/// Configuration for <see cref="SqlCommand"/> instrumentation.
/// </summary>
public class SqlCommandActivityInstrumentationOptions
{
    /// <summary>
    /// Include the command text (i.e. the stored procedure name, SQL statement, or table name) in spans. The risk
    /// of exposing sensitive data, and bloating span size, means that the default value is <c langword="false"/>.
    /// </summary>
    public bool IncludeCommandText { get; set; } = false;
}
