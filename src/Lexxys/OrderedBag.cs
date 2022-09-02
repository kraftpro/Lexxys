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

#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace Lexxys
{
	[Serializable]
	public class OrderedBag<TKey, TValue>: IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary, IList<(TKey Key, TValue Value)>, IReadOnlyList<(TKey Key, TValue Value)>
	{
		public OrderedBag(IEqualityComparer<TKey> comparer)
			: this(0, comparer)
		{
		}

		public OrderedBag(int capacity = 0, IEqualityComparer<TKey>? comparer = null)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);
			List = capacity > 0 ? new List<(TKey, TValue)>(capacity): new List<(TKey, TValue)>();
			Comparer = comparer ?? EqualityComparer<TKey>.Default;
		}

		public OrderedBag(IEnumerable<(TKey, TValue)> collection, IEqualityComparer<TKey>? comparer = null)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			Comparer = comparer ?? EqualityComparer<TKey>.Default;
			List = new List<(TKey, TValue)>(collection);
		}


		protected List<(TKey Key, TValue Value)> List { get; }

		protected IEqualityComparer<TKey> Comparer { get; }

		public ICollection<TKey> Keys => new KeyCollection(this);

		public ICollection<TValue> Values => new ValueCollection(this);

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

		public (TKey, TValue) GetAt(int index) => List[index];

		public void SetAt(int index, (TKey, TValue) value) => List[index] = value;

		public void SetAt(int index, TKey key, TValue value) => List[index] = (key, value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int IndexOf(TKey key) => List.FindIndex(p => Comparer.Equals(p.Key, key));

		public void Add(TKey key, TValue value) => List.Add((key, value));

		public void Add(KeyValuePair<TKey, TValue> item) => List.Add((item.Key, item.Value));

		public bool ContainsKey(TKey key) => IndexOf(key) >= 0;

		public bool Remove(TKey key)
		{
			int i = IndexOf(key);
			if (i < 0)
				return false;
			List.RemoveAt(i);
			return true;
		}

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

		public TValue TryGetValue(TKey key, TValue defaultValue)
		{
			int i = IndexOf(key);
			return i < 0 ? defaultValue: List[i].Value;
		}

		public void Add((TKey, TValue) item) => List.Add(item);

		public void AddRange(IEnumerable<(TKey, TValue)> collection) => List.AddRange(collection);

		public void Clear() => List.Clear();

		public bool Contains(KeyValuePair<TKey, TValue> item) => IndexOf(item.Key) >= 0;

		public bool Contains((TKey, TValue) item) => IndexOf(item.Item1) >= 0;

		public void CopyTo((TKey, TValue)[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);

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

		public int Count => List.Count;

		public bool IsFixedSize => false;

		public bool IsReadOnly => false;

		public bool IsSynchronized => false;

		public object SyncRoot => this;

		public bool Remove((TKey, TValue) item)
		{
			int i = IndexOf(item.Item1);
			if (i < 0)
				return false;
			List.RemoveAt(i);
			return true;
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			int i = IndexOf(item.Key);
			if (i < 0)
				return false;
			List.RemoveAt(i);
			return true;
		}

		public IEnumerator<(TKey Key, TValue Value)> GetEnumerator() => List.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			foreach (var item in List)
			{
				yield return new KeyValuePair<TKey, TValue>(item.Key, item.Value);
			}
		}

		public int IndexOf((TKey, TValue) item) => List.IndexOf(item);

		public void Insert(int index, (TKey, TValue) item)
		{
			if (index < 0 || index > List.Count)
				throw new ArgumentOutOfRangeException(nameof(index), index, null);
			List.Insert(index, item);
		}

		public void RemoveAt(int index) => List.RemoveAt(index);

		(TKey Key, TValue Value) IList<(TKey Key, TValue Value)>.this[int index] { get => List[index]; set => List[index] = value; }

		#region Internal classes

		protected class KeyCollection: ICollection<TKey>, ICollection
		{
			public KeyCollection(OrderedBag<TKey, TValue> dictionary) => Bag = dictionary;

			protected OrderedBag<TKey, TValue> Bag { get; }

			public void Add(TKey item) => throw new NotImplementedException();

			public bool Remove(TKey item) => throw new NotImplementedException();

			public void Clear() => throw new NotImplementedException();

			public bool Contains(TKey item) => Bag.IndexOf(item) >= 0;

			public void CopyTo(TKey[] array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				if (array.Length - index < Bag.Count)
					throw new ArgumentException($"The number of elements in the collection ({Bag.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

				for (int i = 0; i < Bag.List.Count; ++i)
				{
					array[index++] = Bag.List[i].Key;
				}
			}

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				if (array.Length - index < Bag.Count)
					throw new ArgumentException($"The number of elements in the collection ({Bag.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

				for (int i = 0; i < Bag.List.Count; ++i)
				{
					array.SetValue(Bag.List[i].Key, index++);
				}
			}

			public int Count => Bag.Count;

			public bool IsReadOnly => true;

			public bool IsSynchronized => false;

			public object SyncRoot => false;

			public IEnumerator<TKey> GetEnumerator()
			{
				foreach (var item in Bag.List)
				{
					yield return item.Key;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				foreach (var item in Bag.List)
				{
					yield return item.Key;
				}
			}
		}

		protected class ValueCollection: ICollection<TValue>, ICollection
		{
			public ValueCollection(OrderedBag<TKey, TValue> dictionary) => Bag = dictionary;

			protected OrderedBag<TKey, TValue> Bag { get; }

			public void Add(TValue item) => throw new NotImplementedException();

			public bool Remove(TValue item) => throw new NotImplementedException();

			public void Clear() => throw new NotImplementedException();

			public bool Contains(TValue item) => Bag.List.FindIndex(o => Object.Equals(o.Value, item)) >= 0;

			public void CopyTo(TValue[] array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				if (array.Length - index < Bag.Count)
					throw new ArgumentException($"The number of elements in the collection ({Bag.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

				for (int i = 0; i < Bag.List.Count; ++i)
				{
					array[index++] = Bag.List[i].Value;
				}
			}

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				if (array.Length - index < Bag.Count)
					throw new ArgumentException($"The number of elements in the collection ({Bag.Count}) is greater than the available space ({array.Length - index}) from index to the end of the destination array.", nameof(array));

				for (int i = 0; i < Bag.List.Count; ++i)
				{
					array.SetValue(Bag.List[i].Value, index++);
				}
			}

			public int Count => Bag.Count;

			public bool IsReadOnly => true;

			public bool IsSynchronized => false;

			public object SyncRoot => this;

			public IEnumerator<TValue> GetEnumerator()
			{
				foreach (var item in Bag.List)
				{
					yield return item.Value;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				foreach (var item in Bag.List)
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
