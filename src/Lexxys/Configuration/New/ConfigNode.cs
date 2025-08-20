using System.Collections;
using System.Text;

namespace Lexxys.Configuration.New;

/// <summary>
/// Represents a configuration node that can hold either a single value or a collection of configuration nodes.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ConfigNode"/> is an immutable struct used to model hierarchical configuration data.
/// It can encapsulate a string value, a collection of child nodes, or both.
/// </para>
/// </remarks>
public readonly partial struct ConfigNode: IEquatable<ConfigNode>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigNode"/> struct with an value and optional collection.
	/// </summary>
	/// <param name="value">The string value of the configuration node.</param>
	/// <param name="collection">An optional collection of child configuration nodes.</param>
	public ConfigNode(string? value, ConfigNodeCollection? collection = null) => (Value, Collection) = (value, collection);

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigNode"/> struct with an collection.
	/// </summary>
	/// <param name="collection">The collection of child configuration nodes.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ConfigNode(ConfigNodeCollection collection) => Collection = collection ?? throw new ArgumentNullException(nameof(collection));

	/// <summary>
	/// Gets the string value of the configuration node.
	/// </summary>
	public string? Value { get; }

	/// <summary>
	/// Gets the collection of child configuration nodes.
	/// </summary>
	public ConfigNodeCollection? Collection { get; }

	/// <summary>
	/// Gets a value indicating whether the configuration node is empty.
	/// A node is considered empty if it has no value and no child nodes.
	/// </summary>
	public bool IsEmpty => String.IsNullOrEmpty(Value) && Collection.IsEmpty();

	// internal static ConfigNode Join(IReadOnlyCollection<ConfigNode>? nodes)
	// {
	// 	if (nodes is null || nodes.Count == 0) return new ConfigNode();
	// 	if (nodes.Count == 1) return nodes.FirstOrDefault()!;
	// 	return new ConfigNode(new ConfigNodeCollection(nodes));
	// }

	public bool Equals(ConfigNode other)
	{
		if (Value != other.Value)
			return false;
		if (Collection.IsEmpty())
			return other.Collection.IsEmpty();
		return Collection!.Equals(other.Collection);
	}

	public override bool Equals(object? obj) => obj is ConfigNode other && Equals(other);

	public override int GetHashCode() => Value?.GetHashCode() ?? Collection?.GetHashCode() ?? 0;

	public override string ToString() => Collection is null ? Value?.ToString() ?? String.Empty: ToString(new StringBuilder()).ToString();

	public StringBuilder ToString(StringBuilder text)
	{
		if (Value is null)
			return Collection is null ? text.Append("null"): Collection.ToString(text);

		text.Append(Value);
		return Collection.IsEmpty() ? text: Collection.ToString(text.Append(' '));
	}

	public static bool operator ==(ConfigNode left, ConfigNode right) => left.Equals(right);

	public static bool operator !=(ConfigNode left, ConfigNode right) => !left.Equals(right);
}


public class ConfigNodeCollection: IEquatable<ConfigNodeCollection>, IReadOnlyCollection<(string? Key, ConfigNode Value)> //, IReadOnlyCollection<ConfigNode>, IReadOnlyDictionary<string, ConfigNode>//, IList<KeyValuePair<string?, ConfigSourceNode>>
{
	private readonly List<(string? Key, ConfigNode Value)> _nodes;
	private readonly Dictionary<string, int> _map;

	public ConfigNodeCollection()
	{
		_nodes = [];
		_map = [];
	}

	public ConfigNodeCollection(IEnumerable<(string? Key, ConfigNode Value)> nodes)
	{
		_nodes = [];
		_map = [];
		AddRange(nodes);
	}

	public ConfigNodeCollection(IEnumerable<ConfigNode> nodes)
	{
		if (nodes == null) throw new ArgumentNullException(nameof(nodes));

		_nodes = [.. nodes.Select(n => ((string?)null, n))]; // nodes.Where(n => !n.IsEmpty).Select(n => ((string?)null, n))];
		_map = [];
	}

	// public ConfigNodeCollection(string? key, IEnumerable<ConfigNode> nodes)
	// {
	// 	if (key == null) throw new ArgumentNullException(nameof(key));
	// 	if (nodes == null) throw new ArgumentNullException(nameof(nodes));

	// 	_nodes = [(key, ConfigNode.Join([.. nodes]))];
	// 	_map = [];
	// 	_map[key] = 0;
	// }

	public ConfigNode this[int index] => _nodes[index].Value;

	public ConfigNode this[string key] => _map.TryGetValue(key, out int index) ? _nodes[index].Value : throw new KeyNotFoundException();

	public int Count => _nodes.Count;

	public bool IsArray => _map.Count == 0;

	public bool IsMap => _map.Count == _nodes.Count;

	public bool IsMixed => _map.Count > 0 && _map.Count < _nodes.Count;

	public bool ContainsKey(string? key) => key != null && _map.ContainsKey(key);

	public bool TryGetValue(string key, out ConfigNode value)
	{
		if (key != null && _map.TryGetValue(key, out int index))
		{
			value = _nodes[index].Value;
			return true;
		}
		value = default;
		return false;
	}

	public void Add(ConfigNode node)
	{
		if (!node.IsEmpty)
			_nodes.Add((null, node));
	}

	public void Add(string? key, ConfigNode node)
	{
		if (key == null)
		{
			Add(node);
		}
		else if (_map.TryGetValue(key, out int index))
		{
			// var existing = _nodes[index].Value;
			_nodes[index] = (key, node); // existing.IsEmpty ? node : ConfigNode.Join([existing, node]));
		}
		else
		{
			_map[key] = _nodes.Count;
			_nodes.Add((key, node));
		}
	}

	public void AddRange(IEnumerable<ConfigNode>? nodes)
	{
		if (nodes == null) return;

		foreach (var node in nodes)
		{
			if (!node.IsEmpty)
				_nodes.Add((null, node));
		}
	}

	public void AddRange(IEnumerable<(string? Key, ConfigNode Value)>? nodes)
	{
		if (nodes == null) return;

		foreach (var (key, value) in nodes)
		{
			Add(key, value);
		}
	}

	public DictionaryWrapper AsDictionary() => new DictionaryWrapper(this);

	public ValueCollection AsCollection() => new ValueCollection(this);

	public bool Equals(ConfigNodeCollection? other)
	{
		if (other is null) return false;
		if (Count != other.Count) return false;
		for (int i = 0; i < Count; ++i)
		{
			var (k1, v1) = _nodes[i];
			var (k2, v2) = other._nodes[i];
			if (k1 != k2 || v1 != v2)
				return false;
		}
		return true;
	}

	public override bool Equals(object? obj) => obj is ConfigNodeCollection other && Equals(other);

	public override int GetHashCode() => HashCode.Join(0, _nodes.Select(o => HashCode.Join(o.Key?.GetHashCode() ?? 0, o.Value.GetHashCode())));

	public override string ToString() => ToString(new StringBuilder()).ToString();

	public StringBuilder ToString(StringBuilder text)
	{
		if (this.IsEmpty()) return text.Append("[]");

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

	public static bool operator ==(ConfigNodeCollection left, ConfigNodeCollection right) => left.Equals(right);
	public static bool operator !=(ConfigNodeCollection left, ConfigNodeCollection right) => !left.Equals(right);

	public IEnumerator<(string? Key, ConfigNode Value)> GetEnumerator() => _nodes.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public readonly struct DictionaryWrapper(ConfigNodeCollection collection): IReadOnlyDictionary<string, ConfigNode>
	{
		private readonly ConfigNodeCollection _collection = collection;

		public ConfigNode this[string key] => _collection._map.TryGetValue(key, out var index) ? _collection._nodes[index].Value : throw new KeyNotFoundException();

		public int Count => _collection.Count;

		public Dictionary<string, int>.KeyCollection Keys => _collection._map.Keys;
		IEnumerable<string> IReadOnlyDictionary<string, ConfigNode>.Keys => Keys;

		public ValueCollection Values => new ValueCollection(_collection);
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

		public readonly struct ValueCollection(ConfigNodeCollection collection): IReadOnlyCollection<ConfigNode>
		{
			private readonly ConfigNodeCollection _collection = collection;

			public int Count => _collection._map.Count;

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
						if (_nodes[_index].Key != null)
							return true;
					}
					return false;
				}

				public void Reset() => _index = -1;
			}
		}
	}

	public readonly struct ValueCollection(ConfigNodeCollection collection): IReadOnlyCollection<ConfigNode>
	{
		private readonly ConfigNodeCollection _collection = collection;

		public int Count => _collection._nodes.Count - _collection._map.Count;

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
	public static bool IsEmpty([NotNullWhen(false)] this ConfigNodeCollection? collection) => collection is null || collection.Count == 0;
}