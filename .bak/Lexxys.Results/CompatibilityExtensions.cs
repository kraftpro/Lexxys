#if !NET

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


internal static class Extensions
{
	public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
	{
		if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));
		if (dictionary.ContainsKey(key)) return false;
		dictionary.Add(key, value);
		return true;
	}
}
#endif
