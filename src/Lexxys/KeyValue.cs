// Lexxys Infrastructural library.
// file: KeyValue.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lexxys
{
	public static class KeyValue
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
			=> new KeyValuePair<TKey, TValue>(key, value);
	}
}


