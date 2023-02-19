// Lexxys Infrastructural library.
// file: OrderedDictionary.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace Lexxys
{
	/// <summary>
	/// Represents a dictionary with duplicate keys when order of the elements is important.
	/// Find item complexity is O(n).
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	[Serializable]
	public class OrderedBag<TKey, TValue>: IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary, IList<(TKey Key, TValue Value)>, IReadOnlyList<(TKey Key, TValue Value)>
	{
		/// <summary>
		/// Creates a new <see cref="OrderedBag{TKey,TValue}"/> with the specified key <paramref name="comparer"/>.
		/// </summary>
		/// <param name="comparer">Keys equality comparer.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public OrderedBag(IEqualityComparer<TKey> comparer)
			: this(0, comparer)
		{
		}

		/// <summary>
		/// Creates a new <see cref="OrderedBag{TKey,TValue}"/> with the specified initial <paramref name="capacity"/> and key <paramref name="comparer"/>.
		/// </summary>
		/// <param name="capacity">Initial capacity of the <see cref="OrderedBag{TKey,TValue}"/>.</param>
		/// <param name="comparer">Keys equality comparer.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public OrderedBag(int capacity = 0, IEqualityComparer<TKey>? comparer = null)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);
			Comparer = comparer ?? EqualityComparer<TKey>.Default;
			List = capacity > 0 ? new List<(TKey, TValue)>(capacity): new List<(TKey, TValue)>();
		}

		/// <summary>
		/// Initializes a new <see cref="OrderedBag{TKey,TValue}"/> with the specified values.
		/// </summary>
		/// <param name="collection">Collection of values to be copied to the new instance of the <see cref="OrderedBag{TKey,TValue}"/>.</param>
		/// <param name="comparer">Keys equality comparer.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public OrderedBag(IEnumerable<(TKey, TValue)> collection, IEqualityComparer<TKey>? comparer = null)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			Comparer = comparer ?? EqualityComparer<TKey>.Default;
			List = new List<(TKey, TValue)>(collection);
		}

		/// <summary>
		/// Content of the <see cref="OrderedBag{TKey,TValue}"/>.
		/// </summary>
		protected List<(TKey Key, TValue Value)> List { get; }

		/// <summary>
		/// Actual keys comparer of the <see cref="OrderedBag{TKey,TValue}"/>.
		/// </summary>
		protected IEqualityComparer<TKey> Comparer { get; }

		/// <inheritdoc />
		public ICollection<TKey> Keys => new KeyCollection(this);

		/// <inheritdoc />
		public ICollection<TValue> Values => new ValueCollection(this);

		/// <inheritdoc cref="IDictionary{TKey,TValue}.this" />
		public TValue this[TKey key]
		{
			get
			{
				int i = IndexOf(key);
				if (i < 0)
					throw new KeyNotFoundException();
				return List[i].Value;
			}
			set
			{
				int i = IndexOf(key);
				if (i < 0)
					List.Add((key, value));
				else
					List[i] = (key, value);
			}
		}

		/// <summary>
		/// Gets all the values associated with the specified <paramref name="key"/>.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public IEnumerable<TValue> GetItems(TKey key)
			=> List.Where(o => Comparer.Equals(o.Key, key)).Select(o => o.Value);

		/// <summary>
		/// Get element at the specified position.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public (TKey, TValue) GetAt(int index) => List[index];

		/// <summary>
		/// Set element at the specified position.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		public void SetAt(int index, (TKey, TValue) value) => List[index] = value;

		/// <summary>
		/// Set element at the specified position.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetAt(int index, TKey key, TValue value) => List[index] = (key, value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int IndexOf(TKey key) => List.FindIndex(p => Comparer.Equals(p.Key, key));

		/// <inheritdoc />
		public void Add(TKey key, TValue value) => List.Add((key, value));

		/// <inheritdoc />
		public void Add(KeyValuePair<TKey, TValue> item) => List.Add((item.Key, item.Value));

		/// <inheritdoc cref="IDictionary{TKey,TValue}.ContainsKey" />
		public bool ContainsKey(TKey key) => IndexOf(key) >= 0;

		/// <inheritdoc />
		public bool Remove(TKey key)
		{
			int i = IndexOf(key);
			if (i < 0)
				return false;
			List.RemoveAt(i);
			return true;
		}

		/// <inheritdoc cref="IDictionary{TKey,TValue}.TryGetValue" />
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
		{
			int i = IndexOf(key);
			if (i < 0)
			{
				value = default!;
				return false;
			}
			value = List[i].Value;
			return true;
		}

		/// <summary>
		/// Gets the first value associated with the specified <paramref name="key"/> or the specified <paramref name="defaultValue"/>.  
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TValue GetValueOrDefault(TKey key, TValue defaultValue)
		{
			int i = IndexOf(key);
			return i < 0 ? defaultValue: List[i].Value;
		}

		/// <inheritdoc />
		public void Add((TKey, TValue) item) => List.Add(item);

		/// <summary>
		/// Add elements of the specified <paramref name="collection"/> to the end of the <see cref="OrderedBag{TKey,TValue}"/>
		/// </summary>
		/// <param name="collection"></param>
		public void AddRange(IEnumerable<(TKey, TValue)> collection) => List.AddRange(collection);

		/// <inheritdoc cref="ICollection{T}.Clear" />
		public void Clear() => List.Clear();

		/// <inheritdoc />
		public bool Contains(KeyValuePair<TKey, TValue> item) => IndexOf(item.Key) >= 0;

		/// <inheritdoc />
		public bool Contains((TKey, TValue) item) => IndexOf(item.Item1) >= 0;

		/// <inheritdoc />
		public void CopyTo((TKey, TValue)[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, null);
			if (array.Length - arrayIndex < List.Count)
				throw new ArgumentException($"The number of elements in the collection ({List.Count}) is greater than the available space ({array.Length - arrayIndex}) from index to the end of the destination array.", nameof(array));

			for (int i = 0; i < List.Count; ++i)
			{
				array[arrayIndex++] = new KeyValuePair<TKey, TValue>(List[i].Key, List[i].Value);
			}
		}

		/// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
		public int Count => List.Count;

		/// <inheritdoc />
		public bool IsFixedSize => false;

		/// <inheritdoc cref="IDictionary.IsReadOnly" />
		public bool IsReadOnly => false;

		/// <inheritdoc />
		public bool IsSynchronized => false;

		/// <inheritdoc />
		public object SyncRoot => this;

		/// <inheritdoc />
		public bool Remove((TKey, TValue) item)
		{
			int i = IndexOf(item.Item1);
			if (i < 0)
				return false;
			List.RemoveAt(i);
			return true;
		}

		/// <inheritdoc />
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			int i = IndexOf(item.Key);
			if (i < 0)
				return false;
			List.RemoveAt(i);
			return true;
		}

		/// <inheritdoc />
		public IEnumerator<(TKey Key, TValue Value)> GetEnumerator() => List.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			foreach (var item in List)
			{
				yield return new KeyValuePair<TKey, TValue>(item.Key, item.Value);
			}
		}

		/// <inheritdoc />
		public int IndexOf((TKey, TValue) item) => List.IndexOf(item);

		/// <inheritdoc />
		public void Insert(int index, (TKey, TValue) item)
		{
			if (index < 0 || index > List.Count)
				throw new ArgumentOutOfRangeException(nameof(index), index, null);
			List.Insert(index, item);
		}

		/// <inheritdoc />
		public void RemoveAt(int index) => List.RemoveAt(index);

		(TKey Key, TValue Value) IList<(TKey Key, TValue Value)>.this[int index] { get => List[index]; set => List[index] = value; }

		#region Internal classes

		private class KeyCollection: ICollection<TKey>, ICollection
		{
			private readonly OrderedBag<TKey, TValue> _bag;

			public KeyCollection(OrderedBag<TKey, TValue> dictionary) => _bag = dictionary;

			public void Add(TKey item) => throw new NotImplementedException();

			public bool Remove(TKey item) => throw new NotImplementedException();

			public void Clear() => throw new NotImplementedException();

			public bool Contains(TKey item) => _bag.IndexOf(item) >= 0;

			public void CopyTo(TKey[] array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				if (array.Length - index < _bag.Count)
					throw new ArgumentException($"The number of elements in the collection ({_bag.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

				for (int i = 0; i < _bag.List.Count; ++i)
				{
					array[index++] = _bag.List[i].Key;
				}
			}

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				if (array.Length - index < _bag.Count)
					throw new ArgumentException($"The number of elements in the collection ({_bag.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

				for (int i = 0; i < _bag.List.Count; ++i)
				{
					array.SetValue(_bag.List[i].Key, index++);
				}
			}

			public int Count => _bag.Count;

			public bool IsReadOnly => true;

			public bool IsSynchronized => false;

			public object SyncRoot => false;

			public IEnumerator<TKey> GetEnumerator()
			{
				foreach (var item in _bag.List)
				{
					yield return item.Key;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				foreach (var item in _bag.List)
				{
					yield return item.Key;
				}
			}
		}

		private class ValueCollection: ICollection<TValue>, ICollection
		{
			private readonly OrderedBag<TKey, TValue> _bag;
			
			public ValueCollection(OrderedBag<TKey, TValue> dictionary) => _bag = dictionary;

			public void Add(TValue item) => throw new NotImplementedException();

			public bool Remove(TValue item) => throw new NotImplementedException();

			public void Clear() => throw new NotImplementedException();

			public bool Contains(TValue item) => _bag.List.FindIndex(o => Object.Equals(o.Value, item)) >= 0;

			public void CopyTo(TValue[] array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				if (array.Length - index < _bag.Count)
					throw new ArgumentException($"The number of elements in the collection ({_bag.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

				for (int i = 0; i < _bag.List.Count; ++i)
				{
					array[index++] = _bag.List[i].Value;
				}
			}

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				if (array.Length - index < _bag.Count)
					throw new ArgumentException($"The number of elements in the collection ({_bag.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

				for (int i = 0; i < _bag.List.Count; ++i)
				{
					array.SetValue(_bag.List[i].Value, index++);
				}
			}

			public int Count => _bag.Count;

			public bool IsReadOnly => true;

			public bool IsSynchronized => false;

			public object SyncRoot => this;

			public IEnumerator<TValue> GetEnumerator()
			{
				foreach (var item in _bag.List)
				{
					yield return item.Value;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				foreach (var item in _bag.List)
				{
					yield return item.Value;
				}
			}
		}

		private sealed class DictionaryEnumerator: IDictionaryEnumerator
		{
			private List<(TKey Key, TValue Value)>.Enumerator _parent;

			public DictionaryEnumerator(OrderedBag<TKey, TValue> dictionary) => _parent = dictionary.List.GetEnumerator();

			public DictionaryEntry Entry => new DictionaryEntry(_parent.Current.Key!, _parent.Current.Value);

			public object Key => _parent.Current.Key!;

			public object? Value => _parent.Current.Value;

			public object Current => new DictionaryEntry(_parent.Current.Key!, _parent.Current.Value);

			public bool MoveNext() => _parent.MoveNext();

			public void Reset() { }
		}

		#endregion

		void IDictionary.Add(object key, object? value) => Add((TKey)key, (TValue)value!);

		bool IDictionary.Contains(object key) => ContainsKey((TKey)key);

		IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

		void IDictionary.Remove(object key) => Remove((TKey)key);

		object? IDictionary.this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value!; }

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index), index, null);
			if (array.Rank != 1)
				throw new ArgumentException($"Array is multidimensional.", nameof(array));
			if (array.Length - index < List.Count)
				throw new ArgumentException($"The number of elements in the collection ({List.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

			for (int i = 0; i < List.Count; ++i)
			{
				array.SetValue(new DictionaryEntry(List[i].Key!, List[i].Value), index++);
			}
		}

		ICollection IDictionary.Keys => new KeyCollection(this);

		ICollection IDictionary.Values => new ValueCollection(this);

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

		(TKey Key, TValue Value) IReadOnlyList<(TKey Key, TValue Value)>.this[int index] => List[index];
	}
}
