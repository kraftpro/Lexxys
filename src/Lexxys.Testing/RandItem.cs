using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lexxys.Testing;

/// <summary>
/// Represents a random value generator of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of generated elements.</typeparam>
public readonly struct RandItem<T>: IFormattable
{
	/// <summary>
	/// Represents an empty <see cref="RandItem{T}"/>
	/// </summary>
	public static readonly RandItem<T> Empty = new RandItem<T>();

	private readonly object? _value;
	private readonly double _weight;

	#region Constructors

	internal RandItem(RandItem<T>[]? items, bool copy): this(1, items, copy) { }

	internal RandItem(double weight, RandItem<T>[]? items, bool copy)
	{
		if (items is not { Length: >0 }) return;
		if (weight is <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		while (items.Length == 1)
		{
			if (items[0]._value is RandItem<T>[] tmp)
			{
				copy = false;
				items = tmp;
				continue;
			}
			_value = items[0]._value;
			_weight = weight;
			return;
		}
		Debug.Assert(items.Length > 1);
		if (copy)
		{
			var tmp = new RandItem<T>[items.Length];
			Array.Copy(items, tmp, tmp.Length);
			items = tmp;
		}
		_value = items;
		_weight = weight;
	}

	/// <summary>
	/// Creates a clone of the specified <see cref="RandItem{T}"/> with the specified <paramref name="weight"/>.
	/// </summary>
	/// <param name="weight">Weight of the item</param>
	/// <param name="item">The item to be cloned</param>
	public RandItem(double weight, RandItem<T> item)
	{
		if (weight != 0 && !item.IsEmpty)
		{
			_weight = weight;
			_value = item._value;
		}
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> with the only <paramref name="value"/>.
	/// </summary>
	/// <param name="weight">Weight of the item</param>
	/// <param name="value">The item value</param>
	public RandItem(double weight, T value)
	{
		if (weight is <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		_value = value;
		_weight = _value is null ? 0 : weight;
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> that uses <paramref name="generator"/> to create an item value.
	/// </summary>
	/// <param name="weight">Weight of the item</param>
	/// <param name="generator">A function that creates an item value.</param>
	public RandItem(double weight, Func<T> generator)
	{
		if (weight is <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		_value = generator ?? throw new ArgumentNullException(nameof(generator));
		_weight = weight;
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> based om the collection of <see cref="RandItem{T}"/> pairs.
	/// </summary>
	/// <param name="weight">Weight of the item</param>
	/// <param name="items">Collection of <see cref="RandItem{T}"/> to be used to generate item value</param>
	public RandItem(double weight, IEnumerable<RandItem<T>> items) : this(weight, items.ToArray(), false) { }

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> based om the collection of <see cref="RandItem{T}"/> pairs.
	/// </summary>
	/// <param name="weight">Weight of the item</param>
	/// <param name="items">Collection of <see cref="RandItem{T}"/> to be used to generate item value</param>
	public RandItem(double weight, params RandItem<T>[] items): this(weight, items, true) { }

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> of weight 1 with the only <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The item value</param>
	public RandItem(T value) : this(1, value) { }
	
	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> of weight 1 that uses <paramref name="generator"/> to create an item value.
	/// </summary>
	/// <param name="generator">A function that creates an item value.</param>
	public RandItem(Func<T> generator): this(1, generator) { }
	
	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> of weight 1 based on the collection of <see cref="RandItem{T}"/> pairs.
	/// </summary>
	/// <param name="items">Collection of <see cref="RandItem{T}"/> to be used to generate item value</param>
	public RandItem(IEnumerable<RandItem<T>> items): this(items.ToArray(), false) { }

	
	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> of weight 1 based on the collection of <see cref="RandItem{T}"/> pairs.
	/// </summary>
	/// <param name="items">Collection of <see cref="RandItem{T}"/> to be used to generate item value</param>
	public RandItem(params RandItem<T>[] items): this(items, true) { }

	#endregion

	/// <summary>
	/// Weight of the item.
	/// </summary>
	public double Weight => _weight;

	/// <summary>
	/// Generates next item value.
	/// </summary>
	public T Value => NextValue();

	/// <summary>
	/// Returns true if the item has no value.
	/// </summary>
	public bool IsEmpty => _weight == 0;

	/// <summary>
	/// Returns true if the item has only one value.
	/// </summary>
	public bool IsSingle => _value is T;

	/// <summary>
	/// Creates a clone of this <see cref="RandItem{T}"/> with the specified <paramref name="weight"/>.
	/// </summary>
	/// <param name="weight">Weight of the item</param>
	/// <returns></returns>
	public RandItem<T> WithWeight(double weight) => new RandItem<T>(weight, this);

	/// <summary>
	/// Generates nest item value.
	/// </summary>
	/// <returns></returns>
	public T NextValue()
	{
		if (IsEmpty) throw new InvalidOperationException("RandItem is empty");
		if (_value is T value)
			return value;
		if (_value is Func<T> generator)
			return generator();
		var items = Unsafe.As<RandItem<T>[]>(_value);
		Debug.Assert(items is { Length: >0 });
		double total = items.Sum(o => o.Weight);
		double bound = Rand.Dbl(total);
		double sum = 0;
		foreach (var item in items)
		{
			sum += item.Weight;
			if (sum >= bound)
				return item.NextValue();
		}
		throw new InvalidOperationException($"Bound exceeded total: total={total}, bound={bound}");
	}

	/// <summary>
	/// Generates array of items with the specified <paramref name="count"/> of elements.
	/// </summary>
	/// <param name="count">Number of generated elements</param>
	/// <returns></returns>
	public T[] Collect(int count)
	{
		if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

		var result = new T[count];
		for (int i = 0; i < result.Length; i++)
		{
			result[i] = NextValue();
		}
		return result;
	}

	/// <summary>
	/// Infinitive items generator.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<T> Enumerate()
	{
		for (;;) yield return NextValue();
		// ReSharper disable once IteratorNeverReturns
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> joining this random items generator with the specified one.
	/// </summary>
	/// <param name="other">The generator to join with.</param>
	/// <returns>Combined random items generator.</returns>
	public RandItem<T> Or(RandItem<T> other)
	{
		if (other.IsEmpty)
			return this;
		if (IsEmpty)
			return other;
		RandItem<T>[] items;
		var items1 = _value as RandItem<T>[];
		var items2 = other._value as RandItem<T>[];
		if (items1 != null && items2 != null)
		{
			items = new RandItem<T>[items1.Length + items2.Length];
			Array.Copy(items1, items, items1.Length);
			Array.Copy(items2, 0, items, items1.Length, items2.Length);
		}
		else if (items1 != null)
		{
			items = new RandItem<T>[items1.Length + 1];
			Array.Copy(items1, items, items1.Length);
			items[items1.Length] = other;
		}
		else if (items2 != null)
		{
			items = new RandItem<T>[items2.Length + 1];
			items[0] = this;
			Array.Copy(items2, 0, items, 1, items2.Length);
		}
		else
		{
			items = [this, other];
		}
		return new RandItem<T>(_weight + other._weight / 2, items, false);
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> adding a new <paramref name="generator"/> to the current random items generator.
	/// </summary>
	/// <param name="generator">A function that creates an item value.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(Func<T> generator) => Or(1, generator);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> adding a new <paramref name="generator"/> with the specified <paramref name="weight"/> to the current random items generator.
	/// </summary>
	/// <param name="weight">Weight of the generator.</param>
	/// <param name="generator">A function that creates an item value.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(double weight, Func<T> generator) => Or(new RandItem<T>(weight, generator));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> adding a new <paramref name="value"/> to the current random items generator.
	/// </summary>
	/// <param name="value">Value of the item.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(T value) => Or(1, value);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> adding a new <paramref name="value"/> to the current random items generator.
	/// </summary>
	/// <param name="weight">Weight of the value.</param>
	/// <param name="value">Value of the item.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(double weight, T value) => Or(new RandItem<T>(weight, value));

	/// <inheritdoc/>
	public override string ToString() => IsEmpty ? String.Empty: NextValue()?.ToString() ?? String.Empty;

	/// <summary>
	/// Returns a string representation of the current item using the specified <paramref name="format"/> and culture-specific format information.
	/// </summary>
	/// <param name="format">The format to use or <c>null</c>.</param>
	/// <param name="formatProvider">The provider to use to format the value or <c>null</c>.</param>
	/// <returns></returns>
	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		var value = NextValue();
		return value is IFormattable f ? f.ToString(format, formatProvider): value?.ToString() ?? String.Empty;
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> joining two <see cref="RandItem{T}"/>s.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static RandItem<T> operator |(RandItem<T> left, RandItem<T> right) => left.Or(right);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> joining a <see cref="RandItem{T}"/> and a value of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static RandItem<T> operator |(RandItem<T> left, T right) => left.Or(right);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> joining a <see cref="RandItem{T}"/> and a <see cref="Func{T}"/>.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<T> operator |(RandItem<T> left, Func<T> right)
	{
		if (right is null)
			throw new ArgumentNullException(nameof(right));
		return left.Or(right);
	}

	/// <summary>
	/// Returns next value of the random items generator.
	/// </summary>
	/// <param name="value"></param>
	public static implicit operator T(RandItem<T> value) => value.NextValue();

	/// <summary>
	/// Returns a random items generator as a lambda function.
	/// </summary>
	/// <param name="value"></param>
	public static implicit operator Func<T>(RandItem<T> value) => value.NextValue;

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> joining two <see cref="RandItem{T}"/>s.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static RandItem<T> Or(RandItem<T> left, RandItem<T> right) => left.Or(right);
}
