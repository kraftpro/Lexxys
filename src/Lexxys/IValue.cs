// Lexxys Infrastructural library.
// file: IValue.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//


namespace Lexxys;

public interface IValue<out T>: IValue
{
	new T Value { get; }
}

public interface IValue
{
	object? Value { get; }
}

public interface IValueRef<T>: IValue<T>, IValueRef
{
	new T Value { get; set; }
}

public interface IValueRef: IValue
{
	new object? Value { get; set; }
}


