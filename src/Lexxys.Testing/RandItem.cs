using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lexxys.Testing;

/// <summary>
/// Represents a weighted random item or generator that can produce values of type <typeparamref name="T"/>. Supports
/// combining multiple items or generators with associated weights to enable weighted random selection.
/// </summary>
/// <remarks>
/// A <see cref="RandItem{T}"/> can represent a single value, a value generator function, or a collection of other
/// <see cref="RandItem{T}"/> instances, each with an associated weight. When generating a value, the selection is made according to
/// the weights of the contained items or generators. <see cref="RandItem{T}"/> supports fluent composition using the Or methods or
/// the '|' operator, allowing complex weighted random generators to be built from simpler components. The struct is
/// immutable and thread-safe for concurrent use.
/// </remarks>
/// <typeparam name="T">The type of value produced by the random item or generator.</typeparam>
public readonly struct RandItem<T>: IFormattable
{
	/// <summary>
	/// Represents an empty random item instance with default values.
	/// </summary>
	/// <remarks>Use this field to represent the absence of a value or as a default placeholder when no valid random
	/// item is available. The properties of this instance are set to their default values for the type
	/// parameter.</remarks>
	public static readonly RandItem<T> Empty = new RandItem<T>();

	private readonly object? _value; // T | Func<T> | RandItem<T>[]
	private readonly double _weight;

	#region Constructors

	internal RandItem(RandItem<T>[]? items, bool copy): this(1, items, copy) { }

	internal RandItem(double weight, RandItem<T>[]? items, bool copy)
	{
		if (items is not { Length: >0 }) return;
		if (weight is <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);

		int len = items.Length;
		int count = 0;
		int first = 0;
		for (int i = len - 1; i >= 0; --i)
		{
			if (!items[i].IsEmpty)
			{
				first = i;
				++count;
			}
		}

		if (count == 0) return; // All items are empty

		if (count == 1)
		{
			_value = items[first]._value;
			_weight = weight;
			return;
		}

		// Create a copy if requested or if there are empty items to remove
		if (copy || count < len)
		{
			var tmp = new RandItem<T>[count];
			if (count == len)
			{
				Array.Copy(items, tmp, len);
			}
			else
			{
				// Remove empty items
				int j = 0;
				for (int i = first; i < len; ++i)
				{
					if (!items[i].IsEmpty)
						tmp[j++] = items[i];
				}
			}
			items = tmp;
		}
		_value = items;
		_weight = weight;
	}

	/// <summary>
	/// Initializes a copy of the specifies <paramref name="item"/> with the specified <paramref name="weight"/>.
	/// </summary>
	/// <param name="weight">The relative weight assigned to this item. Must be greater than 0.</param>
	/// <param name="item">The <see cref="RandItem{T}"/> instance whose value will be used for a new item.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if weight is less than or equal to 0.</exception>
	public RandItem(double weight, RandItem<T> item)
	{
		if (weight is <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		if (item.IsEmpty) return;
		_weight = weight;
		_value = item._value;
	}

	/// <summary>
	/// Initializes a new instance of the RandItem class with the specified weight and value.
	/// </summary>
	/// <param name="weight">The relative weight assigned to this item. Must be greater than 0. Used to influence the probability of selection
	/// in weighted random operations.</param>
	/// <param name="value">The value associated with this item.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when weight is less than or equal to 0.</exception>
	public RandItem(double weight, T value)
	{
		if (weight is <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		_value = value;
		_weight = weight;
	}

	/// <summary>
	/// Initializes a new instance of the RandItem class with the specified weight and value generator.
	/// </summary>
	/// <param name="weight">The relative probability weight assigned to this item. Must be greater than 0.</param>
	/// <param name="generator">A function that generates the value for this item. Cannot be null.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if weight is less than or equal to 0.</exception>
	/// <exception cref="ArgumentNullException">Thrown if generator is null.</exception>
	public RandItem(double weight, Func<T> generator)
	{
		if (weight is <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		_value = generator ?? throw new ArgumentNullException(nameof(generator));
		_weight = weight;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RandItem{T}"/> class with the specified weight and a collection of child items.
	/// </summary>
	/// <param name="weight">The relative probability weight assigned to this item. Must be non-negative.</param>
	/// <param name="items">A collection of <see cref="RandItem{T}"/> instances representing the child items.</param>
	public RandItem(double weight, IEnumerable<RandItem<T>> items): this(weight, items?.ToArray(), false) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="RandItem{T}"/> class with the specified weight and an array of child items.
	/// </summary>
	/// <param name="weight">The relative probability weight assigned to this item. Must be non-negative.</param>
	/// <param name="items">An array of <see cref="RandItem{T}"/> objects representing the child items to associate with this item. Can be empty.</param>
	public RandItem(double weight, params RandItem<T>[] items): this(weight, items, true) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="RandItem{T}"/> class with the specified value and a default weight of 1.
	/// </summary>
	/// <param name="value">The value to associate with this item.</param>
	public RandItem(T value): this(1, value) { }

	/// <summary>
	/// Initializes a new instance of the RandItem class using the specified value generator.
	/// </summary>
	/// <param name="generator">A delegate that generates values of type T when invoked. Cannot be null.</param>
	public RandItem(Func<T> generator): this(1, generator) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="RandItem{T}"/> class using the specified collection of <see cref="RandItem{T}"/> objects.
	/// </summary>
	/// <param name="items">A collection of <see cref="RandItem{T}"/> objects to include in the new instance. Cannot be null.</param>
	public RandItem(IEnumerable<RandItem<T>> items): this(items.ToArray(), false) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="RandItem{T}"/> class that contains the specified items.
	/// </summary>
	/// <param name="items">An array of <see cref="RandItem{T}"/> objects to include in the new instance. Cannot be null.</param>
	public RandItem(params RandItem<T>[] items): this(items, true) { }

	#endregion

	/// <summary>
	/// Gets the weight value associated with the current instance.
	/// </summary>
	public double Weight => _weight;

	/// <summary>
	/// Gets a value indicating whether the current instance has no weight.
	/// </summary>
	public bool IsEmpty => _weight == 0;

	/// <summary>
	/// Gets a value indicating whether the current instance contains a single value of type <typeparamref name="T"/>.
	/// </summary>
	public bool IsSingle => _value is T;

	/// <summary>
	/// Creates a new random item that associates the current value with the specified selection weight.
	/// </summary>
	/// <param name="weight">The relative probability weight to assign to this item. Must be a non-negative value. Higher values increase the
	/// likelihood of selection.</param>
	/// <returns>A new <see cref="RandItem{T}"/> instance containing the current value and the specified weight.</returns>
	public RandItem<T> WithWeight(double weight) => new RandItem<T>(weight, this);

	/// <summary>
	/// Returns a randomly selected value of type <typeparamref name="T"/> from the current RandItem instance, using the configured weights or
	/// value generators.
	/// </summary>
	/// <remarks>If the <see cref="RandItem{T}"/> instance contains multiple weighted items, the selection is performed based on
	/// their relative weights. If the instance holds a value generator, the generator is invoked to produce the result.
	/// </remarks>
	/// <returns>A value of type <typeparamref name="T"/> selected according to the weights or randomization logic defined in the RandItem instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the RandItem instance is empty and no value can be generated.</exception>
	public T NextValue()
	{
		if (IsEmpty) throw new InvalidOperationException("RandItem is empty");

		// Iterative descent into nested arrays to avoid recursion and allocations
		object? obj = _value;
		for (;;)
		{
			if (obj is null) return default!; // null value
			if (obj is T val) return val;
			if (obj is Func<T> gen) return gen();

			var items = Unsafe.As<RandItem<T>[]>(obj).AsSpan();
			Debug.Assert(items.Length > 0);

			int len = items.Length;

			double total = 0;
			for (int i = 0; i < len; i++)
			{
				total += items[i]._weight;
			}
			Debug.Assert(total > 0);

			double bound = Rand.Dbl(total);

			obj = items[len - 1]._value;
			double sum = 0;
			for (int i = 0; i < len; i++)
			{
				sum += items[i]._weight;
				if (sum >= bound)
				{
					obj = items[i]._value;
					break;
				}
			}
		}
	}

	/// <summary>
	/// Generates an array containing the specified number of values produced by the generator.
	/// </summary>
	/// <param name="count">The number of values to generate. Must be greater than or equal to 0.</param>
	/// <returns>An array of type <typeparamref name="T"/> containing the generated values. The array will be empty if count is 0.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if count is less than 0.</exception>
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
	/// Returns an infinite sequence of values generated by repeatedly invoking NextValue().
	/// </summary>
	/// <remarks>Enumeration continues indefinitely. To avoid an infinite loop, use methods such as Take or
	/// implement your own stopping condition when consuming the sequence.</remarks>
	/// <returns>An infinite <see cref="IEnumerable{T}"/> sequence containing values produced by successive calls to NextValue().
	/// The sequence does not terminate unless the enumeration is externally stopped.</returns>
	public IEnumerable<T> Enumerate()
	{
		for (;;) yield return NextValue();
		// ReSharper disable once IteratorNeverReturns
	}

	/// <summary>
	/// Returns a new random item that represents the logical combination of this item and the specified item, such that
	/// either item may be selected when generating a random value.
	/// </summary>
	/// <remarks>Use this method to construct a random choice between two items or groups of items. The resulting
	/// <see cref="RandItem{T}"/> can be used in further combinations or random selection operations. The relative weights of the items
	/// are averaged in the resulting combination.</remarks>
	/// <param name="other">The other random item to combine with this instance.</param>
	/// <returns>A new <see cref="RandItem{T}"/> that includes both this item and the specified item. If either item is empty, the non-empty item
	/// is returned.</returns>
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
	/// Creates a new <see cref="RandItem{T}"/> adding a new <paramref name="generator"/> with a default weight to the current random items generator.
	/// </summary>
	/// <remarks>Use this method to chain multiple generators, each representing a possible outcome in the random
	/// selection. The added generator will have a weight of 1.</remarks>
	/// <param name="generator">A function that generates an item of type <typeparamref name="T"/> to be included as an alternative option. Cannot be null.</param>
	/// <returns>A <see cref="RandItem{T}"/> instance that includes the specified generator as an additional option.</returns>
	public RandItem<T> Or(Func<T> generator) => Or(1, generator);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> adding a new <paramref name="generator"/> with the specified <paramref name="weight"/> to the current random items generator.
	/// </summary>
	/// <param name="weight">Weight of the generator.</param>
	/// <param name="generator">A function that creates an item value.</param>
	/// <returns>New random items generator.</returns>
	public RandItem<T> Or(double weight, Func<T> generator) => Or(new RandItem<T>(weight, generator));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> adding a new <paramref name="value"/> with a default weight to the current random items generator.
	/// </summary>
	/// <remarks>This method is a shorthand for adding an item with a weight of 1. Use this overload when you want
	/// to add an item with equal probability to others added with the same default weight.</remarks>
	/// <param name="value">The item to add to the pool for random selection.</param>
	/// <returns>A new <see cref="RandItem{T}"/> instance that includes the specified item as an additional option.</returns>
	public RandItem<T> Or(T value) => Or(1, value);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> adding a new <paramref name="value"/> with the specified <paramref name="weight"/> to the current random items generator.
	/// </summary>
	/// <param name="weight">The relative probability weight assigned to the new item. Must be a non-negative value. Higher weights increase the
	/// likelihood of the item being selected.</param>
	/// <param name="value">The value to associate with the new random item.</param>
	/// <returns>A new <see cref="RandItem{T}"/> instance that includes the specified item with the given weight as an additional option.</returns>
	public RandItem<T> Or(double weight, T value) => Or(new RandItem<T>(weight, value));

	/// <inheritdoc/>
	public override string ToString() => IsEmpty ? String.Empty: NextValue()?.ToString() ?? String.Empty;

	/// <summary>
	/// Returns a string representation of the next value, using the specified format and format provider if supported.
	/// </summary>
	/// <param name="format">A standard or custom format string that defines the format of the returned value. If null or empty, the default
	/// format is used.</param>
	/// <param name="formatProvider">An object that supplies culture-specific formatting information. If null, the current culture is used.</param>
	/// <returns>A string representation of the next value, formatted as specified. If the value does not support formatting, its
	/// default string representation is returned.</returns>
	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		var value = NextValue();
		return value is IFormattable f ? f.ToString(format, formatProvider): value?.ToString() ?? String.Empty;
	}

	/// <summary>
	/// Combines two random item generators using a logical OR operation, producing a new generator that yields items from
	/// either source.
	/// </summary>
	/// <remarks>The resulting generator selects items from both sources according to their respective probabilities
	/// or selection logic. Use this operator to create composite random item distributions.</remarks>
	/// <param name="left">The first random item generator to combine.</param>
	/// <param name="right">The second random item generator to combine.</param>
	/// <returns>A new <see cref="RandItem{T}"/> generator that produces items from either the left or right generator.</returns>
	public static RandItem<T> operator |(RandItem<T> left, RandItem<T> right) => left.Or(right);

	/// <summary>
	/// Combines a random item with a value using the bitwise OR operator.
	/// </summary>
	/// <param name="left">The random item to combine.</param>
	/// <param name="right">The value to combine with the random item.</param>
	/// <returns>A new <see cref="RandItem{T}"/> that includes both the original random item and the specified value.</returns>
	public static RandItem<T> operator |(RandItem<T> left, T right) => left.Or(right);

	/// <summary>
	/// Combines the current random item with an additional value generator, producing a new random item that can yield
	/// values from either source.
	/// </summary>
	/// <remarks>This operator enables fluent composition of random items and value generators, allowing for
	/// flexible construction of random value sources.</remarks>
	/// <param name="left">The existing random item to combine.</param>
	/// <param name="right">A function that generates a value of type T to be included as an alternative outcome. Cannot be null.</param>
	/// <returns>A new <see cref="RandItem{T}"/> that represents a random choice between the original item and the value produced by the specified
	/// function.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="right"/> is null.</exception>
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
}
