using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog.Expressions;
using SerilogTracing.Expressions;

namespace SerilogTracing.Sinks.Zipkin;

class ZipkinNameResolver : NameResolver
{
    readonly StaticMemberNameResolver _zipkinFunctions = new(typeof(ZipkinFunctions));
    readonly TracingNameResolver _tracingNameResolver = new();

    public override bool TryResolveFunctionName(string name, [NotNullWhen(true)] out MethodInfo? implementation)
    {
        return
            _zipkinFunctions.TryResolveFunctionName(name, out implementation) ||
            _tracingNameResolver.TryResolveFunctionName(name, out implementation);
    }

    public override bool TryBindFunctionParameter(ParameterInfo parameter, [NotNullWhen(true)] out object? boundValue)
    {
        return _tracingNameResolver.TryBindFunctionParameter(parameter, out boundValue);
    }

    public override bool TryResolveBuiltInPropertyName(string alias, [NotNullWhen(true)] out string? target)
    {
        return _tracingNameResolver.TryResolveBuiltInPropertyName(alias, out target);
    }
}