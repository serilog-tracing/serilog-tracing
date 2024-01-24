using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace SerilogTracing.Tests.Support;

class CollectingSink: ILogEventSink
{
    public List<LogEvent> Events { get; } = [];

    public LogEvent SingleEvent => Assert.Single(Events);
    
    public void Emit(LogEvent logEvent)
    {
        Events.Add(logEvent);
    }
}
