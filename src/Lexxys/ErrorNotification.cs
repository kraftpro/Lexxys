// Lexxys Infrastructural library.
// file: ErrorNotification.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Lexxys
{
	#pragma warning disable CA1710 // Identifiers should have correct suffix

	public sealed class ErrorNotification: ICollection<FieldError>, ICollection
	{
		public static readonly ErrorNotification NoErrors = new ErrorNotification(new List<FieldError>(0), true);
		public static ErrorNotification Empty { get; } = NoErrors;

		private readonly List<FieldError> _errors;

		public ErrorNotification()
		{
			_errors = new List<FieldError>();
		}

		private ErrorNotification(List<FieldError> errors, bool readOnly)
		{
			_errors = errors;
			IsReadOnly = readOnly;
		}

		public bool IsReadOnly { get; }

		public bool IsEmpty => _errors.Count == 0;

		public int Count => _errors.Count;

		public ErrorNotification AsReadOnly()
			=> IsReadOnly ? this : new ErrorNotification(_errors, true);

		public void Add(FieldError? item)
		{
			if (IsReadOnly)
				throw EX.NotSupported("Add");
			if (item is not null && !_errors.Contains(item))
				_errors.Add(item);
		}

		public void Add(string fieldName, string messageCode, params string[] arguments)
			=> Add(new FieldError(fieldName, messageCode, arguments));

		public void Add(string fieldName, string messageCode)
			=> Add(new FieldError(fieldName, messageCode));

		public void Add(string fieldName) => Add(new FieldError(fieldName));

		public void Clear()
		{
			if (IsReadOnly)
				throw EX.NotSupported("Clear");
			_errors.Clear();
		}

		public bool Contains(FieldError item) => _errors.Contains(item);

		public void CopyTo(FieldError[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			_errors.CopyTo(array, arrayIndex);
		}

		public bool Remove(FieldError item) => _errors.Remove(item);

		#region ICollection Members

		bool ICollection.IsSynchronized => ((ICollection)_errors).IsSynchronized;

		object ICollection.SyncRoot => ((ICollection)_errors).SyncRoot;

		void ICollection.CopyTo(Array array, int index) => ((ICollection)_errors).CopyTo(array, index);

		#endregion

		#region IEnumerable Members

		public IEnumerator<FieldError> GetEnumerator() => _errors.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _errors.GetEnumerator();

		#endregion
	}

	public class FieldError
	{
		private static readonly string[] NoArguments = Array.Empty<string>();

		private readonly string[]? _arguments;

		public FieldError()
		{
		}

		public FieldError(string fieldName)
		{
			FieldName = fieldName;
		}

		public FieldError(string fieldName, string messageCode, params string[]? arguments)
		{
			FieldName = fieldName;
			MessageCode = messageCode;
			_arguments = arguments;
		}

		public string? FieldName { get; }
		public string? MessageCode { get; }

		public string[] GetArguments()
		{
			if (_arguments == null || _arguments.Length == 0)
				return NoArguments;
			string[] temp = new string[_arguments.Length];
			_arguments.CopyTo(temp, 0);
			return temp;
		}

		public static bool operator ==(FieldError left, FieldError right) => Equals(left, right);

		public static bool operator !=(FieldError left, FieldError right) => !Equals(left, right);

		public static bool Equals(FieldError? left, FieldError? right)
			=> ReferenceEquals(left, right) || (
			left is not null && right is not null &&
			left.FieldName == right.FieldName &&
			left.MessageCode == right.MessageCode &&
			Tools.Equals(left._arguments, right._arguments));

		/// <inheritdoc />
		public override bool Equals(object? obj)
			=> obj is FieldError error && Equals(this, error);

		/// <inheritdoc />
		public override int GetHashCode()
			=> HashCode.Join(FieldName?.GetHashCode() ?? 0, MessageCode?.GetHashCode() ?? 0, _arguments?.Length.GetHashCode() ?? 0);

		/// <inheritdoc />
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(FieldName).Append(':').Append(MessageCode);
			if (_arguments != null)
			{
				for (int i = 0; i < _arguments.Length; ++i)
				{
					sb.Append(',').Append(_arguments[i]);
				}
			}
			return sb.ToString();
		}
	}
}


