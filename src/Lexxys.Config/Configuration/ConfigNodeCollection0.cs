#if false
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lexxys.Configuration;

/// <summary>
/// Represents an immutable, read-only collection of configuration nodes, each optionally associated with a key.
/// Provides indexed and key-based access to configuration data, supporting both array-like and map-like structures.
/// </summary>
/// <remarks>A ConfigNodeCollection0 can contain nodes with or without associated keys, allowing it to represent
/// arrays, maps, or mixed collections. The collection is immutable after creation. Access by key is case-sensitive and
/// uses ordinal string comparison. Attempting to retrieve a node by a key that does not exist will result in a
/// KeyNotFoundException. The collection supports enumeration of all nodes, as well as specialized views for key-based
/// or value-based access.</remarks>
public class ConfigNodeCollection0: IEquatable<ConfigNodeCollection0>, IReadOnlyList<(string? Key, ConfigNode Value)>, IDump //, IReadOnlyCollection<ConfigNode>, IReadOnlyDictionary<string, ConfigNode>//, IList<KeyValuePair<string?, ConfigSourceNode>>
{
	private readonly (string? Key, ConfigNode Value)[] _nodes;
	private readonly FrozenDictionary<string, int> _map;
	private readonly int _hashCode;

	/// <summary>
	/// Initializes a new, empty instance of the ConfigNodeCollection0 class.
	/// </summary>
	public ConfigNodeCollection0()
	{
		_nodes = [];
		_map = FrozenDictionary<string, int>.Empty;
	}

	/// <summary>
	/// Initializes a new instance of the ConfigNodeCollection0 class from a sequence of key-value pairs.
	/// </summary>
	/// <remarks>If the provided sequence is null, the collection is initialized as empty. When multiple nodes have
	/// the same key, their values are merged into a nested ConfigNode under that key. Keys that are null are
	/// ignored.</remarks>
	/// <param name="nodes">A sequence of tuples where each tuple contains an optional key and a ConfigNode value to include in the collection.
	/// If multiple entries share the same key, their values are combined into a single ConfigNode.</param>
	public ConfigNodeCollection0(IEnumerable<(string? Key, ConfigNode Value)>? nodes)
	{
		if (nodes is null)
		{
			_nodes = [];
			_map = FrozenDictionary<string, int>.Empty;
			return;
		}
		var temp = nodes.ToList();
		var map = new Dictionary<string, int>(StringComparer.Ordinal);
		var combined = new List<ConfigNode>();
		_hashCode = 0;
		
		for (int i = 0; i < temp.Count; ++i)
		{
			var (key, value) = temp[i];
			_hashCode = HashCode.Join(_hashCode, value.GetHashCode());
			if (key == null)
				continue;
			_hashCode = HashCode.Join(_hashCode, key.GetHashCode());
			int j = temp.FindIndex(i + 1, o => o.Key == key);
			if (j >= 0)
			{
				combined.Clear();
				combined.Add(value);
				combined.Add(temp[j].Value);
				temp.RemoveAt(j);
				while ((j = temp.FindIndex(j, o => o.Key == key)) >= 0)
				{
					combined.Add(temp[j].Value);
					temp.RemoveAt(j);
				}
				temp[i] = (key, new ConfigNode(new ConfigNodeCollection(combined)));
			}
			_hashCode = HashCode.Join(_hashCode, key.GetHashCode());
			map.Add(key, i);
		}
		_nodes = temp.ToArray();
		_map = map.ToFrozenDictionary();
	}

	/// <summary>
	/// Initializes a new instance of the ConfigNodeCollection0 class with the specified collection of configuration nodes.
	/// </summary>
	/// <param name="nodes">The collection of ConfigNode objects to include in the collection. If null, the collection will be initialized as
	/// empty.</param>
	public ConfigNodeCollection0(IEnumerable<ConfigNode>? nodes)
	{
		_nodes = nodes == null ? []: [.. nodes.Select(n => ((string?)null, n))];
		_map = FrozenDictionary<string, int>.Empty;
	}

	/// <summary>
	/// Gets the configuration node at the specified index.
	/// </summary>
	/// <remarks>The index must be within the bounds of the collection. This property provides array-like access to
	/// the configuration nodes.</remarks>
	/// <param name="index">The zero-based index of the configuration node to retrieve.</param>
	/// <returns>The configuration node located at the specified index.</returns>
	public ConfigNode this[int index] => _nodes[index].Value;

	/// <summary>
	/// Gets the configuration node associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the configuration node to retrieve.</param>
	/// <returns>The configuration node associated with the specified key.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when a configuration node with the specified key does not exist.</exception>
	public ConfigNode this[string key] => _nodes[_map[key]].Value;

	(string? Key, ConfigNode Value) IReadOnlyList<(string? Key, ConfigNode Value)>.this[int index] => _nodes[index];

	public int Count => _nodes.Length;

	public bool IsArray => _nodes.Length > 0 && _map.Count == 0;

	public bool IsMap => _nodes.Length > 0 && _map.Count == _nodes.Length;

	public bool IsMixed => _map.Count > 0 && _map.Count < _nodes.Length;

	public bool ContainsKey([NotNullWhen(true)] string? key) => key != null && _map.ContainsKey(key);

	public bool TryGetValue([NotNullWhen(true)] string? key, out ConfigNode value)
	{
		if (key != null && _map.TryGetValue(key, out int index))
		{
			value = _nodes[index].Value;
			return true;
		}
		value = default;
		return false;
	}

	/// <summary>
	/// Returns a read-only dictionary view of the configuration collection, allowing access to nodes by their keys.
	/// </summary>
	/// <returns>A <see cref="DictionaryWrapper"/> that provides a read-only dictionary interface to the configuration collection.</returns>
	public DictionaryWrapper AsDictionary() => new DictionaryWrapper(this);

	/// <summary>
	/// Returns a collection containing all values in the current configuration collection.
	/// </summary>
	/// <remarks>The returned collection provides a read-only view of the configuration collection's values.</remarks>
	/// <returns>A <see cref="ValueCollection"/> that contains all values in the configuration collection.</returns>
	public ValueCollection AsCollection() => new ValueCollection(this);

	public bool Equals(ConfigNodeCollection0? other)
	{
		if (other is null) return false;
		if (Count != other.Count) return false;
		if (GetHashCode() != other.GetHashCode()) return false;
		for (int i = 0; i < Count; ++i)
		{
			var (k1, v1) = _nodes[i];
			var (k2, v2) = other._nodes[i];
			if (k1 != k2 || v1 != v2)
				return false;
		}
		return true;
	}

	public override bool Equals(object? obj) => obj is ConfigNodeCollection0 other && Equals(other);

	public override int GetHashCode() => _hashCode;

	public override string ToString() => ToString(new StringBuilder()).ToString();

	public StringBuilder ToString(StringBuilder text)
	{
		if (this.IsEmpty) return text.Append("[]");

		string comma = string.Empty;

		text.Append(IsArray ? '[' : IsMap ? '{' : '(');
		foreach (var (key, value) in _nodes)
		{
			text.Append(comma);
			if (key != null)
				text.Append(key).Append(": ");
			value.ToString(text);
			comma = ", ";
		}
		text.Append(IsArray ? ']' : IsMap ? '}' : ')');

		return text;
	}

	public static bool operator ==(ConfigNodeCollection0 left, ConfigNodeCollection0 right) => left.Equals(right);
	public static bool operator !=(ConfigNodeCollection0 left, ConfigNodeCollection0 right) => !left.Equals(right);

	public IEnumerator<(string? Key, ConfigNode Value)> GetEnumerator() => ((IEnumerable<(string? Key, ConfigNode Value)>)_nodes).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

	public DumpWriter DumpContent(DumpWriter writer)
	{
		char pad = IsArray ? '[': IsMap ? '{' : '(';
		foreach (var (key, value) in _nodes)
		{
			writer.Text(pad);
			if (key == null)
				writer.DumpContent(value);
			else
				writer.Text(key).Text('=').DumpContent(value);
			pad = ',';
		}
		writer.Text(IsArray ? ']': IsMap ? '}' : ')');
		return writer;
	}

	public readonly struct DictionaryWrapper(ConfigNodeCollection0 collection): IReadOnlyDictionary<string, ConfigNode>
	{
		private readonly ConfigNodeCollection0 _collection = collection;

		public ConfigNode this[string key] => _collection._map.TryGetValue(key, out var index) ? _collection._nodes[index].Value : throw new KeyNotFoundException();

		public int Count => _collection.Count;

		public ImmutableArray<string> Keys => _collection._map.Keys;
		IEnumerable<string> IReadOnlyDictionary<string, ConfigNode>.Keys => Keys;

		public DictionaryValueCollection Values => new DictionaryValueCollection(_collection);
	
		IEnumerable<ConfigNode> IReadOnlyDictionary<string, ConfigNode>.Values => Values;

		public bool ContainsKey(string key) => _collection.ContainsKey(key);

		public bool TryGetValue(string key, out ConfigNode value) => _collection.TryGetValue(key, out value);

		public Enumerator GetEnumerator() => new Enumerator(_collection._nodes);

		IEnumerator<KeyValuePair<string, ConfigNode>> IEnumerable<KeyValuePair<string, ConfigNode>>.GetEnumerator() => new Enumerator(_collection._nodes);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator((string? Key, ConfigNode Value)[] nodes): IEnumerator<KeyValuePair<string, ConfigNode>>
		{
			private readonly (string? Key, ConfigNode Value)[] _nodes = nodes;
			private int _index = -1;

			public readonly KeyValuePair<string, ConfigNode> Current
			{
				get
				{
					var (k, v) = _nodes[_index];
					return new KeyValuePair<string, ConfigNode>(k!, v);
				}
			}

			readonly object IEnumerator.Current => Current;

			public readonly void Dispose()
			{
			}

			public bool MoveNext()
			{
				while (++_index < _nodes.Length)
				{
					if (_nodes[_index].Key != null)
						return true;
				}
				return false;
			}

			public void Reset() => _index = -1;
		}

		public readonly struct DictionaryValueCollection(ConfigNodeCollection0 collection): IReadOnlyCollection<ConfigNode>
		{
			private readonly ConfigNodeCollection0 _collection = collection;

			public int Count => _collection._map.Count;

			public DictionaryEnumerator GetEnumerator() => new DictionaryEnumerator(_collection._nodes);

			IEnumerator<ConfigNode> IEnumerable<ConfigNode>.GetEnumerator() => GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public struct DictionaryEnumerator((string? Key, ConfigNode Value)[] nodes): IEnumerator<ConfigNode>
			{
				private readonly (string? Key, ConfigNode Value)[] _nodes = nodes;
				private int _index = -1;

				public readonly ConfigNode Current => _nodes[_index].Value;

				readonly object IEnumerator.Current => Current;

				public readonly void Dispose()
				{
				}

				public bool MoveNext()
				{
					while (++_index < _nodes.Length)
					{
						if (_nodes[_index].Key != null)
							return true;
					}
					return false;
				}

				public void Reset() => _index = -1;
			}
		}
	}

	public readonly struct ValueCollection(ConfigNodeCollection0 collection): IReadOnlyList<ConfigNode>
	{
		private readonly (string? Key, ConfigNode Value)[] _nodes = collection._nodes;

		public int Count => _nodes.Length - (collection._map.Count);

		public ConfigNode this[int index] => _nodes[index].Value;

		public Enumerator GetEnumerator() => new Enumerator(_nodes);

		IEnumerator<ConfigNode> IEnumerable<ConfigNode>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator((string? Key, ConfigNode Value)[] nodes): IEnumerator<ConfigNode>
		{
			private readonly (string? Key, ConfigNode Value)[] _nodes = nodes;
			private int _index = -1;

			public readonly ConfigNode Current => _nodes[_index].Value;

			readonly object IEnumerator.Current => Current;

			public bool MoveNext() => ++_index < _nodes.Length;

			public void Reset() => _index = -1;

			public readonly void Dispose() { }
		}
	}
}


public static partial class ConfigNodeExtensions
{
	extension([NotNullWhen(false)] ConfigNodeCollection0? collection)
	{
		public bool IsEmpty => collection is null || collection.Count == 0;
	}
}
#endif
