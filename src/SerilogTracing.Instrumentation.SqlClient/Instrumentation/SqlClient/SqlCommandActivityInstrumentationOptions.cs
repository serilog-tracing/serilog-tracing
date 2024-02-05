using Microsoft.Data.SqlClient;

namespace SerilogTracing.Instrumentation.SqlClient;

/// <summary>
/// Configuration for <see cref="SqlCommand"/> instrumentation.
/// </summary>
public sealed class SqlCommandActivityInstrumentationOptions
{
    /// <summary>
    /// Include the command text (i.e. the stored procedure name, SQL statement, or table name) in spans. The risk
    /// of exposing sensitive data, and bloating span size, means that the default value is <c langword="false"/>.
    /// </summary>
    public bool IncludeCommandText { get; set; } = false;

    /// <summary>
    /// Attempt to infer the operation (<c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c>, or <c>EXEC</c>)
    /// by inspecting the command text. The inferred value may be incorrect in some cases as only very limited command
    /// parsing is performed. The default is <c langword="true"/>.
    /// </summary>
    public bool InferOperation { get; set; } = true;
}
