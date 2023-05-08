// Lexxys Infrastructural library.
// file: DebugView.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Diagnostics;

namespace Lexxys;

class CollectionDebugView<T>
{
	readonly ICollection<T> _data;

	public CollectionDebugView(ICollection<T> data)
	{
		_data = data;
	}

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Values => _data.ToArray();
}

class ReadOnlyCollectionDebugView<T>
{
	readonly IReadOnlyCollection<T> _data;

	public ReadOnlyCollectionDebugView(IReadOnlyCollection<T> data)
	{
		_data = data;
	}

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Values => _data.ToArray();
}

class DictionaryDebugView<TKey, TValue>
{
	private readonly IDictionary<TKey, TValue> _data;

	public DictionaryDebugView(IDictionary<TKey, TValue> data)
	{
		_data = data;
	}

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public KeyValuePair<TKey, TValue>[] Items
	{
		get
		{
			var tmp = new KeyValuePair<TKey, TValue>[_data.Count];
			_data.CopyTo(tmp, 0);
			return tmp;
		}
	}
}


