#if NETSTANDARD2_0
namespace SerilogTracing.Pollyfill;

static class DeconstructExtensions
{
    public static void Deconstruct<K, V>(this KeyValuePair<K, V> kv, out K key, out V value)
    {
        key = kv.Key;
        value = kv.Value;
    }
}
#endif
