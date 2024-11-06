using System.Diagnostics;
using SerilogTracing.Core;

namespace SerilogTracing.Instrumentation;

class ReplacementActivityListener
{
    internal static ReplacementActivityListener Instance { get; } = new();

    internal ReplacementActivityListener()
    {
        _listener = new ActivityListener();
        _listener.ShouldListenTo = _ => true;
        _listener.ActivityStopped += activity =>
        {
            if (ActivityInstrumentation.TryGetReplacementActivity(activity, out var replacement))
            {
                replacement.Stop();
            }
        };
        
        ActivitySource.AddActivityListener(_listener);
    }

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    readonly ActivityListener _listener;
}
