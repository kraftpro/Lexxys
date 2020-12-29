// Lexxys Infrastructural library.
// file: IValue.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys
{
	public interface IValue<T>: IValue
	{
		new T Value { get; set; }
	}

	public interface IValue
	{
		object Value { get; }
	}

#if NETSTANDARD
	public interface IOptions<out T>: Microsoft.Extensions.Options.IOptions<T> where T: class, new()
	{
	}
#else
	public interface IOptions<out T> where T: class, new()
	{
		T Value { get; }
	}
#endif
}


