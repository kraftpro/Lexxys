// Lexxys Infrastructural library.
// file: ExceptionExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Lexxys
{
	public static class ExceptionExtensions
	{
		public static T Add<T>(this T exception, string name, object? value) where T: Exception
		{
			if (exception == null)
				return exception!;
			name ??= "item";
			if (value != null && !value.GetType().IsSerializable)
				value = value.ToString();
			if (exception.Data.Contains(name))
			{
				string name0 = name;
				int k = 0;
				do
				{
					var v = exception.Data[name];
					if (Object.Equals(value, v))
						return exception;
					name = $"{name0}.{++k}";
				} while (exception.Data.Contains(name));
			}
			exception.Data[name] = value;
			return exception;
		}

		public static T Add<T>(this T exception, IEnumerable<(string Name, object Value)>items)
			where T: Exception
		{
			if (items != null)
			{
				foreach (var (name, value) in items)
				{
					Add(exception, name, value);
				}
			}
			return exception;
		}

		public static bool IsCriticalException(this Exception exception)
		{
			if (exception == null)
				return false;

			exception = Unwrap(exception);
			return exception is NullReferenceException ||
				exception is StackOverflowException ||
				exception is ThreadAbortException ||
				exception is OutOfMemoryException ||
				exception is System.Runtime.InteropServices.SEHException ||
				exception is System.Security.SecurityException;
		}

		public static Exception Unwrap(this Exception exception)
		{
			if (exception == null)
				return exception!;

			while (exception.InnerException != null && (exception is TypeInitializationException || exception is TargetInvocationException || (exception as AggregateException)?.InnerExceptions.Count == 1))
			{
				exception = exception.InnerException;
			}
			return exception;
		}
	}
}


