using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lexxys.Configuration;

/// <summary>
/// Represents an ordered collection of configuration nodes that can be accessed by index or by key, supporting both
/// array-like and map-like structures.
/// </summary>
/// <remarks>
/// A ConfigNodeCollection can contain nodes with or without associated string keys, allowing it to
/// represent arrays, maps, or mixed collections. The collection preserves the order of insertion. Use the IsArray,
/// IsMap, and IsMixed properties to determine the structure of the collection. Access nodes by index for array elements
/// or by key for map entries. Enumeration yields all nodes in insertion order, including both keyed and
/// unkeyed elements.
/// </remarks>
public class ConfigNodeCollection: IEquatable<ConfigNodeCollection>, IReadOnlyList<KeyValuePair<string?, ConfigNode>>, IDumpValue //, IReadOnlyCollection<ConfigNode>, IReadOnlyDictionary<string, ConfigNode>//, IList<KeyValuePair<string?, ConfigSourceNode>>
{
	private readonly KeyValuePair<string?, ConfigNode>[] _nodes;
	private readonly int _hashCode;

	/// <summary>
	/// Initializes a new, empty instance of the ConfigNodeCollection class.
	/// </summary>
	public ConfigNodeCollection(ConfigSourceLocation location = default) => (Location, _nodes) = (location, []);

	/// <summary>
	/// Initializes a new instance of the ConfigNodeCollection class with the specified collection of key-value pairs.
	/// </summary>
	/// <param name="nodes">A sequence of tuples, each containing an optional key and a ConfigNode value to include in the collection. Keys may
	/// be null to indicate unnamed nodes.</param>
	public ConfigNodeCollection(IEnumerable<KeyValuePair<string?, ConfigNode>> nodes, ConfigSourceLocation location = default)
	{
		Location = location;
		if (nodes is null)
		{
			_nodes = [];
			return;
		}
		List<KeyValuePair<string?, ConfigNode>> temp = nodes.ToList();
		var combined = new List<ConfigNode>();

		for (int i = 0; i < temp.Count; ++i)
		{
			var item = temp[i];
			_hashCode = HashCode.Join(_hashCode, item.Value.GetHashCode());
			if (item.Key == null)
				continue;
			_hashCode = HashCode.Join(_hashCode, item.Key.GetHashCode());
			int j = temp.FindIndex(i + 1, o => o.Key == item.Key);
			if (j >= 0)
			{
				combined.Clear();
				combined.Add(item.Value);
				combined.Add(temp[j].Value);
				temp.RemoveAt(j);
				while ((j = temp.FindIndex(j, o => o.Key == item.Key)) >= 0)
				{
					combined.Add(temp[j].Value);
					temp.RemoveAt(j);
				}
				temp[i] = new KeyValuePair<string?, ConfigNode>(item.Key, new ConfigNode(new ConfigNodeCollection(combined, item.Value.Location), item.Value.Location));
			}
			_hashCode = HashCode.Join(_hashCode, item.Key.GetHashCode());
		}
		_nodes = [.. temp];
	}

	public ConfigNodeCollection(IEnumerable<(string? Key, ConfigNode Value)> nodes, ConfigSourceLocation location = default)
		: this(nodes.Select(o => new KeyValuePair<string?, ConfigNode>(o.Key, o.Value)), location)
	{
	}

	/// <summary>
	/// Initializes a new instance of the ConfigNodeCollection class with the specified collection of configuration nodes.
	/// </summary>
	/// <param name="nodes">The collection of ConfigNode instances to include in the collection.</param>
	public ConfigNodeCollection(IEnumerable<ConfigNode> nodes, ConfigSourceLocation location = default)
	{
		Location = location;
		_nodes = nodes == null ? [] : [.. nodes.Select(n => new KeyValuePair<string?, ConfigNode>(null, n))];
	}

	public ConfigSourceLocation Location { get; }

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
	public ConfigNode this[string key]
	{
		get
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var index = FindIndex(key);
			if (index >= 0)
				return _nodes[index].Value;

			throw new KeyNotFoundException($"The given key '{key}' was not present in the collection.");
		}
	}

	public int Count => _nodes.Length;

	public bool IsArray => GetCollectionType() == ConfigNodeCollectionType.Array;

	public bool IsMap => GetCollectionType() == ConfigNodeCollectionType.Map;

	public bool IsMixed => GetCollectionType() == ConfigNodeCollectionType.Mixed;

	KeyValuePair<string?, ConfigNode> IReadOnlyList<KeyValuePair<string?, ConfigNode>>.this[int index] => _nodes[index];

	public bool ContainsKey([NotNullWhen(true)] string? key) => FindIndex(key) >= 0;

	public ConfigNodeCollectionType GetCollectionType()
	{
		bool hasKey = false;
		bool hasNoKey = false;
		foreach (var item in _nodes)
		{
			if (item.Key == null)
				hasNoKey = true;
			else
				hasKey = true;
			if (hasKey && hasNoKey)
				return ConfigNodeCollectionType.Mixed;
		}
		return hasKey ?
			hasNoKey ? ConfigNodeCollectionType.Mixed: ConfigNodeCollectionType.Map:
			hasNoKey ? ConfigNodeCollectionType.Array: ConfigNodeCollectionType.Empty;
	}

	private int FindIndex(string? key)
	{
		if (key == null) return -1;
		for (int i = 0; i < _nodes.Length; ++i)
		{
			if (_nodes[i].Key == key)
				return i;
		}
		return -1;
	}

	public bool TryGetValue([NotNullWhen(true)] string? key, out ConfigNode value)
	{
		int i;
		if (key != null && (i = FindIndex(key)) >= 0)
		{
			value = _nodes[i].Value;
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
	public ValueCollection AsCollection() => new ValueCollection(_nodes);

	public bool Equals(ConfigNodeCollection? other)
	{
		if (other is null) return false;
		if (Count != other.Count) return false;
		if (_hashCode != other._hashCode && _hashCode != 0 && other._hashCode != 0) return false;
		for (int i = 0; i < Count; ++i)
		{
			var item1 = _nodes[i];
			var item2 = other._nodes[i];
			if (item1.Key != item2.Key || item1.Value != item2.Value)
				return false;
		}
		return true;
	}

	public override bool Equals(object? obj) => obj is ConfigNodeCollection other && Equals(other);

	public override int GetHashCode() => _hashCode;

	public override string ToString() => ToString(new StringBuilder()).ToString();

	public StringBuilder ToString(StringBuilder text)
	{
		if (this.IsEmpty) return text.Append("()");

		string comma = string.Empty;

		var collectionType = GetCollectionType();
		text.Append(collectionType == ConfigNodeCollectionType.Array ? '[': collectionType == ConfigNodeCollectionType.Map ? '{': '(');
		foreach (var item in _nodes)
		{
			text.Append(comma);
			if (item.Key != null)
				text.Append(item.Key).Append(": ");
			item.Value.ToString(text);
			comma = ", ";
		}
		text.Append(collectionType == ConfigNodeCollectionType.Array ? ']': collectionType == ConfigNodeCollectionType.Map ? '}': ')');

		return text;
	}

	public static bool operator ==(ConfigNodeCollection left, ConfigNodeCollection right) => left.Equals(right);
	public static bool operator !=(ConfigNodeCollection left, ConfigNodeCollection right) => !left.Equals(right);

	public IEnumerator<KeyValuePair<string?, ConfigNode>> GetEnumerator() => ((IEnumerable<KeyValuePair<string?, ConfigNode>>)_nodes).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void DumpContent(IDumpWriter writer)
	{
		if (this.IsEmpty)
		{
			writer.Begin(type: '(').End();
			return;
		}

		char ch = GetCollectionType() switch
		{
			ConfigNodeCollectionType.Array => '[',
			ConfigNodeCollectionType.Map => '{',
			_ => '('
		};
		writer.Begin(type: ch);
		foreach (var item in _nodes)
		{
			writer.Write(item.Key, item.Value);
		}
		writer.End();
	}

	public readonly struct DictionaryWrapper: IReadOnlyDictionary<string, ConfigNode>
	{
		private readonly ConfigNodeCollection _collection;

		public DictionaryWrapper(ConfigNodeCollection collection)
		{
			_collection = collection;
		}

		public ConfigNode this[string key] => _collection[key];

		public int Count => _collection.Count;

		public DictionaryKeyCollection Keys => new DictionaryKeyCollection(_collection._nodes);

		IEnumerable<string> IReadOnlyDictionary<string, ConfigNode>.Keys => Keys;

		public DictionaryValueCollection Values => new DictionaryValueCollection(_collection._nodes);

		IEnumerable<ConfigNode> IReadOnlyDictionary<string, ConfigNode>.Values => Values;

		public bool ContainsKey(string key) => _collection.ContainsKey(key);

		public bool TryGetValue(string key, out ConfigNode value) => _collection.TryGetValue(key, out value);

		public IEnumerator<KeyValuePair<string, ConfigNode>> GetEnumerator() => new Enumerator(_collection._nodes);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator(KeyValuePair<string?, ConfigNode>[] nodes): IEnumerator<KeyValuePair<string, ConfigNode>>
		{
			private readonly KeyValuePair<string?, ConfigNode>[] _nodes = nodes;
			private int _index = -1;

			public readonly KeyValuePair<string, ConfigNode> Current => _nodes[_index]!;

			readonly object IEnumerator.Current => Current;

			public readonly void Dispose() { }

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

		public struct DictionaryKeyCollection(KeyValuePair<string?, ConfigNode>[] nodes): IEnumerable<string>
		{
			private readonly KeyValuePair<string?, ConfigNode>[] _nodes = nodes;

			public IEnumerator<string> GetEnumerator() => new DictionaryKeyEnumerator(_nodes);

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			struct DictionaryKeyEnumerator(KeyValuePair<string?, ConfigNode>[] nodes): IEnumerator<string>
			{
				private readonly KeyValuePair<string?, ConfigNode>[] _nodes = nodes;
				private int _index = -1;

				public readonly string Current => _nodes[_index].Key!;

				readonly object IEnumerator.Current => Current;

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

				public readonly void Dispose() { }
			}
		}

		public readonly struct DictionaryValueCollection(KeyValuePair<string?, ConfigNode>[] nodes): IReadOnlyCollection<ConfigNode>
		{
			private readonly KeyValuePair<string?, ConfigNode>[] _nodes = nodes;

			public int Count => _nodes.Length;

			public IEnumerator<ConfigNode> GetEnumerator() => new DictionaryEnumerator(_nodes);

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			struct DictionaryEnumerator(KeyValuePair<string?, ConfigNode>[] nodes): IEnumerator<ConfigNode>
			{
				private readonly KeyValuePair<string?, ConfigNode>[] _nodes = nodes;
				private int _index = -1;

				public readonly ConfigNode Current => _nodes[_index].Value;

				readonly object IEnumerator.Current => Current;

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

				public readonly void Dispose() { }
			}
		}
	}

	public readonly struct ValueCollection(KeyValuePair<string?, ConfigNode>[] nodes): IReadOnlyCollection<ConfigNode>
	{
		private readonly KeyValuePair<string?, ConfigNode>[] _nodes = nodes;

		public int Count => _nodes.Length;

		public IEnumerator<ConfigNode> GetEnumerator() => new Enumerator(_nodes);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		struct Enumerator(KeyValuePair<string?, ConfigNode>[] nodes): IEnumerator<ConfigNode>
		{
			private readonly KeyValuePair<string?, ConfigNode>[] _nodes = nodes;
			private int _index = -1;

			public readonly ConfigNode Current => _nodes[_index].Value;

			readonly object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				while (++_index < _nodes.Length)
				{
					if (_nodes[_index].Key == null)
						return true;
				}
				return false;
			}

			public void Reset() => _index = -1;

			public readonly void Dispose() { }
		}
	}
}

public enum ConfigNodeCollectionType
{
	Empty,
	Array,
	Map,
	Mixed
}

public enum ConfigNodeKind
{
	Empty,
	Scalar,
	Array,
	Object,
	Mixed,
	ScalarWithChildren
}

public static partial class ConfigNodeExtensions
{
	extension([NotNullWhen(false)] ConfigNodeCollection? collection)
	{
		public bool IsEmpty => collection is null || collection.Count == 0;
	}
}
