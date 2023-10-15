using System.Collections;

namespace Lexxys
{
	[Serializable]
	public class SafeDictionary<TKey, TValue>: ISafeDictionary<TKey, TValue> where TKey: notnull
	{
		private readonly Dictionary<TKey, TValue> _map;
		private readonly TValue _emptyValue;

		public SafeDictionary(TValue emptyValue = default!)
		{
			_map = new Dictionary<TKey, TValue>();
			_emptyValue = emptyValue;
		}

		public SafeDictionary(IEqualityComparer<TKey> comparer, TValue emptyValue = default!)
		{
			_map = new Dictionary<TKey, TValue>(comparer);
			_emptyValue = emptyValue;
		}

		public SafeDictionary(int count, TValue emptyValue = default!)
		{
			_map = new Dictionary<TKey, TValue>(count);
			_emptyValue = emptyValue;
		}

		public SafeDictionary(IDictionary<TKey, TValue> dictionary, TValue emptyValue = default!)
		{
			_map = new Dictionary<TKey, TValue>(dictionary);
			_emptyValue = emptyValue;
		}

		public SafeDictionary(int capacity, IEqualityComparer<TKey> comparer, TValue emptyValue = default!)
		{
			_map = new Dictionary<TKey, TValue>(capacity, comparer);
			_emptyValue = emptyValue;
		}

		public SafeDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer, TValue emptyValue = default!)
		{
			_map = new Dictionary<TKey, TValue>(dictionary, comparer);
			_emptyValue = emptyValue;
		}

		public TValue this[TKey key] { get => _map.TryGetValue(key, out var v) ? v: _emptyValue; set => _map[key] = value; }

		public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)_map).Keys;

		public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)_map).Values;

		public int Count => _map.Count;

		public bool IsReadOnly => ((IDictionary<TKey, TValue>)_map).IsReadOnly;

		public void Add(TKey key, TValue value) => _map[key] = value;

		public void Add(KeyValuePair<TKey, TValue> item) => _map[item.Key] = item.Value;

		public void Clear() => _map.Clear();

		public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_map).Contains(item);

		public bool ContainsKey(TKey key) => _map.ContainsKey(key);

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_map).CopyTo(array, arrayIndex);

		public bool Remove(TKey key) => _map.Remove(key);

		public bool Remove(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_map).Remove(item);

		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _map.TryGetValue(key, out value);

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => ((IDictionary<TKey, TValue>)_map).GetEnumerator();

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IReadOnlyDictionary<TKey, TValue>)_map).Keys;

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ((IReadOnlyDictionary<TKey, TValue>)_map).Values;

		IEnumerator IEnumerable.GetEnumerator() => ((IDictionary<TKey, TValue>)_map).GetEnumerator();
	}
}
