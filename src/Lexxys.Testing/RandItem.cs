using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lexxys.Testing;

/// <summary>
/// Represents a generator of random items of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of generated elements.</typeparam>
public readonly struct RandItem<T>: IFormattable
{
	/// <summary>
	/// Represents an empty <see cref="RandItem{T}"/>
	/// </summary>
	public static readonly RandItem<T> Empty = new RandItem<T>(Array.Empty<IWeightValuePair<T>>(), false);

	private readonly IWeightValuePair<T>[] _items;

	private RandItem(IWeightValuePair<T>[] items, bool _)
	{
		_items = items;
		Weight = _items.Sum(o => o.Weight);
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> of weight 1, that uses <paramref name="generator"/> to create an item value.
	/// </summary>
	/// <param name="generator">A function that creates an item value</param>
	public RandItem(Func<T> generator): this(1, generator)
	{
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> of the specified <paramref name="weight"/>, that uses <paramref name="generator"/> to create an item value.
	/// </summary>
	/// <param name="weight"></param>
	/// <param name="generator"></param>
	public RandItem(double weight, Func<T> generator)
	{
		if (weight < 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		if (generator == null)
			throw new ArgumentNullException(nameof(generator));

		_items = new[] { WeightValuePair.Create(weight, generator) };
		Weight = weight;
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> of specified <paramref name="weight"/> and a constant <paramref name="value"/>.
	/// </summary>
	/// <param name="weight">Item weight</param>
	/// <param name="value">Item value</param>
	public RandItem(double weight, T value)
	{
		if (weight < 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);

		_items = new[] { WeightValuePair.Create(weight, value) };
		Weight = weight;
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> based of the specified list of <paramref name="items"/>.
	/// </summary>
	/// <param name="items">Items collection to be used to generate item value</param>
	public RandItem(IEnumerable<IWeightValuePair<T>> items)
	{
		if (items == null)
			throw new ArgumentNullException(nameof(items));

		_items = items.ToArray();
		Weight = _items.Sum(o => o.Weight);
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> based of the specifies list of <paramref name="items"/>.
	/// </summary>
	/// <param name="items">Items collection to be used to generate item value</param>
	public RandItem(params IWeightValuePair<T>[] items)
	{
		if (items is not { Length: >0 })
			throw new ArgumentNullException(nameof(items));

		_items = new IWeightValuePair<T>[items.Length];
		Array.Copy(items, _items, _items.Length);
		Weight = _items.Sum(o => o.Weight);
	}

	/// <summary>
	/// Item Weight.
	/// </summary>
	private double Weight { get; }

	/// <summary>
	/// Generates nest item value.
	/// </summary>
	/// <returns></returns>
	public T NextValue()
	{
		if (_items.Length == 0)
			throw new InvalidOperationException("Collection is empty");

		if (_items.Length == 1)
			return _items[0].Value;
		double bound = Rand.Dbl(Weight);
		double p = 0;
		for (int i = 0; i < _items.Length; i++)
		{
			p += _items[i].Weight;
			if (p >= bound)
				return _items[i].Value;
		}
		throw new InvalidOperationException($"Bound exceeded bound:{Weight}, bound:{bound}");
	}

	/// <summary>
	/// Generates array of items with the specified <paramref name="count"/> of elements.
	/// </summary>
	/// <param name="count">Number of generated elements</param>
	/// <returns></returns>
	public T[] Collect(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));
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
		for (;;)
			yield return NextValue();
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> by multiplying all items by specific <paramref name="value"/>
	/// </summary>
	/// <param name="value">Multiplier</param>
	/// <returns>Modified <see cref="RandItem{T}"/>.</returns>
	public RandItem<T> Mult(double value)
	{
		if (value <= 0.0)
			throw new ArgumentOutOfRangeException(nameof(value), value, null);
		if (value == 1.0)
			return this;

		var items = Array.ConvertAll(_items, o => o.Multiply(value));
		return new RandItem<T>(items, false);
	}

	/// <summary>
	/// Joins two random items generators.
	/// </summary>
	/// <param name="other">The generator to join with.</param>
	/// <returns>Combined random items generator.</returns>
	public RandItem<T> Or(RandItem<T> other)
	{
		if (other._items.Length == 0)
			return this;
		if (_items.Length == 0)
			return other;
		var items = new IWeightValuePair<T>[_items.Length + other._items.Length];
		if (_items.Length == 1)
			items[0] = _items[0];
		else
			Array.Copy(_items, items, _items.Length);
		if (other._items.Length == 1)
			items[_items.Length] = other._items[0];
		else
			Array.Copy(other._items, 0, items, _items.Length, other._items.Length);
		return new RandItem<T>(items, false);
	}

	/// <summary>
	/// Appends a new <paramref name="generator"/> to the current random items generator.
	/// </summary>
	/// <param name="generator">Item value generator.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(Func<T> generator) => Or(1, generator);

	/// <summary>
	/// Appends a new <paramref name="generator"/> to the current random items generator.
	/// </summary>
	/// <param name="weight">Weight of the generator.</param>
	/// <param name="generator">Item value generator.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(double weight, Func<T> generator) => Or(WeightValuePair.Create(weight, generator));

	/// <summary>
	/// Appends a new <paramref name="value"/> to the current random items generator.
	/// </summary>
	/// <param name="value">Value of the item.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(T value) => Or(1, value);

	/// <summary>
	/// Appends a new <paramref name="value"/> to the current random items generator.
	/// </summary>
	/// <param name="weight">Weithg of the value.</param>
	/// <param name="value">Value of the item.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(double weight, T value) => Or(WeightValuePair.Create(weight, value));

	/// <summary>
	/// Appends a new item to the current random items generator.
	/// </summary>
	/// <param name="pair">Item value</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(IWeightValuePair<T> pair)
	{
		if (pair.Weight <= 0)
			return this;
		if (_items.Length == 0)
			return new RandItem<T>( new[] { pair }, false);
		var items = new IWeightValuePair<T>[_items.Length + 1];
		if (_items.Length == 1)
			items[0] = _items[0];
		else if (_items.Length > 0)
			Array.Copy(_items, items, _items.Length);
		items[_items.Length] = pair;
		return new RandItem<T>(items, false);
	}

	/// <inheritdoc/>
	public override string ToString() => NextValue()?.ToString() ?? String.Empty;

	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		var value = NextValue();
		return value is IFormattable f ? f.ToString(format, formatProvider): value?.ToString() ?? String.Empty;
	}

    public static RandItem<T> operator *(double mult, RandItem<T> value) => value.Mult(mult);

    public static RandItem<T> operator *(RandItem<T> value, double mult) => value.Mult(mult);

    public static RandItem<T> operator +(RandItem<T> left, RandItem<T> right) => left.Or(right);

    public static RandItem<T> operator +(RandItem<T> left, T right) => left.Or(right);

    public static RandItem<T> operator +(RandItem<T> left, Func<T> right)
	{
		if (right is null)
			throw new ArgumentNullException(nameof(right));
		return left.Or(right);
	}

	/// <summary>
	/// Creates a sequence from two <see cref="RandItem{T}"/>s.
	/// </summary>
	/// <param name="left">First element of the sequence.</param>
	/// <param name="right">Second element of the sequence.</param>
	/// <returns></returns>
	public static RandSeq<T> operator |(RandItem<T> left, RandItem<T> right) => new RandSeq<T>(left, right);

	/// <summary>
	/// Creates a sequence of <see cref="RandItem{T}"/> and the value.
	/// </summary>
	/// <param name="left">First element of the sequence.</param>
	/// <param name="right">Second element of the sequence.</param>
	/// <returns></returns>
	public static RandSeq<T> operator |(RandItem<T> left, T right) => new RandSeq<T>(left, new RandItem<T>(1, right));

	/// <summary>
	/// Creates a sequence of the value and <see cref="RandItem{T}"/>.
	/// </summary>
	/// <param name="left">First element of the sequence.</param>
	/// <param name="right">Second element of the sequence.</param>
	/// <returns></returns>
	public static RandSeq<T> operator |(T left, RandItem<T> right) => new RandSeq<T>(new RandItem<T>(1, left), right);

	/// <summary>
	/// Returns next value of the random items generator.
	/// </summary>
	/// <param name="value"></param>
    [return: MaybeNull]
	public static implicit operator T(RandItem<T> value) => value.NextValue();

	/// <summary>
	/// Returns a random items generator as a lambda function.
	/// </summary>
	/// <param name="value"></param>
	[return: MaybeNull]
	public static implicit operator Func<T>(RandItem<T> value) => value.NextValue;
}
