// Lexxys Infrastructural library.
// file: ValidationException.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Runtime.Serialization;

namespace Lexxys;

[Serializable]
public class ValidationException: InvalidOperationException
{
	public ValidationException()
	{
	}

	public ValidationException(string? message)
		: base(message)
	{
	}

	public ValidationException(string? message, Exception? exception)
		: base(message, exception)
	{
	}

	protected ValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}


