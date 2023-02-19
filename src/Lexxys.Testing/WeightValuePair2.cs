#if !W1
using System;

namespace Lexxys.Testing;

/// <summary>
/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
/// </summary>
public interface WeightValuePair<out T>
{
	T Value { get; }
	double Weight { get; }

	WeightValuePair<T> Multiply(double weight);
}

public static class WeightValuePair
{
	public static WeightValuePair<T> Create<T>(double weight, T value) => new WeightValuePairInternal<T>(weight, value);
	public static WeightValuePair<T> Create<T>(T value) => new WeightValuePairInternal<T>(1, value);
	public static WeightValuePair<T> Create<T>(double weight, Func<T> value) => new WeightFunctionPairInternal<T>(weight, value);
	public static WeightValuePair<T> Create<T>(Func<T> value) => new WeightFunctionPairInternal<T>(1, value);
}

/// <summary>
/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
/// </summary>
class WeightValuePairInternal<T>: WeightValuePair<T>
{
	public double Weight { get; }
	public T Value { get; }

	public WeightValuePairInternal(double weight, T value)
	{
		if (weight <= 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		Weight = weight;
		Value = value;
	}

	private WeightValuePairInternal(double mult, WeightValuePairInternal<T> value)
	{
		if (mult <= 0)
			throw new ArgumentOutOfRangeException(nameof(mult), mult, null);
		Weight = mult * value.Weight;
		Value = value.Value;
	}

	public WeightValuePairInternal<T> Multiply(double value) => value == 1 ? this: new WeightValuePairInternal<T>(value, this);

	WeightValuePair<T> WeightValuePair<T>.Multiply(double value) => value == 1 ? this: new WeightValuePairInternal<T>(value, this);
}


/// <summary>
/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
/// </summary>
class WeightFunctionPairInternal<T>: WeightValuePair<T>
{
	private readonly Func<T> _func;

	public double Weight { get; }
	public T Value => _func();

	public WeightFunctionPairInternal(double weight, Func<T> generator)
	{
		if (weight <= 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);

		Weight = weight;
		_func = generator;
	}

	private WeightFunctionPairInternal(double mult, WeightFunctionPairInternal<T> value)
	{
		if (mult <= 0)
			throw new ArgumentOutOfRangeException(nameof(mult), mult, null);
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		Weight = mult * value.Weight;
		_func = value._func;
	}

	public WeightFunctionPairInternal<T> Multiply(double value) => value == 1 ? this: new WeightFunctionPairInternal<T>(value, this);

	WeightValuePair<T> WeightValuePair<T>.Multiply(double value) => value == 1 ? this: new WeightFunctionPairInternal<T>(value, this);
}
#endif