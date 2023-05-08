// Lexxys Infrastructural library.
// file: ArgumentTypeException.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Runtime.Serialization;

namespace Lexxys;


[Serializable]
public class ArgumentTypeException: ArgumentException
{
	public ArgumentTypeException()
	{
	}

	public ArgumentTypeException(string paramName)
		: base(SR.ArgumentWrongTypeException(paramName), paramName)
	{
	}

	public ArgumentTypeException(string? message, Exception? exception)
		: base(message, exception)
	{
	}

	public ArgumentTypeException(string paramName, Type actualType)
		: base(SR.ArgumentWrongTypeException(paramName, actualType), paramName)
	{
	}

	public ArgumentTypeException(string paramName, Type actualType, Type expectedType)
		: base(SR.ArgumentWrongTypeException(paramName, actualType, expectedType), paramName)
	{
	}

	public ArgumentTypeException(string? message, string paramName, Exception? innerException)
		: base(message, paramName, innerException)
	{
	}

	public ArgumentTypeException(string? message, string paramName)
		: base(message, paramName)
	{
	}

	protected ArgumentTypeException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
	 
}


