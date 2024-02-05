using Xunit;

namespace SerilogTracing.Instrumentation.SqlClient.Tests;

public class TracingInstrumentationConfigurationSqlClientExtensionsTests
{
    [Fact]
    public void ConfigurationMethodsAreCallable()
    {
        var configuration = new TracingConfiguration();

        configuration.Instrument.SqlClientCommands();
        configuration.Instrument.SqlClientCommands(_ => { });
    }
}
