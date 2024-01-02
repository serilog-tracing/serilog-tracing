using System.Diagnostics;

namespace SerilogTracing.Interop;

static class LoggerActivitySource<T>
{
    static readonly string Name = typeof(T).FullName ?? "Serilog";

    // ReSharper disable once StaticMemberInGenericType
    public static ActivitySource Instance { get; } = new(Name, null);
}