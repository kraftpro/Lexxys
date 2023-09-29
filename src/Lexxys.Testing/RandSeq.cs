using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lexxys.Testing;

public static class RandItemsListExtensions
{
	public static T[] Collect<T>(this IReadOnlyCollection<RandItem<T>> collection)
	{
		if (collection == null)
			throw new ArgumentNullException(nameof(collection));

		return Collect(collection, collection.Count);
	}

	public static T[] Collect<T>(this IEnumerable<RandItem<T>> collection, int count)
	{
		if (collection == null)
			throw new ArgumentNullException(nameof(collection));
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, null);
		if (count == 0)
			return Array.Empty<T>();

		var result = new T[count];
		int i = 0;
		foreach (var item in collection)
		{
			result[i] = item.NextValue();
		}
		return result;
	}
}

/// <summary>
/// Represents a sequence of the random elements.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RandSeq<T>: IReadOnlyList<RandItem<T>>
{
	public static readonly RandSeq<T> Empty = new RandSeq<T>();

	private readonly RandItem<T>[] _items;

	private RandSeq()
	{
		_items = Array.Empty<RandItem<T>>();
	}

	private RandSeq(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, null);
		_items = new RandItem<T>[count];
	}

	public RandSeq(IEnumerable<RandItem<T>> items)
	{
		if (items == null)
			throw new ArgumentNullException(nameof(items));
		_items = items.ToArray();
	}

	public RandSeq(params RandItem<T>[] items)
	{
		if (items == null)
			throw new ArgumentNullException(nameof(items));
		_items = new RandItem<T>[items.Length];
		Array.Copy(items, _items, items.Length);
	}

	public RandSeq(RandItem<T> item)
	{
		_items = new[] { item };
	}

	public RandSeq(RandItem<T> item1, RandItem<T> item2)
	{
		_items = new[] { item1, item2 };
	}

	public RandSeq(RandItem<T> item1, RandItem<T> item2, RandItem<T> item3)
	{
		_items = new[] { item1, item2, item3 };
	}

	public RandItem<T> this[int index] => _items[index];

	public int Count => _items.Length;

	public IEnumerator<RandItem<T>> GetEnumerator() => ((IList<RandItem<T>>)_items).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

	public T[] Collect()
	{
		var result = new T[_items.Length];
		for (int i = 0; i < _items.Length; i++)
		{
			result[i] = _items[i].NextValue();
		}
		return result;
	}

	public IEnumerator<T> Enumerate()
	{
		for (;;)
			for (int i = 0; i < _items.Length; ++i)
				yield return _items[i].NextValue();
	}

	public RandSeq<T> Add(RandSeq<T> value)
	{
		if (value is not { _items.Length: >0 })
			return this;
		if (_items.Length == 0)
			return value;

		var result = new RandSeq<T>(_items.Length + value._items.Length);
		if (_items.Length == 1)
			result._items[0] = _items[0];
		else
			Array.Copy(_items, result._items, _items.Length);
		if (value._items.Length == 1)
			result._items[_items.Length] = value._items[0];
		else
			Array.Copy(value._items, 0, result._items, _items.Length, value._items.Length);
		return result;
	}

	public RandSeq<T> Add(RandItem<T> item)
	{
		var result = new RandSeq<T>(_items.Length + 1);
		if (_items.Length == 1)
			result._items[0] = _items[0];
		if (_items.Length > 0)
			Array.Copy(_items, result._items, _items.Length);
		result._items[_items.Length] = item;
		return result;
	}

	public RandSeq<T> Insert(RandItem<T> item)
	{
		var result = new RandSeq<T>(_items.Length + 1);
		if (_items.Length == 1)
			result._items[1] = _items[0];
		if (_items.Length > 0)
			Array.Copy(_items, 0, result._items, 1, _items.Length);
		result._items[0] = item;
		return result;
	}

	public RandSeq<T> Insert(int index, RandItem<T> item)
	{
		if (index < 0 || index > _items.Length)
			throw new ArgumentOutOfRangeException(nameof(index), index, null);
		if (index == 0)
			return Insert(item);
		if (index == _items.Length)
			return Add(item);
		var result = new RandSeq<T>(_items.Length + 1);
		Array.Copy(_items, 0, result._items, 0, index);
		result._items[index] = item;
		Array.Copy(_items, index, result._items, index + 1, _items.Length - index);
		return result;
	}

	public RandSeq<T> Insert(int index, RandSeq<T> value)
	{
		if (value is not { Count: >0 })
			return this;
		if (Count == 0)
			return this;
		if (index < 0 || index > _items.Length)
			throw new ArgumentOutOfRangeException(nameof(index), index, null);
		if (index == _items.Length)
			return Add(value);
		var result = new RandSeq<T>(_items.Length + value.Count);
		Array.Copy(_items, 0, result._items, 0, index);
		Array.Copy(value._items, 0, result._items, index, value._items.Length);
		Array.Copy(_items, index, result._items, index + value._items.Length, _items.Length - index);
		return result;
	}

	public override string ToString()
	{
		return String.Join(null, _items);
	}

	public static RandSeq<T>? operator |(RandSeq<T>? left, RandSeq<T>? right)
	{
		return right is null ? left:
			left is null ? right: left.Add(right);
	}

	public static RandSeq<T> operator |(RandSeq<T>? collection, RandItem<T> item)
	{
		return collection is null ? new RandSeq<T>(item): collection.Add(item);
	}

	public static RandSeq<T> operator |(RandItem<T> item, RandSeq<T>? collection)
	{
		return collection is null ? new RandSeq<T>(item): collection.Insert(item);
	}

	public static implicit operator T[] (RandSeq<T>? collection)
	{
		return collection?.Collect() ?? Array.Empty<T>();
	}

	public T[] ToTArray() => Collect();
}
