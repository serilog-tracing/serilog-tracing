using Serilog.Core;
using Serilog.Events;
using SerilogTracing.Instrumentation;

namespace SerilogTracing.Enrichers;

class SpanTimingEnricher: ILogEventEnricher
{
    public SpanTimingEnricher(string propertyName)
    {
        _propertyName = propertyName;
    }
    
    readonly string _propertyName;
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (LogEventTracingProperties.TryGetElapsed(logEvent, out var elapsed))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(_propertyName, elapsed.Value));
        }
    }
}
