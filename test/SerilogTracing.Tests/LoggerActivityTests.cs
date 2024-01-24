using Serilog;
using Serilog.Events;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests;

public class LoggerActivityTests
{
    [Theory]
    [InlineData(LogEventLevel.Debug, null, LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Debug, LogEventLevel.Debug, LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information, null, LogEventLevel.Information)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Debug, LogEventLevel.Information)]
    public void ExpectedCompletionLevelIsUsed(LogEventLevel initialLevel, LogEventLevel? completionLevel, LogEventLevel expected)
    {
        var sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(sink)
            .CreateLogger();

        using var activity = Some.Activity();
        activity.Start();
        
        var loggerActivity = new LoggerActivity(logger, initialLevel, activity, MessageTemplate.Empty, []);
        loggerActivity.Complete(completionLevel);

        var span = sink.SingleEvent;
        Assert.Equal(expected, span.Level);
    }
}