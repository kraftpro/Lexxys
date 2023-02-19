// Lexxys Infrastructural library.
// file: ValidationResults.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using System;

#pragma warning disable CA2225 // Operator overloads have named alternates

namespace Lexxys
{
	public class ValidationResultsItem
	{
		public string Field { get; }
		public string? Message { get; }
		public ErrorInfo ErrorInfo { get; }

		internal ValidationResultsItem(string field)
		{
			Field = field ?? throw new ArgumentNullException(nameof(field));
			ErrorInfo = ErrorInfo.Empty;
		}
		internal ValidationResultsItem(string field, string? message)
		{
			Field = field ?? throw new ArgumentNullException(nameof(field));
			Message = message;
			ErrorInfo = ErrorInfo.Empty;
		}
		internal ValidationResultsItem(string field, string? message, ErrorInfo? errorInfo)
		{
			Field = field ?? throw new ArgumentNullException(nameof(field));
			Message = message;
			ErrorInfo = errorInfo ?? ErrorInfo.Empty;
		}

		public ValidationResultsItem WithField(string field) => new ValidationResultsItem(field, Message, ErrorInfo);
		public ValidationResultsItem WithMassage(string message) => new ValidationResultsItem(Field, message, ErrorInfo);
		public ValidationResultsItem WithError(ErrorInfo errorInfo) => new ValidationResultsItem(Field, Message, errorInfo);

		public override string ToString()
		{
			return Message != null ? Field + ValidationResults.FieldSeparator + Message : Field;
		}
	}
}
