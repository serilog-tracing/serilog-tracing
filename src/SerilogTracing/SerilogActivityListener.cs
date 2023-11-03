using System.Diagnostics;

namespace SerilogTracing;

public sealed class SerilogActivityListener: IDisposable
{
    readonly ActivityListener _listener;

    internal SerilogActivityListener(ActivityListener listener)
    {
        _listener = listener;
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}