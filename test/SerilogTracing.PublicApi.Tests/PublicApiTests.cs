using System.Reflection;
using PublicApiGenerator;
using SerilogTracing.Expressions;
using Shouldly;
using Xunit;

namespace SerilogTracing.PublicApi.Tests;

public class PublicApiTests
{
    [Theory]
    [InlineData(typeof(ActivityListenerConfiguration))]
    [InlineData(typeof(TracingNameResolver))]
    [InlineData(typeof(ActivityListenerInstrumentationConfigurationSqlClientExtensions))]
    [InlineData(typeof(ActivityListenerInstrumentationConfigurationAspNetCoreExtensions))]
    [InlineData(typeof(OpenTelemetryLoggerConfigurationExtensions))]
    [InlineData(typeof(ZipkinLoggerSinkConfigurationExtensions))]
    public void PublicApiSurfaceIsStable(Type representativeType)
    {
        var assembly = representativeType.Assembly;
        var publicApi = assembly.GeneratePublicApi(
            new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false
            });

        publicApi.ShouldMatchApproved(options =>
        {
            options.WithFilenameGenerator((_, _, fileType, fileExtension) =>
                $"{assembly.GetName().Name!}.{fileType}.{fileExtension}");
        });
    }
}