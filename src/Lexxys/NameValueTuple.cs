namespace Lexxys;

/// <summary>
/// Represents a tuple with a name and a value.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <param name="name">The name component of the tuple.</param>
/// <param name="value">The value component of the tuple.</param>
public readonly struct NameValueTuple<TName, TValue>(TName name, TValue value): IEquatable<NameValueTuple<TName, TValue>>
{
	/// <summary>
	/// Gets the name component of the tuple.
	/// </summary>
	public TName Name { get; } = name;

	/// <summary>
	/// Gets the value component of the tuple.
	/// </summary>
	public TValue Value { get; } = value;

    public override string ToString() => $"[{Name}, {Value}]";

    public static explicit operator (TName Name, TValue Value)(NameValueTuple<TName, TValue> item) => (item.Name, item.Value);

	public static explicit operator KeyValuePair<TName, TValue>(NameValueTuple<TName, TValue> item) => new KeyValuePair<TName, TValue>(item.Name, item.Value);

	public static implicit operator NameValueTuple<TName, TValue>((TName Name, TValue Value) item) => new NameValueTuple<TName, TValue>(item.Name, item.Value);

	public static implicit operator NameValueTuple<TName, TValue>(KeyValuePair<TName, TValue> item) => new NameValueTuple<TName, TValue>(item.Key, item.Value);

    public static bool operator ==(NameValueTuple<TName, TValue> left, NameValueTuple<TName, TValue> right) => left.Equals(right);

    public static bool operator !=(NameValueTuple<TName, TValue> left, NameValueTuple<TName, TValue> right) => !(left == right);

    public void Deconstruct(out TName name, out TValue value)
	{
		name = Name;
		value = Value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj) => obj is NameValueTuple<TName, TValue> tuple && Equals(tuple);

	public bool Equals(NameValueTuple<TName, TValue> other) =>
		EqualityComparer<TName>.Default.Equals(Name, other.Name) &&
		EqualityComparer<TValue>.Default.Equals(Value, other.Value);

	public override int GetHashCode() => HashCode.Join(Name?.GetHashCode() ?? 0, Value?.GetHashCode() ?? 0);
}
