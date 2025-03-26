// Lexxys Infrastructural library.
// file: DebugView.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Diagnostics;

namespace Lexxys;

class CollectionDebugView<T>(ICollection<T> data)
{
	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Values => [.. data];
}

class ReadOnlyCollectionDebugView<T>(IReadOnlyCollection<T> data)
{
	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Values => [.. data];
}

class DictionaryDebugView<TKey, TValue>(IDictionary<TKey, TValue> data)
{
	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public KeyValuePair<TKey, TValue>[] Items => [.. data];
}


