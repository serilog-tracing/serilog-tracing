namespace SerilogTracing.Interop;

class TextException(
    string? message,
    string? type,
    string? toString) : Exception(message ?? type)
{
    public override string ToString() => toString ?? "No information available.";
}
