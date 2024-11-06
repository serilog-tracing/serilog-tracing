using System.Diagnostics;
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

class ReplacementActivityListener
{
    internal static ReplacementActivityListener Instance { get; } = new();

    internal ReplacementActivityListener()
    {
        _listener = new ActivityListener();
        _listener.ShouldListenTo = source => source.Name == Constants.ReplacementActivitySourceName;
        _listener.ActivityStopped += activity =>
        {
            if (!ActivityInstrumentation.TryGetReplacedActivity(activity, out var replaced)) return;
            
            Activity.Current = replaced;
            replaced.Stop();
        };
    }

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    readonly ActivityListener _listener;
}
