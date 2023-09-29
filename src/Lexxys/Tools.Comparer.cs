using System.Collections;

namespace Lexxys;

public static class Comparer
{
	/// <summary>
	/// Creates a new <see cref="IComparer"/> instance using the specified <paramref name="compare"/> function.
	/// </summary>
	/// <param name="compare">The function to compare two objects.</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IComparer<T> Create<T>(Func<T, T, int> compare)
		=> new GenericComparer<T>(compare ?? throw new ArgumentNullException(nameof(compare)));

	/// <summary>
	/// Creates a new <see cref="IComparer"/> instance using the specified <paramref name="compare"/> function.
	/// </summary>
	/// <param name="compare">The function to compare two objects.</param>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IComparer Create<T1, T2>(Func<T1, T2, int> compare)
		=> new GenericComparer<T1, T2>(compare ?? throw new ArgumentNullException(nameof(compare)));

	/// <summary>
	/// Creates a new <see cref="IEqualityComparer"/> instance using the specified <paramref name="compare"/> function.
	/// </summary>
	/// <param name="compare">The function to compare two objects.</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IEqualityComparer<T> Create<T>(Func<T, T, bool> compare)
		=> new GenericEqualityComparer<T>(compare ?? throw new ArgumentNullException(nameof(compare)));

	/// <summary>
	/// Creates a new <see cref="IEqualityComparer"/> instance using the specified <paramref name="compare"/> and <paramref name="hash"/> functions.
	/// </summary>
	/// <param name="compare">The function to compare two objects.</param>
	/// <param name="hash">The function to calculate hash code of an object.</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IEqualityComparer<T> Create<T>(Func<T, T, bool> compare, Func<T, int> hash)
		=> new GenericEqualityComparer<T>(compare ?? throw new ArgumentNullException(nameof(compare)), hash ?? throw new ArgumentNullException(nameof(hash)));

	/// <summary>
	/// Check equality of the two collection of objects of the same type using the specified <paramref name="comparer"/> function.
	/// </summary>
	/// <param name="left">First collection to compare</param>
	/// <param name="right">Second collection to compare</param>
	/// <param name="comparer">The function to compare two objects.</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static bool Equals<T>(IEnumerable<T>? left, IEnumerable<T>? right, IEqualityComparer<T>? comparer = null)
		=> left is null || right is null ? Object.ReferenceEquals(left, right): left.SequenceEqual(right, comparer);

	/// <summary>
	/// Check equality of the two bytes array.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool Equals(byte[]? left, byte[]? right)
		=> ((ReadOnlySpan<byte>)left).SequenceEqual(right);

	#region Internal classes

	private class GenericComparer<T1, T2>: IComparer
	{
		private readonly Func<T1, T2, int> _compare;

		public GenericComparer(Func<T1, T2, int> compare) => _compare = compare;

		public int Compare(object? x, object? y) => x is T1 t1 && y is T2 t2 ? _compare(t1, t2) : 2;
	}

	private class GenericComparer<T>: IComparer<T>
	{
		private readonly Func<T, T, int> _compare;

		public GenericComparer(Func<T, T, int> compare) => _compare = compare;

		public int Compare(T? x, T? y) => x is null ? (y is null ? 0 : 1) : y is null ? -1 : _compare(x, y);
	}

	private class GenericEqualityComparer<T>: IEqualityComparer<T>
	{
		private readonly Func<T, T, bool> _equals;
		private readonly Func<T, int> _hash;

		public GenericEqualityComparer(Func<T, T, bool> equals)
			=> (_equals, _hash) = (equals, o => o?.GetHashCode() ?? 0);

		public GenericEqualityComparer(Func<T, T, bool> equals, Func<T, int> hash)
			=> (_equals, _hash) = (equals, hash);

		public bool Equals(T? x, T? y) => x is null ? y is null : y is not null && _equals(x, y);

		public int GetHashCode(T obj) => _hash(obj);
	}

	#endregion
}