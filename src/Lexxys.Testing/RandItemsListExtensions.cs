namespace Lexxys.Testing;

/// <summary>
/// Extension methods for collections of the random generators.
/// </summary>
public static class RandItemsListExtensions
{
	/// <summary>
	/// Collects the random values from the collection.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection">Collection of the random value generators.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static T[] Collect<T>(this IReadOnlyCollection<RandItem<T>> collection)
	{
		if (collection == null) throw new ArgumentNullException(nameof(collection));

		return Collect(collection, collection.Count);
	}

	/// <summary>
	/// Collects the specified number of random values from the collection.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection">Collection of the random value generators.</param>
	/// <param name="count">Count of the random values to collect.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero.</exception>
	public static T[] Collect<T>(this IEnumerable<RandItem<T>> collection, int count)
	{
		if (collection == null) throw new ArgumentNullException(nameof(collection));
		if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, null);
		if (count == 0) return Array.Empty<T>();

		var result = new T[count];
		int i = 0;
		foreach (var item in collection)
		{
			result[i] = item.NextValue();
		}
		return result;
	}
}