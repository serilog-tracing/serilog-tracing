using OpenTelemetry.Trace;

namespace SerilogTracing.OpenTelemetry.Exporter.Tests;

public class ActivityExportTests : IDisposable
{
    static ActivityExportTests()
    {
        // This is necessary to force activity id allocation on .NET Framework and early .NET Core versions. When this isn't
        // done, log events end up carrying null trace and span ids (which won't work with SerilogTracing).
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
    }

    private readonly Stack<IDisposable> _disposables = new();

    private readonly string _activitySourceName = Some.String();

    private readonly CollectingSink _sink;
    private readonly ILogger _logger;
    private readonly Activity _root;

    public ActivityExportTests()
    {
        _sink = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Sink(_sink)
            .CreateLogger();

        _disposables.Push(logger);
        _logger = logger;

        var rootActivitySourceName = Some.String();

        _disposables.Push(
            Sdk.CreateTracerProviderBuilder()
                .AddSource(rootActivitySourceName)
                .AddSource(_activitySourceName)
                .AddSource(SerilogTracingConstants.SerilogActivitySourceName)
                .AddSerilogExporter(_logger)
                .Build()
        );

        // emulating root activity created by some instrumentation
        // as this activity is never completed, it won't end up in our logs
        _disposables.Push(CreateAlwaysOnListenerFor(rootActivitySourceName));
        var rootSource = new ActivitySource(rootActivitySourceName);
        _disposables.Push(rootSource);
        var root = rootSource.StartActivity(Some.String())!;
        
        Assert.NotNull(root);
        Assert.NotEqual(default(ActivityTraceId).ToHexString(), root.TraceId.ToHexString());
        Assert.NotEqual(default(ActivitySpanId).ToHexString(), root.SpanId.ToHexString());

        _disposables.Push(root);
        _root = root;
    }

    void IDisposable.Dispose()
    {
        while (_disposables.Count > 0) _disposables.Pop().Dispose();
    }

    private static ActivityListener CreateAlwaysOnListenerFor(string sourceName)
    {
        var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == sourceName;
        listener.SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private LogEvent AssertSingleLoggedSpan(LogEventLevel expectedLevel, string expectedText)
    {
        var span = _sink.SingleEvent;

        Assert.Equal(expectedLevel, span.Level);
        Assert.Equal(expectedText, span.MessageTemplate.Text);

        Assert.Equal(_root.TraceId, span.TraceId);
        Assert.Equal(_root.SpanId, (span.Properties[SerilogTracingConstants.ParentSpanIdPropertyName] as ScalarValue)?.Value);

        Assert.Contains(SerilogTracingConstants.SpanStartTimestampPropertyName, span.Properties);

        return span;
    }

    [Theory]
    [InlineData(LogEventLevel.Debug, null, LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Debug, LogEventLevel.Debug, LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information, null, LogEventLevel.Information)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Debug, LogEventLevel.Information)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Error, LogEventLevel.Error)]
    public void Logger_StartActivity(LogEventLevel initialLevel, LogEventLevel? completionLevel, LogEventLevel expectedLevel)
    {
        var messageTemplate = Some.String();
        
        var activity = _logger.StartActivity(initialLevel, messageTemplate);
        activity.Complete(completionLevel);

        AssertSingleLoggedSpan(expectedLevel, messageTemplate);
    }

    [Theory]
    [InlineData(null, LogEventLevel.Information)]
    [InlineData(ActivityStatusCode.Unset, LogEventLevel.Information)]
    [InlineData(ActivityStatusCode.Ok, LogEventLevel.Information)]
    [InlineData(ActivityStatusCode.Error, LogEventLevel.Error)]
    public void Diagnostics_StartActivity(ActivityStatusCode? activityStatusCode, LogEventLevel expectedLevel)
    {
        using var source = new ActivitySource(_activitySourceName);

        var messageTemplate = Some.String();
        
        var activity = source.StartActivity(messageTemplate);
        Assert.NotNull(activity);
        if (activityStatusCode != null)
            activity.SetStatus(activityStatusCode.Value);
        activity.Dispose();

        var span = AssertSingleLoggedSpan(expectedLevel, messageTemplate);
        Assert.Equal(_activitySourceName, (span.Properties[SerilogConstants.SourceContextPropertyName] as ScalarValue)?.Value);
    }
}