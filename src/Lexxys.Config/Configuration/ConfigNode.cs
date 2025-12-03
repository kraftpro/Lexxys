using System.Text;

namespace Lexxys.Configuration;

/// <summary>
/// Represents a node in a configuration structure, which may contain a value, a collection of child nodes, or both.
/// </summary>
/// <remarks>
/// A ConfigNode can represent either a single configuration value, a collection of child nodes, or a
/// combination of both. This struct is immutable and can be used to build hierarchical configuration trees. Two
/// ConfigNode instances are considered equal if both their values and their collections are equal. The struct provides
/// value-based equality and can be used as a key in collections that rely on hashing.
/// </remarks>
public readonly struct ConfigNode: IEquatable<ConfigNode>, IDumpValue
{
	public ConfigNode(string? value) => Value = value;

	public ConfigNode(ConfigNodeCollection collection) => Collection = collection;

	public ConfigNode(string? value, ConfigNodeCollection? collection) => (Value, Collection) = (value, collection);

	public string? Value { get; }

	public ConfigNodeCollection? Collection { get; }

	public string? GetValue() => Value ?? (Collection.IsEmpty ? null: Collection[0].GetValue());

	public bool IsEmpty => String.IsNullOrEmpty(Value) && Collection.IsEmpty;

	public bool Equals(ConfigNode other)
	{
		return Value == other.Value &&
			(Collection.IsEmpty ? other.Collection.IsEmpty: Collection.Equals(other.Collection));
	}

	public override bool Equals(object? obj) => obj is ConfigNode other && Equals(other);

	public override int GetHashCode() => HashCode.Join(Value?.GetHashCode() ?? 0, Collection?.GetHashCode() ?? 0);

	public override string ToString() => Collection is null ? Value ?? String.Empty: ToString(new StringBuilder()).ToString();

	public StringBuilder ToString(StringBuilder text)
	{
		if (Value is null)
			return Collection is null ? text.Append("null"): Collection.ToString(text);

		text.Append(Value);
		return Collection.IsEmpty ? text: Collection.ToString(text.Append(' '));
	}

	public static bool operator ==(ConfigNode left, ConfigNode right) => left.Equals(right);

	public static bool operator !=(ConfigNode left, ConfigNode right) => !left.Equals(right);

	public void DumpContent(IDumpWriter writer)
	{
		if (Collection is null)
			writer.Write(null, Value);
		else
			writer.Write(Value, Collection);
	}
}
