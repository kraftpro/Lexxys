// Lexxys Infrastructural library.
// file: IValue.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

#nullable disable

namespace Lexxys
{
	public interface IValue<out T>: IValue
	{
		new T Value { get; }
	}

	public interface IValue
	{
		object Value { get; }
	}

#if !NETCOREAPP
	public interface IOptions<out T> where T: class
	{
		T Value { get; }
	}
#else
	public interface IOptions<out T>: Microsoft.Extensions.Options.IOptions<T> where T: class
	{
	}
#endif
}


