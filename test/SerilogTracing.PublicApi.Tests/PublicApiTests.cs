using PublicApiGenerator;
using Shouldly;
using Xunit;

namespace SerilogTracing.PublicApi.Tests;

public class PublicApiTests
{
    [Fact]
    public void PublicApiSurfaceIsStable()
    {
        var assembly = typeof(ActivityListenerConfiguration).Assembly;
        var publicApi = assembly.GeneratePublicApi(
            new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                ExcludeAttributes = ["System.Diagnostics.DebuggerDisplayAttribute"]
            });

        publicApi.ShouldMatchApproved(options =>
        {
            options.WithFilenameGenerator((_, _, fileType, fileExtension) =>
                $"{assembly.GetName().Name!}.{fileType}.{fileExtension}");
        });
    }
}