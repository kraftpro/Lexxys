#if !W1
using System;

namespace Lexxys.Testing;

/// <summary>
/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
/// </summary>
public interface IWeightValuePair<out T>
{
	T Value { get; }
	double Weight { get; }

	IWeightValuePair<T> Multiply(double weight);
}

public static class WeightValuePair
{
	public static IWeightValuePair<T> Create<T>(double weight, T value) => new WeightValuePairInternal<T>(weight, value);
	public static IWeightValuePair<T> Create<T>(T value) => new WeightValuePairInternal<T>(1, value);
	public static IWeightValuePair<T> Create<T>(double weight, Func<T> value) => new WeightFunctionPairInternal<T>(weight, value);
	public static IWeightValuePair<T> Create<T>(Func<T> value) => new WeightFunctionPairInternal<T>(1, value);
}

/// <summary>
/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
/// </summary>
class WeightValuePairInternal<T>: IWeightValuePair<T>
{
	public double Weight { get; }
	public T Value { get; }

	public WeightValuePairInternal(double weight, T value)
	{
		if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);

		Weight = weight;
		Value = value;
	}

	private WeightValuePairInternal(double multiplier, WeightValuePairInternal<T> value)
	{
		if (multiplier <= 0) throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, null);

		Weight = multiplier * value.Weight;
		Value = value.Value;
	}

	public WeightValuePairInternal<T> Multiply(double value) => value == 1.0 ? this: new WeightValuePairInternal<T>(value, this);

	IWeightValuePair<T> IWeightValuePair<T>.Multiply(double value) => value == 1.0 ? this: new WeightValuePairInternal<T>(value, this);
}


/// <summary>
/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
/// </summary>
class WeightFunctionPairInternal<T>: IWeightValuePair<T>
{
	private readonly Func<T> _func;

	public double Weight { get; }
	public T Value => _func();

	public WeightFunctionPairInternal(double weight, Func<T> generator)
	{
		if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		if (generator is null) throw new ArgumentNullException(nameof(generator));

		Weight = weight;
		_func = generator;
	}

	private WeightFunctionPairInternal(double multiplier, WeightFunctionPairInternal<T> value)
	{
		if (multiplier <= 0) throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, null);
		if (value == null) throw new ArgumentNullException(nameof(value));

		Weight = multiplier * value.Weight;
		_func = value._func;
	}

	public WeightFunctionPairInternal<T> Multiply(double value) => value == 1.0 ? this: new WeightFunctionPairInternal<T>(value, this);

	IWeightValuePair<T> IWeightValuePair<T>.Multiply(double value) => value == 1.0 ? this: new WeightFunctionPairInternal<T>(value, this);
}
#endif