using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog.Events;
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// Utilities for <see cref="IActivityInstrumentor">activity instrumentors</see> to enrich
/// <see cref="Activity">activities</see> for <see cref="Serilog.ILogger">loggers</see>.
/// </summary>
public static class ActivityInstrumentation
{
    /// <summary>
    /// Associate a <see cref="MessageTemplate"/> with the given <see cref="Activity"/>, without changing the
    /// <see cref="Activity.DisplayName"/>.
    ///
    /// The message template will be assigned to a custom property on the activity, overwriting any previously set value
    /// with the same name.
    /// </summary>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="messageTemplate">The message template to assign.</param>
    public static void SetMessageTemplateOverride(Activity activity, MessageTemplate messageTemplate)
    {
        activity.SetCustomProperty(Constants.MessageTemplateOverridePropertyName, messageTemplate);
    }
    
    /// <summary>
    /// Get a <see cref="MessageTemplate"/> previously associated with the given <see cref="Activity"/> by
    /// <see cref="ActivityInstrumentation.SetMessageTemplateOverride"/>.
    /// </summary>
    /// <param name="activity">The activity containing the message template.</param>
    /// <param name="messageTemplate">The assigned message template, if any.</param>
    /// <returns>True when the activity contains a message template.</returns>
    public static bool TryGetMessageTemplateOverride(Activity activity, [NotNullWhen(true)] out MessageTemplate? messageTemplate)
    {
        if (activity.GetCustomProperty(Constants.MessageTemplateOverridePropertyName) is MessageTemplate customPropertyValue)
        {
            messageTemplate = customPropertyValue;
            return true;
        }

        messageTemplate = null;
        return false;
    }

    /// <summary>
    /// Set a <see cref="LogEventProperty"/> on the given <see cref="Activity"/>, overwriting any previously set value
    /// with the same name.
    ///
    /// Properties are added to a collection in a custom property on the activity.
    /// If the property value is a <see cref="ScalarValue"/> then it will also set a tag on the activity, making
    /// it visible to outside instrumentation.
    /// </summary>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="property">The property to assign.</param>
    public static void SetLogEventProperty(Activity activity, LogEventProperty property)
    {
        SetLogEventProperty(activity, property, GetOrInitLogEventPropertyCollection(activity));
    }

    /// <summary>
    /// Set multiple <see cref="LogEventProperty">log event properties</see>, overwriting any previously set values
    /// with the same names.
    ///
    /// This method behaves like multiple calls to <see cref="ActivityInstrumentation.SetLogEventProperty(Activity, LogEventProperty)"/>, but
    /// is more efficient.
    /// </summary>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="properties">The properties to assign.</param>
    public static void SetLogEventProperties(Activity activity, params LogEventProperty[] properties)
    {
        var collection = GetOrInitLogEventPropertyCollection(activity);

        foreach (var property in properties)
        {
            SetLogEventProperty(activity, property, collection);
        }
    }

    static void SetLogEventProperty(Activity activity, LogEventProperty property, Dictionary<string, LogEventProperty> collection)
    {
        if (property.Value is ScalarValue sv)
        {
            activity.SetTag(property.Name, sv.Value);
        }

        collection[property.Name] = property;
    }

    /// <summary>
    /// Get all <see cref="LogEventProperty">log event properties</see> set on the activity by <see cref="ActivityInstrumentation.SetLogEventProperty(Activity, LogEventProperty)"/>.
    ///
    /// This method won't include tags on the activity.
    /// </summary>
    /// <param name="activity">The activity containing the properties.</param>
    /// <returns>A collection of properties set on the activity.</returns>
    public static IEnumerable<LogEventProperty> GetLogEventProperties(Activity activity)
    {
        return TryGetLogEventPropertyCollection(activity, out var existing) ? existing.Values : Enumerable.Empty<LogEventProperty>();
    }
    
    static bool TryGetLogEventPropertyCollection(Activity activity, [NotNullWhen(true)] out Dictionary<string, LogEventProperty>? properties)
    {
        if (activity.GetCustomProperty(Constants.LogEventPropertyCollectionName) is Dictionary<string, LogEventProperty> existing)
        {
            properties = existing;
            return true;
        }

        properties = null;
        return false;
    }

    static Dictionary<string, LogEventProperty> GetOrInitLogEventPropertyCollection(Activity activity)
    {
        if (TryGetLogEventPropertyCollection(activity, out var existing))
        {
            return existing;
        }

        var added = new Dictionary<string, LogEventProperty>();
        activity.SetCustomProperty(Constants.LogEventPropertyCollectionName, added);

        return added;
    }

    /// <summary>
    /// Try associate an <see cref="Exception"/> with the activity.
    ///
    /// The exception will be stored as an event with the current timestamp, using the conventional
    /// name "exception".
    ///
    /// This method won't overwrite any previously set exception.
    /// </summary>
    /// <param name="activity">The activity to instrument.</param>
    /// <param name="exception">True if the exception was set on the event.</param>
    public static bool TrySetException(Activity activity, Exception exception)
    {
        if (activity.Events.Any(e => e.Name == Constants.ExceptionEventName)) return false;
        
        activity.AddEvent(EventFromException(exception));
        return true;
    }
    
    static ActivityEvent EventFromException(Exception exception)
    {
        var tags = new ActivityTagsCollection
        {
            ["exception.stacktrace"] = exception.ToString(),
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message
        };
        return new ActivityEvent(Constants.ExceptionEventName, DateTimeOffset.Now, tags);
    }

    /// <summary>
    /// Try get an exception previously associated with the activity using the conventional event name "exception".
    /// </summary>
    /// <param name="activity">The activity containing the exception.</param>
    /// <param name="exception">True if an exception event is present on the activity. The type of the returned
    /// exception is not guaranteed to match the one originally set on the activity.</param>
    /// <returns></returns>
    public static bool TryGetException(Activity activity, [NotNullWhen(true)] out Exception? exception)
    {
        exception = ExceptionFromEvents(activity);

        return exception != null;
    }
    
    static Exception? ExceptionFromEvents(Activity activity)
    {
        var first = activity.Events.FirstOrDefault(e => e.Name == "exception");
        if (first.Name != "exception")
            return null;
        
        return new TextException(
            first.Tags.FirstOrDefault(t => t.Key == "exception.message").Value as string,
            first.Tags.FirstOrDefault(t => t.Key == "exception.type").Value as string,
            first.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace").Value as string);
    }
    
    class TextException(
        string? message,
        string? type,
        string? toString) : Exception(message ?? type)
    {
        public override string ToString() => toString ?? "No information available.";
    }

    /// <summary>
    /// Compute a <see cref="LogEventLevel"/> based on the status of the activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>A <see cref="LogEventLevel"/> based on <see cref="Activity.Status"/>. If the status is
    /// <see cref="ActivityStatusCode.Error"/> then the completion value will be <see cref="LogEventLevel.Error"/>.
    /// Otherwise it'll be <see cref="LogEventLevel.Information"/>.</returns>
    public static LogEventLevel GetCompletionLevel(Activity activity)
    {
        return activity.Status == ActivityStatusCode.Error ? LogEventLevel.Error : LogEventLevel.Information;
    }
}