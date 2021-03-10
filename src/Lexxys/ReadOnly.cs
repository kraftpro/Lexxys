// Lexxys Infrastructural library.
// file: ReadOnly.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lexxys
{
	public interface ISafeList<T>: IList<T>
	{
	}

	public interface ISafeDictionary<TKey, TValue>: IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
	{
	}

	public interface IReadOnlySet<T>: IReadOnlyCollection<T>
	{
		bool Contains(T item);
		bool IsProperSubsetOf(IEnumerable<T> other);
		bool IsProperSupersetOf(IEnumerable<T> other);
		bool IsSubsetOf(IEnumerable<T> other);
		bool IsSupersetOf(IEnumerable<T> other);
		bool Overlaps(IEnumerable<T> other);
		bool SetEquals(IEnumerable<T> other);
	}

	public interface IWrappedCollection<T>: IReadOnlyCollection<T>, ICollection<T>, ICollection
	{
		new int Count { get; }
	}

	public interface IWrappedList<T>: IReadOnlyList<T>, IList<T>, IWrappedCollection<T>
	{
		new int Count { get; }
		new T this[int index] { get; }
	}

	public interface IWrappedSafeList<T>: IReadOnlyList<T>, ISafeList<T>, IWrappedCollection<T>
	{
		new T this[int index] { get; }
	}

	public interface IWrappedSet<T>: IReadOnlySet<T>, ISet<T>, IWrappedCollection<T>
	{
		new bool Contains(T item);
		new bool IsSubsetOf(IEnumerable<T> items);
		new bool IsSupersetOf(IEnumerable<T> items);
		new bool IsProperSubsetOf(IEnumerable<T> items);
		new bool IsProperSupersetOf(IEnumerable<T> items);
		new bool Overlaps(IEnumerable<T> items);
		new bool SetEquals(IEnumerable<T> items);
	}

	public interface IWrappedDictionary<TKey, TValue>: IReadOnlyDictionary<TKey, TValue>, ISafeDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IDictionary, IWrappedCollection<KeyValuePair<TKey, TValue>>
	{
		new void Clear();
		new bool IsReadOnly { get; }
		new bool ContainsKey(TKey key);
		new TValue this[TKey key] { get; }
		new IWrappedCollection<TKey> Keys { get; }
		new IWrappedCollection<TValue> Values { get; }
		new bool TryGetValue(TKey key, out TValue value);
		new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();
	}

	public static class ReadOnly
	{
		#region Empty Collections

		public static IWrappedList<T> Empty<T>() => EmptyCollectionInstance<T>.ListValue;

		public static IWrappedDictionary<TKey, TValue> Empty<TKey, TValue>() => EmptyDictionaryInstance<TKey, TValue>.DictionaryValue;

		public static IWrappedList<T> Empty<T>(IEnumerable<T> _) => EmptyCollectionInstance<T>.ListValue;

		public static IWrappedSet<T> Empty<T>(ISet<T> _) => EmptyCollectionInstance<T>.SetValue;

		public static IWrappedSafeList<T> Empty<T>(ISafeList<T> _) => EmptyCollectionInstance<T>.SafeListValue;

		public static IDictionary<TKey, TValue> Empty<TKey, TValue>(IDictionary<TKey, TValue> _) => EmptyDictionaryInstance<TKey, TValue>.DictionaryValue;

		public static IWrappedSafeList<T> EmptySafeCollection<T>() => EmptyCollectionInstance<T>.SafeListValue;

		public static IWrappedSet<T> EmptySet<T>() => EmptyCollectionInstance<T>.SetValue;

		public static ListValueWrap<T> EmptyValue<T>() => ListValueWrap<T>.Empty;

		private static class EmptyCollectionInstance<T>
		{
			public static readonly IWrappedList<T> ListValue = new ReadOnlyArrayWrap<T>(EmptyArray<T>.Value);
			public static readonly IWrappedSafeList<T> SafeListValue = new ReadOnlySafeListWrap<T>(new EmptySafeListInstance<T>());
			public static readonly IWrappedSet<T> SetValue = new ReadOnlySetWrap<T>(new SortedSet<T>());
		}
		private static class EmptyDictionaryInstance<TKey, TValue>
		{
			public static readonly IWrappedDictionary<TKey, TValue> DictionaryValue = new ReadOnlyDictionaryWrap<TKey, TValue>(new SortedList<TKey, TValue>());
			public static readonly ISafeDictionary<TKey, TValue> SafeDictionaryValue = new ReadOnlyDictionaryWrap<TKey, TValue>(new SortedList<TKey, TValue>());
		}


		#endregion

		public static IWrappedDictionary<TKey, TValue> Wrap<TKey, TValue>(IDictionary<TKey, TValue> value, bool substituteNullValue)
		{
			return value == null ? (substituteNullValue ? Empty<TKey, TValue>(): null): new ReadOnlyDictionaryWrap<TKey, TValue>(value);
		}

		public static IWrappedDictionary<TKey, TValue> Wrap<TKey, TValue>(IDictionary<TKey, TValue> value) => Wrap(value, false);

		public static IWrappedDictionary<TKey, TValue> WrapCopy<TKey, TValue>(IDictionary<TKey, TValue> value, bool substituteNullValue)
		{
			return value == null ? 
				substituteNullValue ? Empty<TKey, TValue>(): null: 
				value.Count == 0 ? Empty<TKey, TValue>(): new ReadOnlyDictionaryWrap<TKey, TValue>(new Dictionary<TKey, TValue>(value));
		}

		public static IWrappedDictionary<TKey, TValue> WrapCopy<TKey, TValue>(IDictionary<TKey, TValue> value) => WrapCopy(value, false);

		public static IWrappedList<T> WrapValue<T>(params T[] value)
		{
			return value == null || value.Length == 0 ? Empty<T>(): new ReadOnlyArrayWrap<T>(value);
		}

		public static IWrappedList<T> Wrap<T>(T[] value, bool substituteNullValue)
		{
			return value == null ? 
				substituteNullValue ? Empty<T>(): null: 
				value.Length == 0 ? Empty<T>(): new ReadOnlyArrayWrap<T>(value);
		}

		public static IWrappedList<T> Wrap<T>(T[] value) => Wrap(value, false);

		public static IWrappedList<T> Wrap<T>(T[] value, int offset, int count = -1, bool substituteNullValue = false)
		{
			return value == null ? (substituteNullValue ? Empty<T>(): null):
				count == 0 ? Empty<T>() : new ReadOnlyArrayFragmentWrap<T>(value, offset, count);
		}

		public static IWrappedList<T> Wrap<T>(IList<T> value, bool substituteNullValue)
		{
			return value == null ? 
				substituteNullValue ? Empty<T>(): null:
				new ReadOnlyListWrap<T>(value);
		}

		public static IWrappedList<T> Wrap<T>(IList<T> value) => Wrap(value, false);

		public static IWrappedList<T> Wrap<T>(IList<T> value, int offset, int count = -1, bool substituteNullValue = false)
		{
			return value == null ? 
				substituteNullValue ? Empty<T>(): null:
				count == 0 ? Empty<T>(): new ReadOnlyListFragmentWrap<T>(value, offset, count);
		}

		public static IWrappedSafeList<T> Wrap<T>(ISafeList<T> value, bool substituteNullValue)
		{
			return value == null ? 
				substituteNullValue ? EmptySafeCollection<T>(): null:
				new ReadOnlySafeListWrap<T>(value);
		}

		public static IWrappedSafeList<T> Wrap<T>(ISafeList<T> value) => Wrap(value, false);

		public static IWrappedSafeList<T> Wrap<T>(ISafeList<T> value, int offset, int count = -1, bool substituteNullValue = false)
		{
			return value == null ? 
				substituteNullValue ? EmptySafeCollection<T>(): null:
				count == 0 ? EmptySafeCollection<T>(): new ReadOnlySafeListFragmentWrap<T>(value, offset, count);
		}

		public static IWrappedCollection<T> Wrap<T>(ICollection<T> value, bool substituteNullValue)
		{
			return value == null ? 
				substituteNullValue ? Empty<T>(): null:
				(IWrappedCollection<T>)new ReadOnlyCollectionWrap<T>(value);
		}

		public static IWrappedCollection<T> Wrap<T>(ICollection<T> value) => Wrap(value, false);

		public static IWrappedList<T> WrapCopy<T>(ICollection<T> value, bool substituteNullValue)
		{
			if (value == null)
				return substituteNullValue ? Empty<T>(): null;
			if (value.Count == 0)
				return Empty<T>();
			var items = new T[value.Count];
			value.CopyTo(items, 0);
			return new ReadOnlyArrayWrap<T>(items);
		}

		public static IWrappedList<T> WrapCopy<T>(ICollection<T> value) => WrapCopy(value, false);

		public static IEnumerable<T> Wrap<T>(IEnumerable<T> value, bool substituteNullValue)
		{
			return value == null ? 
				substituteNullValue ? Empty<T>(): null: 
				(IEnumerable<T>)new ReadOnlyEnumerableWrap<T>(value);
		}

		public static IEnumerable<T> Wrap<T>(IEnumerable<T> value) => Wrap(value, false);

		public static IWrappedList<T> WrapCopy<T>(IEnumerable<T> value, bool substituteNullValue)
		{
			if (value == null)
				return substituteNullValue ? Empty<T>(): null;
			if (value is ICollection<T> collection)
				return WrapCopy(collection, substituteNullValue);
			var temp = new List<T>(value);
			if (temp.Count == 0)
				return Empty<T>();
			return new ReadOnlyListWrap<T>(temp);
		}

		public static IWrappedList<T> WrapCopy<T>(IEnumerable<T> value) => WrapCopy(value, false);

		public static IWrappedSet<T> Wrap<T>(ISet<T> value, bool substituteNullValue)
		{
			return value == null ? (substituteNullValue ? EmptySet<T>(): null):
				new ReadOnlySetWrap<T>(value);
		}

		public static IWrappedSet<T> Wrap<T>(ISet<T> value) => Wrap(value, false);


		public static IWrappedSet<T> WrapCopy<T>(ISet<T> value, bool substituteNullValue)
		{
			return value == null ? (substituteNullValue ? EmptySet<T>(): null):
				value.Count == 0 ? EmptySet<T>():
				new ReadOnlySetWrap<T>(new HashSet<T>(value));
		}

		public static IWrappedSet<T> WrapCopy<T>(ISet<T> value) => WrapCopy(value, false);


		public static IReadOnlyList<T> ReWrap<T>(IReadOnlyList<T> value, bool substituteNullValue)
		{
			return value == null ?
				substituteNullValue ? (IReadOnlyList<T>)Empty<T>() : null :
				new ReadOnlyListReWrap<T>(value);
		}

		public static IReadOnlyList<T> ReWrap<T>(IReadOnlyList<T> value) => ReWrap(value, false);

		public static IReadOnlyList<T> ReWrap<T>(IReadOnlyList<T> value, int offset, int count = -1, bool substituteNullValue = false)
		{
			return value == null ?
				substituteNullValue ? Empty<T>() : null :
				count == 0 ? (IReadOnlyList<T>)Empty<T>() : new ReadOnlyListFragmentReWrap<T>(value, offset, count);
		}

		public static IReadOnlyCollection<T> ReWrap<T>(IReadOnlyCollection<T> value, bool substituteNullValue)
		{
			return value == null ?
				substituteNullValue ? (IReadOnlyCollection<T>)Empty<T>() : null :
				new ReadOnlyCollectionReWrap<T>(value);
		}

		public static IReadOnlyCollection<T> ReWrap<T>(IReadOnlyCollection<T> value) => ReWrap(value, false);

		public static IReadOnlyDictionary<TKey, TValue> ReWrap<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> value, bool substituteNullValue)
		{
			return value == null ? (substituteNullValue ? (IReadOnlyDictionary<TKey, TValue>)Empty<TKey, TValue>() : null) : new ReadOnlyDictionaryReWrap<TKey, TValue>(value);
		}

		public static IReadOnlyDictionary<TKey, TValue> ReWrap<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> value) => ReWrap(value, false);


		public static ListValueWrap<T> ValueWrap<T>(IReadOnlyList<T> value)
		{
			return value == null ? ListValueWrap<T>.Empty: new ListValueWrap<T>(value);
		}

		public static ListFragmentValueWrap<T> ValueWrap<T>(IReadOnlyList<T> value, int offset, int count = -1)
		{
			return value == null || count == 0 ? ListFragmentValueWrap<T>.Empty: new ListFragmentValueWrap<T>(value, offset, count);
		}

		public static CollectionValueWrap<T> ValueWrap<T>(IReadOnlyCollection<T> value)
		{
			return value == null ? CollectionValueWrap<T>.Empty: new CollectionValueWrap<T>(value);
		}

		public static DictionaryValueWrap<TKey, TValue> ValueWrap<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> value)
		{
			return value == null ? DictionaryValueWrap<TKey, TValue>.Empty: new DictionaryValueWrap<TKey, TValue>(value);
		}

		#region Implementation

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
		private class ReadOnlyDictionaryWrap<TKey, TValue>: IWrappedDictionary<TKey, TValue>
		{
			private readonly IDictionary<TKey, TValue> _dictionary;

			public ReadOnlyDictionaryWrap(IDictionary<TKey, TValue> dictionary) => _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

			public void Add(TKey key, TValue value) => throw new ReadOnlyException();

			public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

			public IWrappedCollection<TKey> Keys => new ReadOnlyCollectionWrap<TKey>(_dictionary.Keys);
			IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

			ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

			public bool Remove(TKey key) => throw new ReadOnlyException();

			public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

			public IWrappedCollection<TValue> Values => new ReadOnlyCollectionWrap<TValue>(_dictionary.Values);

			IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

			ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

			public TValue this[TKey key] { get => _dictionary[key]; set => throw new ReadOnlyException(); }

			public void Add(KeyValuePair<TKey, TValue> item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);

			public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => _dictionary.CopyTo(array, arrayIndex);

			public int Count => _dictionary.Count;

			public bool IsReadOnly => true;

			public bool Remove(KeyValuePair<TKey, TValue> item) => throw new ReadOnlyException();

			public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

			public void Add(object key, object value) => throw new ReadOnlyException();

			public bool Contains(object key) => key is TKey k && ContainsKey(k);

			IDictionaryEnumerator IDictionary.GetEnumerator()
			{
				return _dictionary is IDictionary d ? d.GetEnumerator(): throw new NotImplementedException();
			}

			public bool IsFixedSize => true;

			ICollection IDictionary.Keys => new ReadOnlyCollectionWrap<TKey>(_dictionary.Keys);

			public void Remove(object key) => throw new ReadOnlyException();

			ICollection IDictionary.Values => new ReadOnlyCollectionWrap<TValue>(_dictionary.Values);

			public object this[object key]
			{
				get => key is TKey index ? this[index]: throw new ReadOnlyException();
				set => throw new ReadOnlyException();
			}

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));

				if (_dictionary is ICollection collection)
				{
					collection.CopyTo(array, index);
				}
				else if (array is KeyValuePair<TKey, TValue>[] values)
				{
					_dictionary.CopyTo(values, index);
				}
				else
				{
					values = new KeyValuePair<TKey, TValue>[_dictionary.Count];
					_dictionary.CopyTo(values, 0);
					Array.Copy(values, 0, array, index, values.Length);
				}
			}

			public bool IsSynchronized => (_dictionary as ICollection)?.IsSynchronized == true;

			public object SyncRoot => (_dictionary as ICollection)?.SyncRoot ?? _dictionary;

			public override string ToString() => "RO:" + _dictionary.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
		private class ReadOnlyDictionaryReWrap<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
		{
			private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;

			public ReadOnlyDictionaryReWrap(IReadOnlyDictionary<TKey, TValue> dictionary) => _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

			public int Count => _dictionary.Count;

			public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

			public TValue this[TKey key] { get => _dictionary[key]; }

			public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

			public IEnumerable<TKey> Keys => _dictionary.Keys;

			public IEnumerable<TValue> Values => _dictionary.Values;

			public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

			public override string ToString() => "RO:" + _dictionary.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
		public readonly struct DictionaryValueWrap<TKey, TValue>: IReadOnlyDictionary<TKey, TValue>
		{
			public static readonly DictionaryValueWrap<TKey, TValue> Empty = new DictionaryValueWrap<TKey, TValue>(Empty<TKey, TValue>());

			private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;

			public DictionaryValueWrap(IReadOnlyDictionary<TKey, TValue> dictionary) => _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

			public int Count => _dictionary.Count;

			public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

			public TValue this[TKey key] { get => _dictionary[key]; }

			public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

			public IEnumerable<TKey> Keys => _dictionary.Keys;

			public IEnumerable<TValue> Values => _dictionary.Values;

			public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

			public override bool Equals(object obj) => obj is DictionaryValueWrap<TKey, TValue> other && Equals(other);

			public bool Equals(DictionaryValueWrap<TKey, TValue> other) => ReferenceEquals(_dictionary, other._dictionary);

			public override int GetHashCode() => _dictionary.GetHashCode();

			public static bool operator ==(DictionaryValueWrap<TKey, TValue> left, DictionaryValueWrap<TKey, TValue> right) => left.Equals(right);

			public static bool operator !=(DictionaryValueWrap<TKey, TValue> left, DictionaryValueWrap<TKey, TValue> right) => !(left == right);

			public override string ToString() => "RO:" + _dictionary.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlyListWrap<T>: IWrappedList<T>
		{
			private readonly IList<T> _list;

			public ReadOnlyListWrap(IList<T> list) => _list = list ?? throw new ArgumentNullException(nameof(list));

			public int IndexOf(T item) => _list.IndexOf(item);

			public void Insert(int index, T item) => throw new ReadOnlyException();

			public void RemoveAt(int index) => throw new ReadOnlyException();

			public T this[int index] { get => _list[index]; set => throw new ReadOnlyException(); }

			public void Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item) => _list.Contains(item);

			public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

			public int Count => _list.Count;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));

				if (_list is ICollection xx)
				{
					xx.CopyTo(array, index);
				}
				else if (array is T[] values)
				{
					_list.CopyTo(values, index);
				}
				else
				{
					values = new T[_list.Count];
					_list.CopyTo(values, 0);
					Array.Copy(values, 0, array, index, values.Length);
				}
			}

			public bool IsSynchronized => (_list as ICollection)?.IsSynchronized == true;

			public object SyncRoot => (_list as ICollection)?.SyncRoot ?? _list;

			public override string ToString() => "RO:" + _list.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlyListReWrap<T> : IReadOnlyList<T>
		{
			private readonly IReadOnlyList<T> _list;

			public ReadOnlyListReWrap(IReadOnlyList<T> list) => _list = list ?? throw new ArgumentNullException(nameof(list));

			public T this[int index] => _list[index];

			public int Count => _list.Count;

			public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

			public override string ToString() => "RO:" + _list.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		public readonly struct ListValueWrap<T>: IReadOnlyList<T>
		{
			public static readonly ListValueWrap<T> Empty = new ListValueWrap<T>(Array.Empty<T>());
			private readonly IReadOnlyList<T> _list;

			public ListValueWrap(IReadOnlyList<T> list) => _list = list ?? throw new ArgumentNullException(nameof(list));

			public T this[int index]
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get { return _list[index]; }
			}

			public int Count
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get { return _list.Count; }
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

			public override bool Equals(object obj) => obj is ListValueWrap<T> other && Equals(other);

			public bool Equals(ListValueWrap<T> other) => ReferenceEquals(_list, other._list);

			public override int GetHashCode() => _list.GetHashCode();

			public static bool operator ==(ListValueWrap<T> left, ListValueWrap<T> right) => left.Equals(right);

			public static bool operator !=(ListValueWrap<T> left, ListValueWrap<T> right) => !(left == right);

			public override string ToString() => "RO:" + _list.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlySafeListWrap<T>: IWrappedSafeList<T>
		{
			private readonly ISafeList<T> _list;

			public ReadOnlySafeListWrap(ISafeList<T> list) => _list = list ?? throw new ArgumentNullException(nameof(list));

			public int IndexOf(T item) => _list.IndexOf(item);

			public void Insert(int index, T item) => throw new ReadOnlyException();

			public void RemoveAt(int index) => throw new ReadOnlyException();

			public T this[int index] { get => _list[index]; set => throw new ReadOnlyException(); }

			public void Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item) => _list.Contains(item);

			public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

			public int Count => _list.Count;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));

				if (_list is ICollection xx)
				{
					xx.CopyTo(array, index);
				}
				else if (array is T[] values)
				{
					_list.CopyTo(values, index);
				}
				else
				{
					values = new T[_list.Count];
					_list.CopyTo(values, 0);
					Array.Copy(values, 0, array, index, values.Length);
				}
			}

			public bool IsSynchronized => (_list as ICollection)?.IsSynchronized == true;

			public object SyncRoot => (_list as ICollection)?.SyncRoot ?? _list;

			public override string ToString() => "RO:" + _list.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlySafeListFragmentWrap<T>: IWrappedSafeList<T>
		{
			private readonly ISafeList<T> _list;
			private readonly int _start;
			private readonly int _end;

			public ReadOnlySafeListFragmentWrap(ISafeList<T> list, int offset, int count)
			{
				_list = list ?? throw new ArgumentNullException(nameof(list));
				if (offset < 0)
					offset = 0;
				else if (offset >= list.Count)
					offset = list.Count;
				_start = offset;

				if (count < 0)
				{
					_end = -1;
				}
				else
				{
					_end = offset + count;
					if (_end > list.Count)
						_end = list.Count;
				}
			}

			public int IndexOf(T item)
			{
				int end = _end < 0 ? _list.Count: _end;
				for (int i = _start; i < end; ++i)
				{
					if (Object.Equals(_list[i], item))
						return i;
				}
				return -1;
			}

			public void Insert(int index, T item) => throw new ReadOnlyException();

			public void RemoveAt(int index) => throw new ReadOnlyException();

			public T this[int index]
			{
				get
				{
					int i = index + _start;
					if (i < 0 || i >= (_end < 0 ? _list.Count: _end))
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
					return _list[i];
				}
				set => throw new ReadOnlyException();
			}

			public void Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item) => IndexOf(item) >= 0;

			public void CopyTo(T[] array, int arrayIndex)
			{
				int end = (_end < 0 ? _list.Count: _end);
				if (arrayIndex < 0 || array.Length - arrayIndex < end - _start)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, null);

				for (int i = _start; i < end; ++i)
				{
					array[i - _start] = _list[i];
				}
			}

			public int Count => (_end < 0 ? _list.Count: _end) - _start;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator()
			{
				int end = (_end < 0 ? _list.Count: _end);
				for (int i = _start; i < end; ++i)
				{
					yield return _list[i];
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				int end = (_end < 0 ? _list.Count: _end);
				for (int i = _start; i < end; ++i)
				{
					yield return _list[i];
				}
			}

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				int end = (_end < 0 ? _list.Count: _end);
				if (index < 0 || array.Length - index < end - _start)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);

				for (int i = _start; i < end; ++i)
				{
					array.SetValue(_list[i], i - _start);
				}
			}

			public bool IsSynchronized => (_list as ICollection)?.IsSynchronized == true;

			public object SyncRoot => (_list as ICollection)?.SyncRoot ?? _list;

			public override string ToString() => "RO:" + _list.ToString();
		}

		private class EmptySafeListInstance<T>: ISafeList<T>
		{
			public T this[int index]
			{
				get { return default; }
				set { }
			}

			public int IndexOf(T item) => -1;

			public void Insert(int index, T item) => throw new ReadOnlyException();

			public void RemoveAt(int index) => throw new ReadOnlyException();

			public void Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item) => false;

			public void CopyTo(T[] array, int arrayIndex)
			{
			}

			public int Count => 0;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator() => Empty<T>().GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => Empty<T>().GetEnumerator();

			public override string ToString() => "RO:" + base.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlyCollectionWrap<T>: IWrappedCollection<T>
		{
			private readonly ICollection<T> _collection;

			public ReadOnlyCollectionWrap(ICollection<T> collection) => _collection = collection ?? throw new ArgumentNullException(nameof(collection));

			public void Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item) => _collection.Contains(item);

			public void CopyTo(T[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

			public int Count => _collection.Count;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));

				if (_collection is ICollection xx)
				{
					xx.CopyTo(array, index);
				}
				else if (array is T[] values)
				{
					_collection.CopyTo(values, index);
				}
				else
				{
					values = new T[_collection.Count];
					_collection.CopyTo(values, 0);
					Array.Copy(values, 0, array, index, values.Length);
				}
			}

			public bool IsSynchronized => (_collection as ICollection)?.IsSynchronized == true;

			public object SyncRoot => (_collection as ICollection)?.SyncRoot ?? _collection;

			public override string ToString() => "RO:" + _collection.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlyCollectionReWrap<T> : IReadOnlyCollection<T>
		{
			private readonly IReadOnlyCollection<T> _collection;

			public ReadOnlyCollectionReWrap(IReadOnlyCollection<T> collection) => _collection = collection ?? throw new ArgumentNullException(nameof(collection));

			public int Count => _collection.Count;

			public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

			public override string ToString() => "RO:" + _collection.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		public readonly struct CollectionValueWrap<T>: IReadOnlyCollection<T>
		{
			public static readonly CollectionValueWrap<T> Empty = new CollectionValueWrap<T>(Array.Empty<T>());

			private readonly IReadOnlyCollection<T> _collection;

			public CollectionValueWrap(IReadOnlyCollection<T> collection) => _collection = collection ?? throw new ArgumentNullException(nameof(collection));

			public int Count => _collection.Count;

			public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

			public override bool Equals(object obj) => obj is CollectionValueWrap<T> other && Equals(other);

			public bool Equals(CollectionValueWrap<T> other) => ReferenceEquals(_collection, other._collection);

			public override int GetHashCode() => _collection.GetHashCode();

			public static bool operator ==(CollectionValueWrap<T> left, CollectionValueWrap<T> right) => left.Equals(right);

			public static bool operator !=(CollectionValueWrap<T> left, CollectionValueWrap<T> right) => !(left == right);

			public override string ToString() => "RO:" + _collection.ToString();
		}

		private class ReadOnlyEnumerableWrap<T>: IEnumerable<T>
		{
			private readonly IEnumerable<T> _enumerable;

			public ReadOnlyEnumerableWrap(IEnumerable<T> enumerable) => _enumerable = enumerable?.AsEnumerable() ?? throw new ArgumentNullException(nameof(enumerable));

			public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _enumerable.GetEnumerator();

			public override string ToString() => "RO:" + _enumerable.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlyArrayWrap<T>: IWrappedList<T>
		{
			private readonly T[] _array;

			public ReadOnlyArrayWrap(T[] array) => _array = array ?? throw new ArgumentNullException(nameof(array));

			public int IndexOf(T item) => Array.IndexOf(_array, item);

			public void Insert(int index, T item) => throw new ReadOnlyException();

			public void RemoveAt(int index) => throw new ReadOnlyException();

			public T this[int index]
			{
				get => _array[index];
				set => throw new ReadOnlyException();
			}

			public void Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item) => Array.IndexOf(_array, item) >= 0;

			public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_array, 0, array, arrayIndex, _array.Length);

			public int Count => _array.Length;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _array.GetEnumerator();

			public void CopyTo(Array array, int index) => Array.Copy(_array, 0, array, index, _array.Length);

			public bool IsSynchronized => false;

			public object SyncRoot => _array;

			public override string ToString() => "RO:" + _array.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlyArrayFragmentWrap<T>: IWrappedList<T>
		{
			private readonly T[] _array;
			private readonly int _start;
			private readonly int _end;

			public ReadOnlyArrayFragmentWrap(T[] array, int offset, int count)
			{
				_array = array ?? throw new ArgumentNullException(nameof(array));
				if (offset < 0)
					offset = 0;
				else if (offset >= array.Length)
					offset = array.Length;
				_start = offset;

				if (count < 0)
				{
					_end = array.Length;
				}
				else
				{
					_end = offset + count;
					if (_end > array.Length)
						_end = array.Length;
				}
			}

			public int IndexOf(T item)
			{
				int index = Array.IndexOf(_array, item, _start);
				return index >= _end ? -1: index - _start;
			}

			public void Insert(int index, T item) => throw new ReadOnlyException();

			public void RemoveAt(int index) => throw new ReadOnlyException();

			public T this[int index]
			{
				get
				{
					if (index < 0 || index >= _end - _start)
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
					return _array[index + _start];
				}
				set => throw new ReadOnlyException();
			}

			public void Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item)
			{
				int index = Array.IndexOf(_array, item, _start);
				return index >= 0 && index < _end;
			}

			public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_array, _start, array, arrayIndex, _end - _start);

			public int Count => _end - _start;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator()
			{
				for (int i = _start; i < _end; ++i)
				{
					yield return _array[i];
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				for (int i = _start; i < _end; ++i)
				{
					yield return _array[i];
				}
			}

			public void CopyTo(Array array, int index) => Array.Copy(_array, _start, array, index, _end - _start);

			public bool IsSynchronized => false;

			public object SyncRoot => _array;

			public override string ToString() => $"RO({_start},{_end}):" + _array.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlyListFragmentWrap<T>: IWrappedList<T>
		{
			private readonly IList<T> _list;
			private readonly int _start;
			private readonly int _end;

			public ReadOnlyListFragmentWrap(IList<T> list, int offset, int count)
			{
				_list = list ?? throw new ArgumentNullException(nameof(list));
				if (offset < 0)
					offset = 0;
				else if (offset >= list.Count)
					offset = list.Count;
				_start = offset;

				if (count < 0)
				{
					_end = -1;
				}
				else
				{
					_end = offset + count;
					if (_end > list.Count)
						_end = list.Count;
				}
			}

			public int IndexOf(T item)
			{
				int end = _end < 0 ? _list.Count: _end;
				for (int i = _start; i < end; ++i)
				{
					if (Object.Equals(_list[i], item))
						return i;
				}
				return -1;
			}

			public void Insert(int index, T item) => throw new ReadOnlyException();

			public void RemoveAt(int index) => throw new ReadOnlyException();

			public T this[int index]
			{
				get
				{
					int i = index + _start;
					if (i < 0 || i >= (_end < 0 ? _list.Count: _end))
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
					return _list[i];
				}
				set => throw new ReadOnlyException();
			}

			public void Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item) => IndexOf(item) >= 0;

			public void CopyTo(T[] array, int arrayIndex)
			{
				int end = (_end < 0 ? _list.Count: _end);
				if (arrayIndex < 0 || array.Length - arrayIndex < end - _start)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, null);

				for (int i = _start; i < end; ++i)
				{
					array[i - _start] = _list[i];
				}
			}

			public int Count => (_end < 0 ? _list.Count: _end) - _start;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator()
			{
				int end = (_end < 0 ? _list.Count: _end);
				for (int i = _start; i < end; ++i)
				{
					yield return _list[i];
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				int end = (_end < 0 ? _list.Count: _end);
				for (int i = _start; i < end; ++i)
				{
					yield return _list[i];
				}
			}

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));
				int end = (_end < 0 ? _list.Count: _end);
				if (index < 0 || array.Length - index < end - _start)
					throw new ArgumentOutOfRangeException(nameof(index), index, null);

				for (int i = _start; i < end; ++i)
				{
					array.SetValue(_list[i], i - _start);
				}
			}

			public bool IsSynchronized => (_list as ICollection)?.IsSynchronized == true;

			public object SyncRoot => (_list as ICollection)?.SyncRoot ?? _list;

			public override string ToString() => $"RO({_start},{_end}):" + _list.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlyListFragmentReWrap<T> : IReadOnlyList<T>
		{
			private readonly IReadOnlyList<T> _list;
			private readonly int _start;
			private readonly int _end;

			public ReadOnlyListFragmentReWrap(IReadOnlyList<T> list, int offset, int count)
			{
				_list = list ?? throw new ArgumentNullException(nameof(list));
				if (offset < 0)
					offset = 0;
				else if (offset >= list.Count)
					offset = list.Count;
				_start = offset;

				if (count < 0)
				{
					_end = -1;
				}
				else
				{
					_end = offset + count;
					if (_end > list.Count)
						_end = list.Count;
				}
			}

			public T this[int index]
			{
				get
				{
					int i = index + _start;
					if (i < 0 || i >= (_end < 0 ? _list.Count : _end))
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
					return _list[i];
				}
			}

			public int Count => (_end < 0 ? _list.Count : _end) - _start;

			public IEnumerator<T> GetEnumerator()
			{
				int end = (_end < 0 ? _list.Count : _end);
				for (int i = _start; i < end; ++i)
				{
					yield return _list[i];
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				int end = (_end < 0 ? _list.Count : _end);
				for (int i = _start; i < end; ++i)
				{
					yield return _list[i];
				}
			}

			public override string ToString() => $"RO({_start},{_end}):" + _list.ToString();
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		public readonly struct ListFragmentValueWrap<T>: IReadOnlyList<T>
		{
			public static readonly ListFragmentValueWrap<T> Empty = new ListFragmentValueWrap<T>(Array.Empty<T>(), 0, 0);

			private readonly IReadOnlyList<T> _list;
			private readonly int _start;
			private readonly int _end;

			public ListFragmentValueWrap(IReadOnlyList<T> list, int offset, int count)
			{
				_list = list ?? throw new ArgumentNullException(nameof(list));
				if (offset < 0)
					offset = 0;
				else if (offset >= list.Count)
					offset = list.Count;
				_start = offset;

				if (count < 0)
				{
					_end = -1;
				}
				else
				{
					_end = offset + count;
					if (_end > list.Count)
						_end = list.Count;
				}
			}

			public T this[int index]
			{
				get
				{
					int i = index + _start;
					if (i < 0 || i >= (_end < 0 ? _list.Count : _end))
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
					return _list[i];
				}
			}

			public int Count => (_end < 0 ? _list.Count : _end) - _start;

			public IEnumerator<T> GetEnumerator()
			{
				int end = (_end < 0 ? _list.Count : _end);
				for (int i = _start; i < end; ++i)
				{
					yield return _list[i];
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				int end = (_end < 0 ? _list.Count : _end);
				for (int i = _start; i < end; ++i)
				{
					yield return _list[i];
				}
			}

			public override string ToString() => $"RO({_start},{_end}):" + _list.ToString();

			public override bool Equals(object obj) => obj is ListFragmentValueWrap<T> other && Equals(other);

			public bool Equals(ListFragmentValueWrap<T> other) => ReferenceEquals(_list, other._list) && _start == other._start && _end == other._end;

			public override int GetHashCode() => (_list, _start, _end).GetHashCode();

			public static bool operator ==(ListFragmentValueWrap<T> left, ListFragmentValueWrap<T> right) => left.Equals(right);

			public static bool operator !=(ListFragmentValueWrap<T> left, ListFragmentValueWrap<T> right) => !(left == right);
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		private class ReadOnlySetWrap<T>: IWrappedSet<T>
		{
			readonly ISet<T> _set;

			public ReadOnlySetWrap(ISet<T> set) => _set = set ?? throw new ArgumentNullException(nameof(set));

			public bool Add(T item) => throw new ReadOnlyException();

			public void ExceptWith(IEnumerable<T> other) => throw new ReadOnlyException();

			public void IntersectWith(IEnumerable<T> other) => throw new ReadOnlyException();

			public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

			public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

			public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

			public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

			public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

			public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

			public void SymmetricExceptWith(IEnumerable<T> other) => throw new ReadOnlyException();

			public void UnionWith(IEnumerable<T> other) => throw new ReadOnlyException();

			void ICollection<T>.Add(T item) => throw new ReadOnlyException();

			public void Clear() => throw new ReadOnlyException();

			public bool Contains(T item) => _set.Contains(item);

			public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

			public int Count => _set.Count;

			public bool IsReadOnly => true;

			public bool Remove(T item) => throw new ReadOnlyException();

			public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();

			public void CopyTo(Array array, int index)
			{
				if (array == null)
					throw new ArgumentNullException(nameof(array));

				if (_set is ICollection xx)
				{
					xx.CopyTo(array, index);
				}
				else if (array is T[] values)
				{
					_set.CopyTo(values, index);
				}
				else
				{
					values = new T[_set.Count];
					_set.CopyTo(values, 0);
					Array.Copy(values, 0, array, index, values.Length);
				}
			}

			public bool IsSynchronized => (_set as ICollection)?.IsSynchronized == true;

			public object SyncRoot => (_set as ICollection)?.SyncRoot ?? _set;

			public override string ToString() => "RO:" + _set.ToString();
		}

		#endregion
	}

	public class ReadOnlyException: NotSupportedException
	{
		public ReadOnlyException(): base(SR.ReadOnlyException())
		{
		}

		public ReadOnlyException(object value): base(SR.ReadOnlyException(value))
		{
		}

		public ReadOnlyException(object value, object item): base(SR.ReadOnlyException(value, item))
		{
		}
	}
}
