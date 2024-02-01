using Serilog.Core;
using Serilog.Events;
using SerilogTracing.Interop;

namespace SerilogTracing.Enrichers;

internal class SpanTimingMilliseconds: ILogEventEnricher
{
    public SpanTimingMilliseconds(string propertyName)
    {
        _propertyName = propertyName;
    }
    
    readonly string _propertyName;
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.TryGetElapsed(out var elapsed))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(_propertyName, elapsed.Value.TotalMilliseconds));
        }
    }
}
