using Xunit;

namespace SerilogTracing.Instrumentation.SqlClient.Tests;

public class ActivityListenerInstrumentationConfigurationSqlClientExtensionsTests
{
    [Fact]
    public void ConfigurationMethodsAreCallable()
    {
        var configuration = new ActivityListenerConfiguration();

        configuration.Instrument.SqlClientCommands();
        configuration.Instrument.SqlClientCommands(_ => { });
    }
}
