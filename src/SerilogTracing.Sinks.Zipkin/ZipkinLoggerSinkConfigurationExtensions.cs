using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using SerilogTracing.Sinks.Zipkin;

namespace SerilogTracing;

/// <summary>
/// Extends Serilog's <c>WriteTo</c> object with the <see cref="Zipkin"/> method.
/// </summary>
public static class ZipkinLoggerSinkConfigurationExtensions
{
    static HttpMessageHandler CreateDefaultHttpMessageHandler() =>
#if FEATURE_SOCKETS_HTTP_HANDLER
        new SocketsHttpHandler { ActivityHeadersPropagator = null };
#else
        new HttpClientHandler();
#endif

    /// <summary>
    /// Send trace data to a Zipkin server.
    /// </summary>
    /// <param name="this">The configuration object that is extended.</param>
    /// <param name="endpoint">The URL of the Zipkin instance, for example, <c>http://localhost:9411</c>.</param>
    /// <param name="configureBatchingOptions">Optionally, a callback that can modify how spans are collected into
    /// batches before delivery to the sink.</param>
    /// <param name="messageHandler">Optionally, an <see cref="HttpMessageHandler"/> used when sending request to
    /// Zipkin. The default handler disables instrumentation of outbound requests so that the sink does not
    /// inadvertently generate traces.</param>
    /// <param name="restrictedToMinimumLevel">The minimum level for
    /// events passed through the sink. Ignored when <paramref name="levelSwitch" /> is specified.</param>
    /// <param name="levelSwitch">A switch allowing the pass-through minimum level
    /// to be changed at runtime.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static LoggerConfiguration Zipkin(
        this LoggerSinkConfiguration @this,
        string endpoint,
        Action<PeriodicBatchingSinkOptions>? configureBatchingOptions = null,
        HttpMessageHandler? messageHandler = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        var batchingOptions = new PeriodicBatchingSinkOptions();
        configureBatchingOptions?.Invoke(batchingOptions);

        messageHandler ??= CreateDefaultHttpMessageHandler();

        return @this.Sink(
            new PeriodicBatchingSink(new ZipkinSink(new Uri(endpoint), messageHandler), batchingOptions),
            restrictedToMinimumLevel,
            levelSwitch);
    }
}
