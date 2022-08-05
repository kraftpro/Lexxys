// Lexxys Infrastructural library.
// file: IEnum.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

namespace Lexxys
{
	public interface IEnum
	{
		int Value { get; }
		string Name { get; }
	}

	public interface IOrderedEnum: IEnum
	{
		int Order { get; }
	}

	public interface IEnum<T>
	{
		T Value { get; }
		string Name { get; }
	}

	public interface IOrderedEnum<T>: IEnum<T>
	{
		int Order { get; }
	}
}

