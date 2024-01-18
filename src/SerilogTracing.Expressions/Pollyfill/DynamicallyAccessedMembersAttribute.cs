#if NETSTANDARD2_0
namespace SerilogTracing.Expressions.Pollyfill;

class DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) : Attribute
{
    public DynamicallyAccessedMemberTypes MemberTypes { get; } = memberTypes;
}

[Flags]
internal enum DynamicallyAccessedMemberTypes
{
    PublicMethods = 0x0008
}
#endif
