using System;

namespace Lexxys.Testing
{
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
		public static IWeightValuePair<T> Create<T>(double weight, T value) => new WeightValuePair<T>(weight, value);
		public static IWeightValuePair<T> Create<T>(T value) => new WeightValuePair<T>(1, value);
		public static IWeightValuePair<T> Create<T>(double weight, Func<T> value) => new WeightFunctionPair<T>(weight, value);
		public static IWeightValuePair<T> Create<T>(Func<T> value) => new WeightFunctionPair<T>(1, value);
	}

	/// <summary>
	/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
	/// </summary>
	class WeightValuePair<T>: IWeightValuePair<T>
	{
		public double Weight { get; }
		public T Value { get; }

		public WeightValuePair(double weight, T value)
		{
			if (weight <= 0)
				throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
			Weight = weight;
			Value = value;
		}

		private WeightValuePair(double mult, WeightValuePair<T> value)
		{
			if (mult <= 0)
				throw new ArgumentOutOfRangeException(nameof(mult), mult, null);
			Weight = mult * value.Weight;
			Value = value.Value;
		}

		public WeightValuePair<T> Multiply(double value) => value == 1 ? this: new WeightValuePair<T>(value, this);

		IWeightValuePair<T> IWeightValuePair<T>.Multiply(double value) => value == 1 ? this: new WeightValuePair<T>(value, this);
	}


	/// <summary>
	/// Represents <see cref="Weight"/> and <see cref="Value"/> pair.
	/// </summary>
	class WeightFunctionPair<T>: IWeightValuePair<T>
	{
		private readonly Func<T> _func;

		public double Weight { get; }
		public T Value => _func();

		public WeightFunctionPair(double weight, Func<T> generator)
		{
			if (weight <= 0)
				throw new ArgumentOutOfRangeException(nameof(weight), weight, null);

			Weight = weight;
			_func = generator;
		}

		private WeightFunctionPair(double mult, WeightFunctionPair<T> value)
		{
			if (mult <= 0)
				throw new ArgumentOutOfRangeException(nameof(mult), mult, null);
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			Weight = mult * value.Weight;
			_func = value._func;
		}

		public WeightFunctionPair<T> Multiply(double value) => value == 1 ? this: new WeightFunctionPair<T>(value, this);

		IWeightValuePair<T> IWeightValuePair<T>.Multiply(double value) => value == 1 ? this: new WeightFunctionPair<T>(value, this);
	}
}
