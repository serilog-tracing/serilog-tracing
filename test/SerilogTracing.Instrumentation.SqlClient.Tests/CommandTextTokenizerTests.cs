using Xunit;

namespace SerilogTracing.Instrumentation.SqlClient.Tests;

public class CommandTextTokenizerTests
{
    [Theory]
    [InlineData("", null)]
    [InlineData("test", null)]
    [InlineData("select 1", "SELECT")]
    [InlineData("--test\nselect 1", "SELECT")]
    [InlineData("use a;select 1", "SELECT")]
    [InlineData("insert 1", "INSERT")]
    [InlineData("update 1", "UPDATE")]
    [InlineData("delete 1", "DELETE")]
    [InlineData("exec 1", "EXEC")]
    [InlineData("insert a;select 1", "INSERT")]
    public void OperationTypeIsInferred(string sql, string? expected)
    {
        var actual = CommandTextTokenizer.FindFirstOperation(sql);
        Assert.Equal(expected, actual);
    }
}