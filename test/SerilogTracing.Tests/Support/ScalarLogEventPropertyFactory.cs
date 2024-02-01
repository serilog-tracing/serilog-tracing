using Serilog.Core;
using Serilog.Events;

namespace SerilogTracing.Tests.Support;

public class ScalarLogEventPropertyFactory : ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) => new(name, new ScalarValue(value));
}