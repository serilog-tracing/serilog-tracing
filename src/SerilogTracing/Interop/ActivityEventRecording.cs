namespace SerilogTracing.Interop;

[Flags]
enum ActivityEventRecording
{
    None,
    AsLogEvents
    // `AsEventsArray` not currently supported, but an option we'd consider.
}
