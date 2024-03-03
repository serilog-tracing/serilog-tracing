using Xunit;

namespace SerilogTracing.Instrumentation.AspNetCore.Tests;

public class ActivityListenerInstrumentationConfigurationAspNetCoreExtensionsTests
{
    [Fact]
    public void ConfigurationMethodsAreCallable()
    {
        var configuration = new ActivityListenerConfiguration();

        configuration.Instrument.AspNetCoreRequests();
        configuration.Instrument.AspNetCoreRequests(_ => { });
    }
}
