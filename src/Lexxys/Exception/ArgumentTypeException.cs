// Lexxys Infrastructural library.
// file: ArgumentTypeException.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable enable

namespace Lexxys
{

	[Serializable]
	public class ArgumentTypeException: ArgumentException
	{
		public ArgumentTypeException()
		{
		}

		public ArgumentTypeException(string paramName)
			: base(SR.ArgumentWrongTypeException(paramName), paramName)
		{
			base.Data[EX.DicArgName] = paramName;
		}

		public ArgumentTypeException(string message, Exception exception)
			: base(message, exception)
		{
		}

		public ArgumentTypeException(string paramName, Type? actualType)
			: base(SR.ArgumentWrongTypeException(paramName, actualType), paramName)
		{
			base.Data[EX.DicArgName] = paramName;
			base.Data[EX.DicArgActualType] = actualType;
		}

		public ArgumentTypeException(string paramName, Type? actualType, Type expectedType)
			: base(SR.ArgumentWrongTypeException(paramName, actualType, expectedType), paramName)
		{
			base.Data[EX.DicArgName] = paramName;
			if (actualType != null)
				base.Data[EX.DicArgActualType] = actualType;
			base.Data[EX.DicArgExpectedType] = expectedType;
		}

		public ArgumentTypeException(string message, string paramName, Exception innerException)
			: base(message, paramName, innerException)
		{
			base.Data[EX.DicArgName] = paramName;
		}

		public ArgumentTypeException(string message, string paramName)
			: base(message, paramName)
		{
			base.Data[EX.DicArgName] = paramName;
		}

		protected ArgumentTypeException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		 
	}
}


