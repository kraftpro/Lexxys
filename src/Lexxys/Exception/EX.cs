// Lexxys Infrastructural library.
// file: EX.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Lexxys
{
	using Data;

	public static class EX
	{
		private static ILogger? Log => __loger ??= Statics.TryGetLogger("Lexxys.EX");
		private static ILogger? __loger;
#if DEBUG
		private static int _debugLogging;
#endif

		#region Extensions

		public static T DebugLog<T>(this T exception)
			where T: Exception
		{
#if DEBUG
			if (_debugLogging >= 0)
				Log?.Debug(null, exception);
#endif
			return exception;
		}

		[Conditional("DEBUG")]
		public static void EnableDebugLogging()
		{
#if DEBUG
			Interlocked.Increment(ref _debugLogging);
#endif
		}

		[Conditional("DEBUG")]
		public static void DisableDebugLogging()
		{
#if DEBUG
			Interlocked.Decrement(ref _debugLogging);
#endif
		}

		public static T LogError<T>(this T exception)
			where T: Exception
		{
			Log?.Error(null, exception);
			return exception;
		}

		private static object? Serializable(object? value)
		{
			return value is null || value.GetType().IsSerializable ? value: value.ToString();
		}
		#endregion


		public const string DicArgName = "Parameter name";
		public const string DicArgActual = "Actual value";
		public const string DicArgExpected = "Expected value";
		public const string DicArgActualType = "Actual type";
		public const string DicArgExpectedType = "Expected type";
		public const string DicOperation = "Operation";
		public const string DicResource = "Resource";

		public static UnauthorizedAccessException UnauthorizedAccess()
		{
			return new UnauthorizedAccessException(SR.UnauthorizedAccess()).DebugLog();
		}
		public static UnauthorizedAccessException UnauthorizedAccess(string? resourceName)
		{
			var e = new UnauthorizedAccessException(SR.UnauthorizedAccess(resourceName));
			e.Data[DicResource] = resourceName;
			return e.DebugLog();
		}
		public static UnauthorizedAccessException UnauthorizedAccess(string? message, string? resourceName)
		{
			var e = new UnauthorizedAccessException(message);
			e.Data[DicResource] = resourceName;
			return e.DebugLog();
		}
		public static UnauthorizedAccessException UnauthorizedAccess(string? resourceName, Exception? exception)
		{
			var e = new UnauthorizedAccessException(SR.UnauthorizedAccess(resourceName), exception);
			e.Data[DicResource] = resourceName;
			return e.DebugLog();
		}
		public static UnauthorizedAccessException UnauthorizedAccess(string? message, string? resourceName, Exception? exception)
		{
			var e = new UnauthorizedAccessException(message, exception);
			e.Data[DicResource] = resourceName;
			return e.DebugLog();
		}

		public static OverflowException Overflow()
		{
			return new OverflowException(SR.OverflowException()).DebugLog();
		}
		public static OverflowException Overflow(string value)
		{
			return new OverflowException(SR.OverflowException(value)).DebugLog();
		}

		public static ArgumentException Argument()
		{
			return new ArgumentException(SR.ArgumentException()).DebugLog();
		}
		public static ArgumentException Argument(string? message)
		{
			return new ArgumentException(message).DebugLog();
		}
		public static ArgumentException Argument(string? message, Exception? exception)
		{
			return new ArgumentException(message, exception).DebugLog();
		}
		public static ArgumentException Argument(string? message, string? paramName)
		{
			var e = new ArgumentException(message, paramName);
			e.Data[DicArgName] = paramName;
			return e.DebugLog();
		}
		public static ArgumentException Argument(string? message, string? paramName, object? actualValue)
		{
			var e = new ArgumentException(message, paramName);
			e.Data[DicArgName] = paramName;
			e.Data[DicArgActual] = Serializable(actualValue);
			return e.DebugLog();
		}
		public static ArgumentException Argument(string? message, string? paramName, object? actualValue, Exception? exception)
		{
			var e = new ArgumentException(message, paramName, exception);
			e.Data[DicArgName] = paramName;
			e.Data[DicArgActual] = Serializable(actualValue);
			return e.DebugLog();
		}

		public static ArgumentNullException ArgumentNull(string paramName)
		{
			var e = new ArgumentNullException(paramName, SR.ArgumentNullException(paramName));
			e.Data[DicArgName] = paramName;
			return e.DebugLog();
		}

		public static ArgumentOutOfRangeException ArgumentOutOfRange()
		{
			var e = new ArgumentOutOfRangeException();
			return e.DebugLog();
		}
		public static ArgumentOutOfRangeException ArgumentOutOfRange(string paramName)
		{
			var e = new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRangeException(paramName));
			e.Data[DicArgName] = paramName;
			return e.DebugLog();
		}
		public static ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object? actualValue)
		{
			var e = new ArgumentOutOfRangeException(paramName, actualValue, SR.ArgumentOutOfRangeException(paramName));
			e.Data[DicArgName] = paramName;
			e.Data[DicArgActual] = Serializable(actualValue);
			return e.DebugLog();
		}
		public static ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object? actualValue, Exception? exception)
		{
			var e = new ArgumentOutOfRangeException(SR.ArgumentOutOfRangeException(paramName), exception);
			e.Data[DicArgName] = paramName;
			e.Data[DicArgActual] = Serializable(actualValue);
			return e.DebugLog();
		}
		public static ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object? actualValue, object? expectedValue)
		{
			var e = new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRangeException(paramName));
			e.Data[DicArgName] = paramName;
			e.Data[DicArgActual] = Serializable(actualValue);
			e.Data[DicArgExpected] = Serializable(expectedValue);
			return e.DebugLog();
		}
		public static ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object? actualValue, object? minValue, object? maxValue)
		{
			var e = new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRangeException(paramName));
			e.Data[DicArgName] = paramName;
			e.Data[DicArgActual] = Serializable(actualValue);
			e.Data[DicArgExpected] = Tuple.Create(Serializable(minValue), Serializable(maxValue));
			return e.DebugLog();
		}

		public static ArgumentTypeException ArgumentWrongType(string paramName, Type actualType)
		{
			return new ArgumentTypeException(paramName, actualType).DebugLog();
		}
		public static ArgumentTypeException ArgumentWrongType(string paramName, Type actualType, Type expectedType)
		{
			return new ArgumentTypeException(paramName, actualType, expectedType).DebugLog();
		}


		public static ConfigurationException Configuration()
		{
			return new ConfigurationException().DebugLog();
		}
		public static ConfigurationException Configuration(Xml.XmlLiteNode? config)
		{
			return new ConfigurationException(config).DebugLog();
		}
		public static ConfigurationException Configuration(string? message)
		{
			return new ConfigurationException(message).DebugLog();
		}
		public static ConfigurationException Configuration(string? message, Xml.XmlLiteNode? config)
		{
			return new ConfigurationException(message, config).DebugLog();
		}
		public static ConfigurationException Configuration(string? message, Exception? exception)
		{
			return new ConfigurationException(message, exception).DebugLog();
		}
		public static ConfigurationException Configuration(string? message, Xml.XmlLiteNode? config, Exception? exception)
		{
			return new ConfigurationException(message, config, exception).DebugLog();
		}


		public static ValidationException Validation()
		{
			return new ValidationException().DebugLog();
		}
		public static ValidationException Validation(string? message)
		{
			return new ValidationException(message).DebugLog();
		}
		public static ValidationException Validation(string? message, Exception? exception)
		{
			return new ValidationException(message, exception).DebugLog();
		}


		public static InvalidOperationException InvalidOperation()
		{
			return new InvalidOperationException().DebugLog();
		}
		public static InvalidOperationException InvalidOperation(string? message)
		{
			return new InvalidOperationException(message).DebugLog();
		}
		public static InvalidOperationException InvalidOperation(string? message, Exception? exception)
		{
			return new InvalidOperationException(message, exception).DebugLog();
		}

		public static FormatException WrongFormat()
		{
			return new FormatException(SR.FormatException()).DebugLog();
		}
		public static FormatException WrongFormat(string? message)
		{
			return new FormatException(message).DebugLog();
		}
		public static FormatException WrongFormat(string? message, Exception? exception)
		{
			return new FormatException(message, exception).DebugLog();
		}

		public static NotImplementedException NotImplemented()
		{
			return new NotImplementedException(SR.OperationNotImplemented()).DebugLog();
		}
		public static NotImplementedException NotImplemented(string operation)
		{
			var e = new NotImplementedException(SR.OperationNotImplemented(operation));
			e.Data[DicOperation] = operation;
			return e.DebugLog();
		}
		public static NotSupportedException NotSupported()
		{
			var e = new NotSupportedException(SR.OperationNotSupported());
			return e.DebugLog();
		}
		public static NotSupportedException NotSupported(string operation)
		{
			var e = new NotSupportedException(SR.OperationNotSupported(operation));
			e.Data[DicOperation] = operation;
			return e.DebugLog();
		}

		public static ReadOnlyException CollectionIsReadOnly()
		{
			return new ReadOnlyException(SR.ReadOnlyException()).DebugLog();
		}

		public static ReadOnlyException CollectionIsReadOnly(object objectInfo)
		{
			return new ReadOnlyException(SR.ReadOnlyException(objectInfo)).Add("objectInfo", objectInfo).DebugLog();
		}

		public static ReadOnlyException CollectionIsReadOnly(object objectInfo, object item)
		{
			return new ReadOnlyException(SR.ReadOnlyException(objectInfo, item))
				.Add("objectInfo", objectInfo)
				.Add("item", item).DebugLog();
		}
	}

}
