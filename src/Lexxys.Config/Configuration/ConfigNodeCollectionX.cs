#if false
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Lexxys.Configuration;

/// <summary>
/// Represents an ordered collection of configuration nodes that can be accessed by index or by key, supporting both
/// array-like and map-like structures.
/// </summary>
/// <remarks>
/// A ConfigNodeCollectionX can contain nodes with or without associated string keys, allowing it to
/// represent arrays, maps, or mixed collections. The collection preserves the order of insertion. Use the IsArray,
/// IsMap, and IsMixed properties to determine the structure of the collection. Access nodes by index for array elements
/// or by key for map entries. Enumeration yields all nodes in insertion order, including both keyed and
/// unkeyed elements.
/// </remarks>
public class ConfigNodeCollectionX: IEquatable<ConfigNodeCollectionX>, IReadOnlyCollection<(string? Key, ConfigNode Value)>, IDump //, IReadOnlyCollection<ConfigNode>, IReadOnlyDictionary<string, ConfigNode>//, IList<KeyValuePair<string?, ConfigSourceNode>>
{
	private readonly List<(string? Key, ConfigNode Value)> _nodes;
	private Dictionary<string, int>? _map;
	private int _hashCode;

	/// <summary>
	/// Initializes a new, empty instance of the ConfigNodeCollectionX class.
	/// </summary>
	public ConfigNodeCollectionX()
	{
		_nodes = [];
	}

	/// <summary>
	/// Initializes a new instance of the ConfigNodeCollectionX class with the specified collection of key-value pairs.
	/// </summary>
	/// <param name="nodes">A sequence of tuples, each containing an optional key and a ConfigNode value to include in the collection. Keys may
	/// be null to indicate unnamed nodes.</param>
	public ConfigNodeCollectionX(IEnumerable<(string? Key, ConfigNode Value)> nodes)
	{
		_nodes = [];
		AddRange(nodes);
	}

	/// <summary>
	/// Initializes a new instance of the ConfigNodeCollectionX class with the specified collection of configuration nodes.
	/// </summary>
	/// <param name="nodes">The collection of ConfigNode instances to include in the collection.</param>
	public ConfigNodeCollectionX(IEnumerable<ConfigNode> nodes)
	{
		_nodes = nodes == null ? []: [.. nodes.Select(n => ((string?)null, n))];
	}

	/// <summary>
	/// Gets the ConfigNode at the specified index in the collection.
	/// </summary>
	/// <param name="index">The zero-based index of the ConfigNode to retrieve.</param>
	/// <returns></returns>
	public ConfigNode this[int index] => _nodes[index].Value;

	/// <summary>
	/// Gets the configuration node associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the configuration node to retrieve.</param>
	/// <returns>The configuration node associated with the specified key.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when a configuration node with the specified key does not exist.</exception>
	public ConfigNode this[string key] => _map != null ? _nodes[_map[key]].Value: throw new KeyNotFoundException();

	public int Count => _nodes.Count;

	public bool IsArray => _map == null;

	public bool IsMap => _map != null && _map.Count == _nodes.Count;

	public bool IsMixed => _map != null && _map.Count < _nodes.Count;

	public bool ContainsKey([NotNullWhen(true)] string? key) => key != null && _map != null && _map.ContainsKey(key);

	public bool TryGetValue([NotNullWhen(true)] string? key, out ConfigNode value)
	{
		if (key != null && _map != null && _map.TryGetValue(key, out int index))
		{
			value = _nodes[index].Value;
			return true;
		}
		value = default;
		return false;
	}

	/// <summary>
	/// Adds a configuration node to the collection without an associated key.
	/// </summary>
	/// <param name="node">The configuration node to add.</param>
	public void Add(ConfigNode node)
	{
		_nodes.Add((null, node));
		_hashCode = 0;
	}

	/// <summary>
	/// Adds a configuration node to the collection, optionally associating it with the specified key.
	/// </summary>
	/// <remarks>If a node with the specified key already exists, it is replaced with the new node. If the key is
	/// null, the node is added without a key association.</remarks>
	/// <param name="key">The key to associate with the configuration node. If null, the node is added without a key.</param>
	/// <param name="node">The configuration node to add to the collection.</param>
	public void Add(string? key, ConfigNode node)
	{
		if (key == null)
		{
			Add(node);
			return;
		}
		(_map ??= []).Add(key, _nodes.Count);
		_nodes.Add((key, node));
		_hashCode = 0;
	}

	/// <summary>
	/// Adds the specified collection of configuration nodes to the current collection.
	/// </summary>
	/// <param name="nodes">The collection of <see cref="ConfigNode"/> objects to add. If <paramref
	/// name="nodes"/> is <see langword="null"/>, no action is taken.</param>
	public void AddRange(IEnumerable<ConfigNode>? nodes)
	{
		if (nodes == null) return;

		foreach (var node in nodes)
		{
			_nodes.Add((null, node));
			_hashCode = 0;
		}
	}

	/// <summary>
	/// Adds a collection of key-value pairs to the configuration.
	/// </summary>
	/// <remarks>Each key-value pair in the collection is added by calling the <c>Add</c> method for each element.
	/// If a key already exists, behavior depends on the implementation of <c>Add</c>.</remarks>
	/// <param name="nodes">The collection of key-value pairs to add. Each pair consists of an optional key and a configuration node value. If
	/// <paramref name="nodes"/> is <see langword="null"/>, no action is taken.</param>
	public void AddRange(IEnumerable<(string? Key, ConfigNode Value)>? nodes)
	{
		if (nodes == null) return;

		foreach (var (key, value) in nodes)
		{
			Add(key, value);
		}
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

	public bool Equals(ConfigNodeCollectionX? other)
	{
		if (other is null) return false;
		if (Count != other.Count) return false;
		if (_hashCode != other._hashCode && _hashCode != 0 && other._hashCode != 0) return false;
		for (int i = 0; i < Count; ++i)
		{
			var (k1, v1) = _nodes[i];
			var (k2, v2) = other._nodes[i];
			if (k1 != k2 || v1 != v2)
				return false;
		}
		return true;
	}

	public override bool Equals(object? obj) => obj is ConfigNodeCollectionX other && Equals(other);

	public override int GetHashCode() => _hashCode != 0 ?
		_hashCode:
		(_hashCode = HashCode.Join(0, _nodes.Select(o => HashCode.Join(o.Key?.GetHashCode() ?? 0, o.Value.GetHashCode()))));

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

	public static bool operator ==(ConfigNodeCollectionX left, ConfigNodeCollectionX right) => left.Equals(right);
	public static bool operator !=(ConfigNodeCollectionX left, ConfigNodeCollectionX right) => !left.Equals(right);

	public IEnumerator<(string? Key, ConfigNode Value)> GetEnumerator() => _nodes.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

	public readonly struct DictionaryWrapper: IReadOnlyDictionary<string, ConfigNode>
	{
		private readonly ConfigNodeCollectionX _collection;

		public DictionaryWrapper(ConfigNodeCollectionX collection)
		{
			_collection = collection;
			_collection._map ??= [];
		}

		public ConfigNode this[string key] => _collection._map!.TryGetValue(key, out var index) ? _collection._nodes[index].Value : throw new KeyNotFoundException();

		public int Count => _collection.Count;

		public Dictionary<string, int>.KeyCollection Keys => _collection._map!.Keys;
		IEnumerable<string> IReadOnlyDictionary<string, ConfigNode>.Keys => Keys;

		public DictionaryValueCollection Values => new DictionaryValueCollection(_collection);
	
		IEnumerable<ConfigNode> IReadOnlyDictionary<string, ConfigNode>.Values => Values;

		public bool ContainsKey(string key) => _collection.ContainsKey(key);

		public bool TryGetValue(string key, out ConfigNode value) => _collection.TryGetValue(key, out value);

		public IEnumerator<KeyValuePair<string, ConfigNode>> GetEnumerator() => new Enumerator(_collection._nodes);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		struct Enumerator(List<(string? Key, ConfigNode Value)> nodes): IEnumerator<KeyValuePair<string, ConfigNode>>
		{
			private readonly List<(string? Key, ConfigNode Value)> _nodes = nodes;
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
				while (++_index < _nodes.Count)
				{
					if (_nodes[_index].Key != null)
						return true;
				}
				return false;
			}

			public void Reset() => _index = -1;
		}

		public readonly struct DictionaryValueCollection(ConfigNodeCollectionX collection): IReadOnlyCollection<ConfigNode>
		{
			private readonly ConfigNodeCollectionX _collection = collection;

			public int Count => _collection._map!.Count;

			public IEnumerator<ConfigNode> GetEnumerator() => new DictionaryEnumerator(_collection._nodes);

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			struct DictionaryEnumerator(List<(string? Key, ConfigNode Value)> nodes): IEnumerator<ConfigNode>
			{
				private readonly List<(string? Key, ConfigNode Value)> _nodes = nodes;
				private int _index = -1;

				public readonly ConfigNode Current => _nodes[_index].Value;

				readonly object IEnumerator.Current => Current;

				public readonly void Dispose()
				{
				}

				public bool MoveNext()
				{
					while (++_index < _nodes.Count)
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

	public readonly struct ValueCollection(ConfigNodeCollectionX collection): IReadOnlyCollection<ConfigNode>
	{
		private readonly ConfigNodeCollectionX _collection = collection;

		public int Count => _collection._nodes.Count - (_collection._map?.Count ?? 0);

		public IEnumerator<ConfigNode> GetEnumerator() => new Enumerator(_collection._nodes);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		struct Enumerator(List<(string? Key, ConfigNode Value)> nodes): IEnumerator<ConfigNode>
		{
			private readonly List<(string? Key, ConfigNode Value)> _nodes = nodes;
			private int _index = -1;

			public readonly ConfigNode Current => _nodes[_index].Value;

			readonly object IEnumerator.Current => Current;

			public readonly void Dispose()
			{
			}

			public bool MoveNext()
			{
				while (++_index < _nodes.Count)
				{
					if (_nodes[_index].Key == null)
						return true;
				}
				return false;
			}

			public void Reset() => _index = -1;
		}
	}
}

public static partial class ConfigNodeExtensions
{
	extension([NotNullWhen(false)] ConfigNodeCollectionX? collection)
	{
		public bool IsEmpty => collection is null || collection.Count == 0;
	}
}
#endif