using Xunit;

namespace SerilogTracing.Instrumentation.AspNetCore.Tests;

public class TracingInstrumentationConfigurationAspNetCoreExtensionsTests
{
    [Fact]
    public void ConfigurationMethodsAreCallable()
    {
        var configuration = new TracingConfiguration();

        configuration.Instrument.AspNetCoreRequests();
        configuration.Instrument.AspNetCoreRequests(_ => { });
    }
}
