using Serilog.Events;

namespace Example;

public static class TemplateFunctions
{
    // ReSharper disable once UnusedMember.Global
    public static LogEventPropertyValue? Duration(LogEventPropertyValue? from, LogEventPropertyValue? to)
    {
        
        if (AsDateTimeOffset(from) is {} f && AsDateTimeOffset(to) is {} t)
        {
            return new ScalarValue((t - f).TotalMilliseconds);
        }

        return null;
    }

    static DateTimeOffset? AsDateTimeOffset(LogEventPropertyValue? value)
    {
        if (value is ScalarValue { Value: DateTime dt })
            return dt;
        
        if (value is ScalarValue { Value: DateTimeOffset dto })
            return dto;

        return null;
    }
}