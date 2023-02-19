#if W1
using System;

namespace Lexxys.Testing;

/// <summary>
/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
/// </summary>
public readonly struct WeightValuePair<T>
{
	private readonly Func<T> _generator;
	public double Weight { get; }
	public T Value => _generator();

	public WeightValuePair(double weight, T value)
	{
		if (weight is <= 0 or > 1)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);

		Weight = weight;
		_generator = () => value;
	}

	public WeightValuePair(double weight, Func<T> generator)
	{
		if (weight is <= 0 or > 1)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		if (generator is null)
			throw new ArgumentNullException(nameof(generator));

		Weight = weight;
		_generator = generator;
	}

	public WeightValuePair<T> Multiply(double value)
	{
		if (value <= 0 || (Weight * value) > 1)
			throw new ArgumentOutOfRangeException(nameof(value), value, null);

		return value == 1 ? this : new WeightValuePair<T>(Weight * value, _generator);
	}
}

public static class WeightValuePair
{
	public static WeightValuePair<T> Create<T>(double weight, T value) => new WeightValuePair<T>(weight, value);
	public static WeightValuePair<T> Create<T>(T value) => new WeightValuePair<T>(1, value);
	public static WeightValuePair<T> Create<T>(double weight, Func<T> value) => new WeightValuePair<T>(weight, value);
	public static WeightValuePair<T> Create<T>(Func<T> value) => new WeightValuePair<T>(1, value);
}
#endif