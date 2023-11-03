using System.Diagnostics;

namespace SerilogTracing;

static class SerilogActivitySource<T>
{
    static readonly string Name = typeof(T).FullName ?? "Serilog";

    // ReSharper disable once StaticMemberInGenericType
    public static ActivitySource Instance { get; } = new(Name, null);
}