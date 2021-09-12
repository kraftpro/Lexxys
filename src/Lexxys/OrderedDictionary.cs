// Lexxys Infrastructural library.
// file: OrderedDictionary.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Lexxys
{
	[Serializable]
	public class OrderedBag<TKey, TValue>: IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary, IList<KeyValuePair<TKey, TValue>>, IReadOnlyList<KeyValuePair<TKey, TValue>>
	{
		protected List<KeyValuePair<TKey, TValue>> List;
		protected IEqualityComparer<TKey> Comparer;

		public OrderedBag(IEqualityComparer<TKey> comparer)
			: this(0, comparer)
		{
		}

		public OrderedBag(int capacity = 0, IEqualityComparer<TKey> comparer = null)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);
			List = capacity > 0 ? new List<KeyValuePair<TKey, TValue>>(capacity): new List<KeyValuePair<TKey, TValue>>();
			Comparer = comparer ?? EqualityComparer<TKey>.Default;
		}

		public OrderedBag(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer = null)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			Comparer = comparer ?? EqualityComparer<TKey>.Default;
			List = new List<KeyValuePair<TKey, TValue>>(collection);
		}


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
					List.Add(new KeyValuePair<TKey, TValue>(key, value));
				else
					List[i] = new KeyValuePair<TKey, TValue>(key, value);
			}
		}

		public KeyValuePair<TKey, TValue> GetAt(int index) => List[index];

		public void SetAt(int index, KeyValuePair<TKey, TValue> value) => List[index] = value;

		public void SetAt(int index, TKey key, TValue value) => List[index] = new KeyValuePair<TKey, TValue>(key, value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int IndexOf(TKey key) => List.FindIndex(p => Comparer.Equals(p.Key, key));

		public void Add(TKey key, TValue value) => List.Add(new KeyValuePair<TKey, TValue>(key, value));

		public bool ContainsKey(TKey key) => IndexOf(key) >= 0;

		public bool Remove(TKey key)
		{
			int i = IndexOf(key);
			if (i < 0)
				return false;
			List.RemoveAt(i);
			return true;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			int i = IndexOf(key);
			if (i < 0)
			{
				value = default;
				return false;
			}
			value = List[i].Value;
			return true;
		}

		public TValue TryGetValue(TKey key, TValue defaultValue = default)
		{
			int i = IndexOf(key);
			return i < 0 ? defaultValue: List[i].Value;
		}

		public void Add(KeyValuePair<TKey, TValue> item) => List.Add(item);

		public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection) => List.AddRange(collection);

		public void Clear() => List.Clear();

		public bool Contains(KeyValuePair<TKey, TValue> item) => IndexOf(item.Key) >= 0;

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);

		public int Count => List.Count;

		public bool IsFixedSize => false;

		public bool IsReadOnly => false;

		public bool IsSynchronized => false;

		public object SyncRoot => this;

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			int i = IndexOf(item.Key);
			if (i < 0)
				return false;
			List.RemoveAt(i);
			return true;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => List.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

		public int IndexOf(KeyValuePair<TKey, TValue> item) => List.IndexOf(item);

		public void Insert(int index, KeyValuePair<TKey, TValue> item)
		{
			if (index < 0 || index > List.Count)
				throw new ArgumentOutOfRangeException(nameof(index), index, null);
			List.Insert(index, item);
		}

		public void RemoveAt(int index) => List.RemoveAt(index);

		KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index] { get => List[index]; set => List[index] = value; }

		#region Internal classes

		protected class KeyCollection: ICollection<TKey>, ICollection
		{
			protected OrderedBag<TKey, TValue> Bag;

			public KeyCollection(OrderedBag<TKey, TValue> dictionary) => Bag = dictionary;

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
					throw new ArgumentOutOfRangeException(nameof(array) + ".Length", array.Length, null);

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
					throw new ArgumentOutOfRangeException(nameof(array) + ".Length", array.Length, null);

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
			protected OrderedBag<TKey, TValue> Bag;

			public ValueCollection(OrderedBag<TKey, TValue> dictionary) => Bag = dictionary;

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
					throw new ArgumentOutOfRangeException(nameof(array) + ".Length", array.Length, null);

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
					throw new ArgumentOutOfRangeException(nameof(array) + ".Length", array.Length, null);

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
			private List<KeyValuePair<TKey, TValue>>.Enumerator _parent;

			public DictionaryEnumerator(OrderedBag<TKey, TValue> dictionary) => _parent = dictionary.List.GetEnumerator();

			public DictionaryEntry Entry => new DictionaryEntry(_parent.Current.Key, _parent.Current.Value);

			public object Key => _parent.Current.Key;

			public object Value => _parent.Current.Value;

			public object Current => new DictionaryEntry(_parent.Current.Key, _parent.Current.Value);

			public bool MoveNext() => _parent.MoveNext();

			public void Reset() { }
		}

		#endregion

		void IDictionary.Add(object key, object value) => Add((TKey)key, (TValue)value);

		bool IDictionary.Contains(object key) => ContainsKey((TKey)key);

		IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

		void IDictionary.Remove(object key) => Remove((TKey)key);

		object IDictionary.this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value; }

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index), index, null);
			if (array.Rank != 1)
				throw new ArgumentOutOfRangeException(nameof(array) + ".Rank", array.Rank, null);
			if (array.Length - index < List.Count)
				throw new ArgumentOutOfRangeException(nameof(array) + ".Length", array.Length, null);

			for (int i = 0; i < List.Count; ++i)
			{
				array.SetValue(new DictionaryEntry(List[i].Key, List[i].Value), index++);
			}
		}

		ICollection IDictionary.Keys => new KeyCollection(this);

		ICollection IDictionary.Values => new ValueCollection(this);

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

		KeyValuePair<TKey, TValue> IReadOnlyList<KeyValuePair<TKey, TValue>>.this[int index] => List[index];
	}
}
