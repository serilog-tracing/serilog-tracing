using System.Diagnostics.CodeAnalysis;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace SerilogTracing.Sinks.Zipkin;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
static class ZipkinFunctions
{
    static readonly JsonValueFormatter JsonValueFormatter = new("$type");

    public static LogEventPropertyValue? ToUpperInvariant(LogEventPropertyValue? value)
    {
        // In Serilog.Expressions we'd use Coerce.String() here.
        var s = value switch
        {
            ScalarValue { Value: string str } => str,
            ScalarValue { Value: { } v } when v.GetType().IsEnum => v.ToString(),
            _ => null
        };

        return s is null ? null : new ScalarValue(s.ToUpperInvariant());
    }

    public static LogEventPropertyValue? AsStringTags(LogEventPropertyValue? value)
    {
        if (value is not StructureValue sv) return null;

        return new StructureValue(sv.Properties
                .Select(p =>
                {
                    switch (p.Value)
                    {
                        case ScalarValue { Value: string }:
                            return p;
                        case ScalarValue { Value: null }:
                            return new LogEventProperty(p.Name, new ScalarValue(""));
                        default:
                            var sw = new StringWriter();
                            JsonValueFormatter.Format(p.Value, sw);
                            return new LogEventProperty(p.Name, new ScalarValue(sw.ToString()));
                    }
                }),
            sv.TypeTag);
    }
}