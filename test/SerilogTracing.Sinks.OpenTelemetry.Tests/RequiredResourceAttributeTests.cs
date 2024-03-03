﻿using System.Globalization;
using SerilogTracing.Sinks.OpenTelemetry.ProtocolHelpers;
using SerilogTracing.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace SerilogTracing.Sinks.OpenTelemetry.Tests;

public class RequiredResourceAttributeTests
{
    [Fact]
    public void ServiceNameIsPreservedWhenPresent()
    {
        var supplied = Some.String();
        var ra = new Dictionary<string, object>
        {
            ["service.name"] = supplied
        };

        var actual = RequiredResourceAttributes.AddDefaults(ra);

        Assert.Equal(supplied, actual["service.name"]);
    }

    [Fact]
    public void MissingServiceNameDefaultsToExecutableName()
    {
        var actual = RequiredResourceAttributes.AddDefaults(new Dictionary<string, object>());

        Assert.StartsWith("unknown_service:", (string)actual["service.name"]);
    }

    [Fact]
    public void MissingTelemetrySdkGroupDefaultsToKnownValues()
    {
        var actual = RequiredResourceAttributes.AddDefaults(new Dictionary<string, object>());
        Assert.Equal("serilog", actual["telemetry.sdk.name"]);
        Assert.Equal("dotnet", actual["telemetry.sdk.language"]);
        // First character of the version is always expected to be numeric.
        Assert.True(int.TryParse(((string)actual["telemetry.sdk.version"])[..1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _));
    }
}
