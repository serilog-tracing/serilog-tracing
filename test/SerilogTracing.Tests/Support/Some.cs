using System.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing.Tests.Support;

public static class Some
{
    public static string String()
    {
        return $"string-{Guid.NewGuid()}";
    }

    public static int Integer()
    {
        return Interlocked.Increment(ref _integer);
    }

    public static bool Boolean()
    {
        return Integer() % 2 == 0;
    }

    static int _integer = new Random().Next(int.MaxValue / 2);

    public static Activity Activity(string? name = null, bool recorded = true, bool allData = true)
    {
        var activity = new Activity(name ?? String());

        if (recorded)
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        }

        activity.IsAllDataRequested = allData;

        return activity;
    }

    public static LogEvent SerilogEvent(string messageTemplate, DateTimeOffset? timestamp = null, Exception? ex = null)
    {
        return SerilogEvent(messageTemplate, new List<LogEventProperty>(), timestamp, ex);
    }

    public static LogEvent SerilogEvent(string messageTemplate, IEnumerable<LogEventProperty> properties, DateTimeOffset? timestamp = null, Exception? ex = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow;
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);
        var logEvent = new LogEvent(
            ts,
            LogEventLevel.Warning,
            ex,
            template,
            properties);

        return logEvent;
    }

    public static ActivitySource ActivitySource()
    {
        return new ActivitySource(String());
    }

    public static ActivityListener AlwaysOnListenerFor(string sourceName)
    {
        var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == sourceName;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> ctx) => ctx.Source.Name == sourceName ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None;
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
