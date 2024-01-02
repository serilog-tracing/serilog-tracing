using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog.Events;
using Serilog.Expressions;

namespace SerilogTracing.Formatting;

class TracingFunctionsNameResolver : NameResolver
{
    public override bool TryResolveFunctionName(string name, [NotNullWhen(true)] out MethodInfo? implementation)
    {
        if (name == nameof(ElapsedMilliseconds))
        {
            implementation = GetType().GetMethod(nameof(ElapsedMilliseconds))!;
            return true;
        }

        implementation = null;
        return false;
    }

    public static LogEventPropertyValue? ElapsedMilliseconds(LogEventPropertyValue? from, LogEventPropertyValue? to)
    {
        if (AsDateTimeOffset(from) is {} f && AsDateTimeOffset(to) is {} t)
        {
            return new ScalarValue((t - f).TotalMilliseconds);
        }

        return null;
    }

    static DateTimeOffset? AsDateTimeOffset(LogEventPropertyValue? value)
    {
        return value switch
        {
            ScalarValue { Value: DateTime dt } => dt,
            ScalarValue { Value: DateTimeOffset dto } => dto,
            _ => null
        };
    }
}