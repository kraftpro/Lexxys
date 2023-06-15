namespace Lexxys.Validation;

public readonly struct ErrorAttrib: IEquatable<ErrorAttrib>
{
	public string Name { get; }
	public object? Value { get; }

	public ErrorAttrib(string name, object? value)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Value = value;
	}

	public override bool Equals(object? obj)
	{
		return obj is ErrorAttrib attrib && Equals(attrib);
	}

	public bool Equals(ErrorAttrib other)
	{
		return Name == other.Name && EqualityComparer<object?>.Default.Equals(Value, other.Value);
	}

	public override int GetHashCode()
	{
		return HashCode.Join(Name?.GetHashCode() ?? 0, Value?.GetHashCode() ?? 0);
	}

	public static bool operator ==(ErrorAttrib left, ErrorAttrib right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ErrorAttrib left, ErrorAttrib right)
	{
		return !(left == right);
	}
}
