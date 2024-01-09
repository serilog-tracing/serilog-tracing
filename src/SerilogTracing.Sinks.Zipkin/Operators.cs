using System.Diagnostics.CodeAnalysis;
using Serilog.Events;

namespace SerilogTracing.Sinks.Zipkin;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
static class Operators
{
    static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    public static LogEventPropertyValue? Micros(LogEventPropertyValue? value)
    {
        if (value is ScalarValue { Value: DateTime dt })
            return ToMicros(dt.ToUniversalTime());

        if (value is ScalarValue { Value: DateTimeOffset dto })
            return ToMicros(dto.UtcDateTime);

        return null;
    }

    static LogEventPropertyValue? ToMicros(DateTime utcDateTime)
    {
        if (utcDateTime < UnixEpoch) throw new ArgumentOutOfRangeException(nameof(utcDateTime));
        var timeSinceEpoch = utcDateTime - UnixEpoch;
        return new ScalarValue((ulong)timeSinceEpoch.Ticks / 10);
    }
}
