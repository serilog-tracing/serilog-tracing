using System.Diagnostics;
using Xunit;

namespace SerilogTracing.Tests;

[Collection("Shared")]
public class TracingConfigurationTests
{
    [Fact]
    public void TracingConfigurationMethodsAreCallable()
    {
        // At this stage just covers some code paths that are
        // otherwise uncalled: not yet verifying outcomes, but
        // will at least pick up on obvious things like NREs.

        var configuration = new TracingConfiguration();

        configuration.Instrument.WithDefaultInstrumentation(true);
        configuration.Instrument.WithDefaultInstrumentation(false);
        configuration.Instrument.HttpClientRequests();

        configuration.Sample.Using((ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.None);
    }
}