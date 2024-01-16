using System.Diagnostics;

namespace SerilogTracing.Tests.Support;

static class Some
{
    public static string String()
    {
        return $"string-{Guid.NewGuid()}";
    }

    public static int Integer()
    {
        return Interlocked.Increment(ref _integer);
    }

    public static bool Boolean()
    {
        return Integer() % 2 == 0;
    }

    static int _integer = new Random().Next(int.MaxValue / 2);
    
    public static Activity Activity(string? name = null)
    {
        return new Activity(name ?? String());
    }
}