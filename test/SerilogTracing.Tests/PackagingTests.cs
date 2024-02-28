using Xunit;

namespace SerilogTracing.Tests;

public class PackagingTests
{
    [Fact]
    public void SerilogTracingAssemblyIsSigned()
    {
        var assemblyName = typeof(ActivityListenerConfiguration).Assembly.GetName();
        var token = assemblyName.GetPublicKeyToken();
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }
}