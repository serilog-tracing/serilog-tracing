using System.Text.RegularExpressions;

namespace SerilogTracing.Instrumentation.SqlClient;

static class CommandTextTokenizer
{
    static readonly Regex Pattern = new("\\b(select|insert|update|delete|exec)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
    public static string? FindFirstOperation(string sql)
    {
        var m = Pattern.Match(sql);
        return m.Success ? m.Groups[0].Value.ToUpperInvariant() : null;
    }
}