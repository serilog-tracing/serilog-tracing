using System.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace SerilogTracing;

public sealed class LoggerActivity : IDisposable
{
    internal const string SelfPropertyName = "SerilogTracing.LoggerActivity.Self";
    
    bool _complete;
    readonly Activity? _activity;

    public LoggerActivity(Activity? activity, MessageTemplate messageTemplate, IEnumerable<LogEventProperty> captures)
    {
        _activity = activity;
        Activity = activity;
        MessageTemplate = messageTemplate;
        Captures = captures;
        
        activity?.SetCustomProperty(SelfPropertyName, this);
    }

    public Activity? Activity { get; private init; }
    public MessageTemplate MessageTemplate { get; }
    public IEnumerable<LogEventProperty> Captures { get; }
    public Exception? Exception { get; private set; }
    public LogEventLevel? CompletionLevel { get; private set; }

    public void Complete(
        LogEventLevel level = LogEventLevel.Information,
        Exception? exception = null)
    {
        if (_complete)
        {
            return;
        }
        
        _complete = true;

        CompletionLevel = level;
        if (exception != null)
        {
            Exception = exception;
            _activity?.AddEvent(EventFromException(exception));
        }

        Activity?.SetStatus(level <= LogEventLevel.Warning ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        Activity?.Dispose();
    }

    static ActivityEvent EventFromException(Exception exception)
    {
        var tags = new ActivityTagsCollection
        {
            ["exception.stacktrace"] = exception.ToString(),
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message
        };
        return new ActivityEvent("exception", DateTimeOffset.Now, tags);
    }

    public static Exception? ExceptionFromEvents(Activity activity)
    {
        var first = activity.Events.FirstOrDefault(e => e.Name == "exception");
        if (first.Name != "exception")
            return null;
        return new TextException(
            first.Tags.FirstOrDefault(t => t.Key == "exception.message").Value as string,
            first.Tags.FirstOrDefault(t => t.Key == "exception.type").Value as string,
            first.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace").Value as string);
    }

    public void Dispose()
    {
        Complete();
    }
}

public class TextException : Exception
{
    readonly string? _toString;

    public TextException(
        string? message,
        string? type,
        string? toString)
    : base(message ?? type)
    {
        _toString = toString;
    }

    public override string ToString() => _toString ?? "No information available.";
}

static class SerilogActivitySource
{
    public const string Name = "Serilog";
    const string Version = "0.0.1";

    public static ActivitySource Instance { get; } = new ActivitySource(Name, Version);
}

public static class LoggerTracingExtensions
{
    static readonly MessageTemplate NoTemplate = new MessageTemplate(Enumerable.Empty<MessageTemplateToken>());
    
    [MessageTemplateFormatMethod(nameof(messageTemplate))]
    public static LoggerActivity StartActivity(this ILogger logger, string messageTemplate, params object?[] propertyValues)
    {
        var activity = SerilogActivitySource.Instance.StartActivity();

        if (!logger.BindMessageTemplate(messageTemplate, propertyValues, out var parsedTemplate, out var captures))
            return new LoggerActivity(null, NoTemplate, Enumerable.Empty<LogEventProperty>());
            
        return new LoggerActivity(activity, parsedTemplate, captures);
    }
}

public static class LoggerConfigurationTracingExtensions
{
    public static Logger CreateTracingLogger(this LoggerConfiguration loggerConfiguration)
    {
        var logger = loggerConfiguration.CreateLogger();
        var listener = new ActivityListener();
        listener.Sample = delegate { return ActivitySamplingResult.AllData; };
        listener.ShouldListenTo = source => logger.ForContext(Constants.SourceContextPropertyName, source.Name).IsEnabled(LogEventLevel.Fatal);
        listener.ActivityStopped += a =>
        {
            var serilogActivity = a.GetCustomProperty(LoggerActivity.SelfPropertyName) is LoggerActivity sa
                ? sa
                : null;
            
            var activityLogger = logger.ForContext(Constants.SourceContextPropertyName, a.Source.Name);

            var level = serilogActivity?.CompletionLevel ?? (a.Status == ActivityStatusCode.Error ? LogEventLevel.Error : LogEventLevel.Information);
            if (!activityLogger.IsEnabled(level))
                return;

            var template = serilogActivity?.MessageTemplate ?? new MessageTemplate(new[] { new TextToken(a.DisplayName) });
            var exception = serilogActivity != null ? serilogActivity.Exception : LoggerActivity.ExceptionFromEvents(a);

            var properties = new Dictionary<string, LogEventProperty>((serilogActivity?.Captures ?? Enumerable.Empty<LogEventProperty>()).ToDictionary(p => p.Name));
            foreach (var tag in a.Tags.Concat(a.Baggage).Select(t => new KeyValuePair<string,object?>(t.Key, t.Value)).Concat(a.TagObjects))
            {
                if (properties.ContainsKey(tag.Key))
                    continue;

                if (!logger.BindProperty(tag.Key, tag.Value, destructureObjects: false, out var property))
                    continue;
                
                properties.Add(tag.Key, property);
            }

            properties["@st"] = new LogEventProperty("@st", new ScalarValue(a.StartTimeUtc));
            if (a.ParentSpanId != default)
            {
                properties["@ps"] = new LogEventProperty("@ps", new ScalarValue(a.ParentSpanId.ToString()));
            }

            var evt = new LogEvent(
                DateTimeOffset.Now,
                level,
                exception,
                template,
                properties.Values,
                a.TraceId,
                a.SpanId);

            logger.Write(evt);
        };
        
        ActivitySource.AddActivityListener(listener);

        return logger;
    }
}
