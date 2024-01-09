using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace SerilogTracing.Sinks.Zipkin;

public static class ZipkinLoggerSinkConfigurationExtensions
{
    public static LoggerConfiguration Zipkin(
        this LoggerSinkConfiguration @this,
        string endpoint, PeriodicBatchingSinkOptions? batchingOptions = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        batchingOptions ??= new PeriodicBatchingSinkOptions();
        return @this.Sink(
            new PeriodicBatchingSink(new ZipkinSink(new Uri(endpoint)), batchingOptions),
            restrictedToMinimumLevel,
            levelSwitch);
    }
}

