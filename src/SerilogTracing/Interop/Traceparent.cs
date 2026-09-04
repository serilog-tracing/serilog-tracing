using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SerilogTracing.Interop;

static class Traceparent
{
    // {ver}-{traceid}-{spanid}-{flags}
    // 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
    public static string Format(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags flags)
    {
#if FEATURE_STRING_CREATE
        // This is an optimized activity context to traceparent formatter for recent versions of .NET
        // It follows the same basic approach as others do, but pre-allocates and elides bounds checks.

        // SAFETY: The string is pre-allocated to 55 UTF16 chars (110 bytes), trace id is guaranteed to be 32 chars,
        // and span id is guaranteed to be 16 chars. Transmuting char* to byte* is valid.
        return string.Create(55, (traceId, spanId, flags), static (span, state) =>
        {
            ref var dst = ref MemoryMarshal.GetReference(span);
            
            // We don't expect these to change
            Debug.Assert(
                span.Length == 55 &&
                state.traceId.ToHexString().Length == 32 &&
                state.spanId.ToHexString().Length == 16);

            // Version
            Unsafe.Add(ref dst, 0) = '0';
            Unsafe.Add(ref dst, 1) = '0';
            
            Unsafe.Add(ref dst, 2) = '-';
            
            // Trace id
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<char, byte>(ref Unsafe.Add(ref dst, 3)),
                ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(state.traceId.ToHexString().AsSpan())),
                64);
            
            Unsafe.Add(ref dst, 35) = '-';
            
            // Span id
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<char, byte>(ref Unsafe.Add(ref dst, 36)),
                ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(state.spanId.ToHexString().AsSpan())),
                32);
            
            Unsafe.Add(ref dst, 52) = '-';
            
            // Flags
            Unsafe.Add(ref dst, 53) = NibbleToHex((byte)state.flags >> 4);
            Unsafe.Add(ref dst, 54) = NibbleToHex((byte)state.flags & 0x0F);
        });

        static char NibbleToHex(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));
#else
        return $"00-{traceId.ToHexString()}-{spanId.ToHexString()}-{(byte)flags:x2}";
#endif
    }
}
