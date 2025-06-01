#if !NETCOREAPP

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class NotNullAttribute: Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
internal sealed class MaybeNullAttribute: Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class DoesNotReturnIfAttribute(bool parameterValue): Attribute
{
	public bool ParameterValue { get; } = parameterValue;
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class NotNullWhenAttribute(bool returnValue): Attribute
{
	public bool ReturnValue { get; } = returnValue;
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
internal sealed class NotNullIfNotNullAttribute(string parameterName): Attribute
{
	public string ParameterName { get; } = parameterName;
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class MaybeNullWhenAttribute(bool returnValue): Attribute
{
	public bool ReturnValue { get; } = returnValue;
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class DoesNotReturnAttribute: Attribute
{
}

#endif
