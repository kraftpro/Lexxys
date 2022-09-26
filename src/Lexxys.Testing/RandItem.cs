using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys.Testing;

public class RandItem<T>: IFormattable
{
	/// <summary>
	/// Represents an empty <see cref="RandItem{T}"/>
	/// </summary>
	public static readonly RandItem<T> Empty = new RandItem<T>(Array.Empty<IWeightValuePair<T>>(), false);

	private readonly IWeightValuePair<T>[] _items;

	private RandItem(IWeightValuePair<T>[] items, bool _)
	{
		_items = items ?? throw new ArgumentNullException(nameof(items));
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
		_items = new IWeightValuePair<T>[] { new WeightFunctionPair<T>(weight, generator) };
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
		_items = new IWeightValuePair<T>[] { new WeightValuePair<T>(weight, value) };
		Weight = weight;
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> based of the specifies list of <paramref name="items"/>.
	/// </summary>
	/// <param name="items">Items collection to be used to generate item value</param>
	public RandItem(IEnumerable<IWeightValuePair<T>> items)
	{
		_items = items.ToArray();
		Weight = _items.Sum(o => o.Weight);
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> based of the specifies list of <paramref name="items"/>.
	/// </summary>
	/// <param name="items">Items collection to be used to generate item value</param>
	public RandItem(params IWeightValuePair<T>[] items)
	{
		if (items == null)
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
		if (_items.Length == 1)
			return _items[0].Value;
		if (_items.Length == 0)
			return default;
		double bound = Weight;
		double p = Rand.Dbl(bound);
		double w = 0;
		for (int i = 0; i < _items.Length; i++)
		{
			w += _items[i].Weight;
			if (p <= w)
				return _items[i].Value;
		}
		throw new InvalidOperationException($"Bound exceeded bound:{bound}, p:{p}");
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

	public IEnumerable<T> Enumerate()
	{
		for (;;)
			yield return NextValue();
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> by multiplying all items by specific <paramref name="value"/>
	/// </summary>
	/// <param name="value">Multiplier</param>
	/// <returns></returns>
	public RandItem<T> Mult(double value)
	{
		if (value <= 0)
			throw new ArgumentOutOfRangeException(nameof(value), value, null);
		if (value == 1)
			return this;

		var items = Array.ConvertAll(_items, o => o.Multiply(value));
		return new RandItem<T>(items, false);
	}

	public RandItem<T> Or(RandItem<T> other)
	{
		if (other == null)
			throw new ArgumentNullException(nameof(other));
		if (_items.Length == 0)
			return other;
		if (other._items.Length == 0)
			return this;
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

	public RandItem<T> Or(Func<T> generator) => Or(1, generator);

	public RandItem<T> Or(double weight, Func<T> generator) => Or(new WeightFunctionPair<T>(weight, generator));

	public RandItem<T> Or(T value) => Or(1, value);

	public RandItem<T> Or(double weight, T value) => Or(new WeightValuePair<T>(weight, value));

	public RandItem<T> Or(IWeightValuePair<T> pair)
	{
		if (pair == null)
			throw new ArgumentNullException(nameof(pair));
		if (pair.Weight <= 0)
			return this;
		var items = new IWeightValuePair<T>[_items.Length + 1];
		if (_items.Length == 1)
			items[0] = _items[0];
		else if (_items.Length > 0)
			Array.Copy(_items, items, _items.Length);
		items[_items.Length] = pair;
		return new RandItem<T>(items, false);
	}

	public override string ToString()
	{
		return NextValue()?.ToString() ?? String.Empty;
	}

	public string ToString(string format, IFormatProvider formatProvider)
	{
		T value = NextValue();
		if (value == null)
			return String.Empty;
		return value is IFormattable f ? f.ToString(format, formatProvider): value.ToString();
	}

	public static RandItem<T> operator *(double mult, RandItem<T> value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		return value.Mult(mult);
	}

	public static RandItem<T> operator *(RandItem<T> value, double mult)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		return value.Mult(mult);
	}

	public static RandItem<T> operator +(RandItem<T> left, RandItem<T> right)
	{
		return right == null ? left:
			left == null ? right: left.Or(right);
	}

	public static RandItem<T> operator +(RandItem<T> left, T right)
	{
		return right == null ? left:
			left == null ? new RandItem<T>(1, right): left.Or(right);
	}

	public static RandItem<T> operator +(RandItem<T> left, Func<T> right)
	{
		return right == null ? left:
			left == null ? new RandItem<T>(right): left.Or(right);
	}

	public static RandSeq<T> operator |(RandItem<T> left, RandItem<T> right)
	{
		return right == null && left == null ? null: new RandSeq<T>(left, right);
	}

	public static RandSeq<T> operator |(RandItem<T> left, T right)
	{
		return right == null && left == null ? null: new RandSeq<T>(left, new RandItem<T>(1, right));
	}

	public static RandSeq<T> operator |(T left, RandItem<T> right)
	{
		return right == null && left == null ? null: new RandSeq<T>(new RandItem<T>(1, left), right);
	}

	public static implicit operator T(RandItem<T> value)
	{
		return value == null ? default: value.NextValue();
	}

	public static implicit operator Func<T>(RandItem<T> value)
	{
		return value == null ? () => default: value.NextValue;
	}
}
