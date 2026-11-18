using System.Diagnostics;
using Xunit;

namespace SerilogTracing.Tests;

[Collection("Shared")]
public class ActivityListenerConfigurationTests
{
      [Fact]
      public void TracingConfigurationMethodsAreCallable()
      {
        // At this stage just covers some code paths that are
        // otherwise uncalled: not yet verifying outcomes, but
        // will at least pick up on obvious things like NREs.

        var configuration = new ActivityListenerConfiguration();

        configuration.Instrument.WithDefaultInstrumentation(true);
        configuration.Instrument.WithDefaultInstrumentation(false);
        configuration.Instrument.HttpClientRequests();

        configuration.Sample.Using((ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.None);
      }

      [Fact]
      public void AddingHttpClientRequests_WhenWithDefaultInstrumentationIsTrue_Throws()
      {
        // Arrange
        var configuration = new ActivityListenerConfiguration();
        configuration.Instrument.WithDefaultInstrumentation(true);

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => configuration.Instrument.HttpClientRequests()
        );
      }

      [Fact]
      public void AddingHttpClientRequests_WhenWithDefaultInstrumentationIsNotCalled_Throws()
      {
        // Arrange
        var configuration = new ActivityListenerConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => configuration.Instrument.HttpClientRequests()
        );
      }

      [Fact]
      public void AddingHttpClientRequests_WhenWithDefaultInstrumentationIsFalse_DoenstThrow()
      {
        // Arrange
        var configuration = new ActivityListenerConfiguration();
        configuration.Instrument.WithDefaultInstrumentation(false);
        configuration.Instrument.HttpClientRequests();
      }
}