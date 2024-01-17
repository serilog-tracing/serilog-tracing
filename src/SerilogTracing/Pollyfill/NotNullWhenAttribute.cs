#if NETSTANDARD2_0
namespace SerilogTracing.Pollyfill;

sealed class NotNullWhenAttribute: Attribute
{
    public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

    public bool ReturnValue { get; }
}
#endif
