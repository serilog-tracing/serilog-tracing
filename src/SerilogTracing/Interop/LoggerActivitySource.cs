using System.Diagnostics;

namespace SerilogTracing.Interop;

static class LoggerActivitySource
{
    const string Name = "Serilog";

    // ReSharper disable once StaticMemberInGenericType
    public static ActivitySource Instance { get; } = new(Name, null);
}