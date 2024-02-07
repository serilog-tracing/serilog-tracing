using System.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Instrumentation;
using SerilogTracing.Tests.Support;
using Xunit;

namespace SerilogTracing.Tests;

public class ActivityInstrumentationTests
{
    [Fact]
    public void GetSetMessageTemplateOverride()
    {
        var templateA = new MessageTemplateParser().Parse("{Template} A");
        var templateB = new MessageTemplateParser().Parse("{Template} B");

        var activity = Some.Activity();
        
        Assert.False(ActivityInstrumentation.TryGetMessageTemplateOverride(activity, out _));
        
        ActivityInstrumentation.SetMessageTemplateOverride(activity, templateA);
        
        Assert.True(ActivityInstrumentation.TryGetMessageTemplateOverride(activity, out var fromActivity));
        Assert.Equal(templateA.Text, fromActivity.Text);
        
        ActivityInstrumentation.SetMessageTemplateOverride(activity, templateB);
        
        Assert.True(ActivityInstrumentation.TryGetMessageTemplateOverride(activity, out fromActivity));
        Assert.Equal(templateB.Text, fromActivity.Text);
    }

    [Fact]
    public void GetSetLogEventProperties()
    {
        var activity = Some.Activity();

        var a = Some.Boolean();
        var b = Some.Integer();
        var c = Some.Integer();
        var d = Some.Integer();
        var e = new Dictionary<ScalarValue, LogEventPropertyValue>
        {
            [new ScalarValue("a")] = new ScalarValue(a)
        };

        activity.SetTag("a", a);
        
        // Tags are not included in custom properties
        Assert.False(ActivityInstrumentation.TryGetLogEventPropertyCollection(activity, out var logEventProperties));
        Assert.Null(logEventProperties);
        
        ActivityInstrumentation.SetLogEventProperty(activity, new LogEventProperty("b", new ScalarValue(b)));
        ActivityInstrumentation.SetLogEventProperties(activity, new LogEventProperty("c", new ScalarValue(c)), new LogEventProperty("d", new ScalarValue(d)), new LogEventProperty("e", new DictionaryValue(e)));
        
        // Scalar properties are set as tags
        Assert.Equal(b, activity.GetTagItem("b"));
        Assert.Equal(c, activity.GetTagItem("c"));
        Assert.Equal(d, activity.GetTagItem("d"));
        Assert.IsType<DictionaryValue>(activity.GetTagItem("e"));

        Assert.True(ActivityInstrumentation.TryGetLogEventPropertyCollection(activity, out logEventProperties));
        
        // All set properties are present
        Assert.Equal(b, ((ScalarValue)logEventProperties.Values.First(p => p.Name == "b").Value).Value);
        Assert.Equal(c, ((ScalarValue)logEventProperties.Values.First(p => p.Name == "c").Value).Value);
        Assert.Equal(d, ((ScalarValue)logEventProperties.Values.First(p => p.Name == "d").Value).Value);
        Assert.NotNull(logEventProperties.Values.First(p => p.Name == "e").Value);
    }

    [Fact]
    public void GetSetException()
    {
        var activity = Some.Activity();

        activity.AddEvent(new ActivityEvent("exception"));
        
        Assert.True(ActivityInstrumentation.TryGetException(activity, out _));
        
        Assert.False(ActivityInstrumentation.TrySetException(activity, new Exception("Test Error")));

        activity = Some.Activity();
        
        Assert.False(ActivityInstrumentation.TryGetException(activity, out _));
        
        Assert.True(ActivityInstrumentation.TrySetException(activity, new Exception("Test Error")));
        
        Assert.True(ActivityInstrumentation.TryGetException(activity, out var exception));
        
        Assert.Equal("Test Error", exception.Message);
    }
}