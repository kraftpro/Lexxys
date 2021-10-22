// Lexxys Infrastructural library.
// file: IVersionedValue.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

#nullable disable

namespace Lexxys
{
	public interface IVersionedValue<out T>: IValue<T>
	{
		/// <summary>
		/// Returns a version number starting 0 or -1 no data found.
		/// </summary>
		int Version { get; }
	}
}
