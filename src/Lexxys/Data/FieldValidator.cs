// Lexxys Infrastructural library.
// file: FieldValidator.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys.Data
{
	public static class FieldValidator
	{
		private static bool _ignoreReferenceKey;

		static FieldValidator()
		{
			Config.Current.Changed += Config_Changed;
			Config_Changed();
		}

		private static void Config_Changed(object? sender = null, ConfigurationEventArgs? _ = null)
		{
			_ignoreReferenceKey = !Config.Current.GetValue(Check.ConfigReference + ":validate", true).Value;
		}

		public static ValidationResults ReferenceKey(IDataContext dc, int? value, string reference, string field) => ReferenceKey(dc, value, reference, field, true);

		public static ValidationResults ReferenceKey(IDataContext dc, int? value, string reference, string field, bool nullable)
		{
			if (reference == null || reference.Length <= 0)
				throw new ArgumentNullException(nameof(reference));
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value == null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue()):
				_ignoreReferenceKey || Check.ReferenceKey(dc, value, reference) ?
					ValidationResults.Empty:
					ValidationResults.Create(field, ErrorInfo.BadReference(value, reference));
		}

		public static ValidationResults ReferenceKey(IDataContext dc, int value, string reference, string field)
		{
			if (reference == null || reference.Length <= 0)
				throw new ArgumentNullException(nameof(reference));
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return _ignoreReferenceKey || Check.ReferenceKey(dc, value, reference) ?
				ValidationResults.Empty:
				ValidationResults.Create(field, ErrorInfo.BadReference(value, reference));
		}

		public static ValidationResults ReferenceKey(IDataContext dc, int value, string table, string key, string field) => ReferenceKey(dc, value, table, key, field, false);

		public static ValidationResults ReferenceKey(IDataContext dc, int? value, string table, string key, string field) => ReferenceKey(dc, value, table, key, field, true);

		public static ValidationResults ReferenceKey(IDataContext dc, int? value, string table, string key, string field, bool nullable)
		{
			if (table == null || table.Length <= 0)
				throw new ArgumentNullException(nameof(table));
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value == null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue()):
				_ignoreReferenceKey || Check.ReferenceKey(dc, value, table, key) ?
					ValidationResults.Empty:
					ValidationResults.Create(field, ErrorInfo.BadReference(value, table + "." + key));
		}

		public static ValidationResults ReferenceKey(int value, string field)
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return Check.IsId(value) ?
				ValidationResults.Empty:
				ValidationResults.Create(field, ErrorInfo.BadReference(value));
		}

		public static ValidationResults ReferenceKey(int? value, string field)
		{
			return ReferenceKey(value, field, true);
		}

		public static ValidationResults ReferenceKey(int? value, string field, bool nullable)
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value == null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue()):
				Check.IsId(value) ?
					ValidationResults.Empty:
					ValidationResults.Create(field, ErrorInfo.BadReference(value));
		}

		public static ValidationResults Range<T>(T value, ValueTuple<T, T>[]? ranges, string field)
			where T: struct, IComparable<T>, IEquatable<T>
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				Check.Range(value, ranges) ?
					ValidationResults.Empty:
				ranges?.Length == 1 ?
					ValidationResults.Create(field, ErrorInfo.OutOfRange((T?)value, ranges[0].Item1, ranges[0].Item2)):
					ValidationResults.Create(field, ErrorInfo.OutOfRange(value));
		}

		public static ValidationResults Range<T>(T? value, ValueTuple<T, T>[]? ranges, string field)
			where T: struct, IComparable<T>, IEquatable<T>
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return Range(value, ranges, field, true);
		}

		public static ValidationResults Range<T>(T? value, ValueTuple<T, T>[]? ranges, string field, bool nullable)
			where T: struct, IComparable<T>, IEquatable<T>
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value == null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue()):
				Check.Range(value, ranges, nullable) ?
					ValidationResults.Empty:
				ranges?.Length == 1 ?
					ValidationResults.Create(field, ErrorInfo.OutOfRange(value, ranges[0].Item1, ranges[0].Item2)):
					ValidationResults.Create(field, ErrorInfo.OutOfRange(value));
		}

		public static ValidationResults Range<T>(T value, T[]? values, string field)
			where T : struct, IEquatable<T>
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return Check.Range(value, values) ? ValidationResults.Empty: ValidationResults.Create(field, ErrorInfo.OutOfRange(value));
		}

		public static ValidationResults Range<T>(T? value, T[]? values, string field)
			where T : struct, IEquatable<T>
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return Range(value, values, field, true);
		}

		public static ValidationResults Range<T>(T? value, T[]? values, string field, bool nullable)
			where T : struct, IEquatable<T>
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value == null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue()):
				Check.Range(value, values, nullable) ?
					ValidationResults.Empty:
					ValidationResults.Create(field, ErrorInfo.OutOfRange(value));
		}

		public static ValidationResults FieldValue<T>(T? value, T? min, T? max, string field, bool nullable)
			where T: class, IComparable<T>
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value is null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue()):
				min is not null && value.CompareTo(min) < 0 ?
					ValidationResults.Create(field, ErrorInfo.OutOfRange(value, min, max)):
				max is not null && value.CompareTo(max) > 0 ?
					ValidationResults.Create(field, ErrorInfo.OutOfRange(value, min, max)):
					ValidationResults.Empty;
		}

		public static ValidationResults FieldValue<T>(T? value, T? min, T? max, string field)
			where T : class, IComparable<T>
		{
			return FieldValue(value, min, max, field, true);
		}

		public static ValidationResults FieldValue<T>(T? value, T? min, T? max, string field, bool nullable)
			where T: struct, IComparable<T>
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				!value.HasValue ?
					nullable ? ValidationResults.Empty: ValidationResults.Create(field, ErrorInfo.NullValue()):
				min.HasValue && value.GetValueOrDefault().CompareTo(min.GetValueOrDefault()) < 0 ?
					ValidationResults.Create(field, ErrorInfo.OutOfRange(value.GetValueOrDefault(), min, max)):
				max.HasValue && value.GetValueOrDefault().CompareTo(max.GetValueOrDefault()) > 0 ?
					ValidationResults.Create(field, ErrorInfo.OutOfRange(value.GetValueOrDefault(), min, max)):
					ValidationResults.Empty;
		}

		public static ValidationResults FieldValue<T>(T? value, T? min, T? max, string field)
			where T: struct, IComparable<T>
		{
			return FieldValue(value, min, max, field, true);
		}

		public static ValidationResults FieldValue<T>(T? value, T min, T max, string field)
			where T: struct, IComparable<T>
		{
			return FieldValue(value, (T?)min, (T?)max, field, true);
		}

		public static ValidationResults FieldValue<T>(T? value, T min, T max, string field, bool nullable)
			where T: struct, IComparable<T>
		{
			return FieldValue(value, (T?)min, (T?)max, field, nullable);
		}

		public static ValidationResults FieldValue(string? value, int length, string field, bool nullable = false)
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value == null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue()):
				length > 0 && value.Length > length ?
					ValidationResults.Create(field, ErrorInfo.SizeOutOfRange(value, ErrorDataType.String, length)):
					ValidationResults.Empty;
		}

		public static ValidationResults FieldValue(byte[]? value, int length, string field, bool nullable = false)
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value == null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue()):
				length > 0 && value.Length > length ?
					ValidationResults.Create(field, ErrorInfo.SizeOutOfRange(value, ErrorDataType.Binary, length)):
					ValidationResults.Empty;
		}

		public static ValidationResults Format(string? value, ErrorDataType dataType, Func<string, bool> test, int length, string field, bool nullable)
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));
			if (test == null)
				throw new ArgumentNullException(nameof(test));

			return
				value == null ?
					nullable ? ValidationResults.Empty : ValidationResults.Create(field, ErrorInfo.NullValue(dataType)):
				length > 0 && value.Length > length ?
					ValidationResults.Create(field, ErrorInfo.SizeOutOfRange(value, dataType)):
				test(value) ?
					ValidationResults.Empty:
					ValidationResults.Create(field, ErrorInfo.BadFormat(value, dataType));
		}

		public static ValidationResults EmailAddress(string? value, int length, string field, bool nullable = false)
		{
			return Format(value, ErrorDataType.Phone, ValueValidator.IsEmail, length, field, nullable);
		}

		public static ValidationResults PhoneNumber(string? value, string field, bool nullable = false, bool strict = false)
		{
			return PhoneNumber(value, 10, field, nullable, strict);
		}

		public static ValidationResults PhoneNumber(string? value, int length, string field, bool nullable = false, bool strict = false)
		{
			return Format(value, ErrorDataType.Phone, o => ValueValidator.IsPhone(o, strict), length, field, nullable);
		}

		public static ValidationResults HttpAddress(string? value, int length, string field, bool nullable = false)
		{
			return Format(value, ErrorDataType.Phone, ValueValidator.IsHttpUrl, length, field, nullable);
		}

		public static ValidationResults EinCode(int value, string field)
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return ValueValidator.IsEin(value) ?
				ValidationResults.Empty:
				ValidationResults.Create(field, ErrorInfo.BadFormat(value, ErrorDataType.Ein));
		}

		public static ValidationResults EinCode(int? value, string field)
		{
			return EinCode(value, field, true);
		}

		public static ValidationResults EinCode(int? value, string field, bool nullable)
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return
				value == null ?
					nullable ? ValidationResults.Empty: ValidationResults.Create(field, ErrorInfo.NullValue()):
				ValueValidator.IsEin(value.GetValueOrDefault()) ?
					ValidationResults.Empty:
					ValidationResults.Create(field, ErrorInfo.BadFormat(value, ErrorDataType.Ein));
		}

		public static ValidationResults EinCode(string? value, string field, bool nullable = false)
		{
			return EinCode(value, 0, field, nullable);
		}

		public static ValidationResults EinCode(string? value, int length, string field, bool nullable = false)
		{
			return Format(value, ErrorDataType.Phone, ValueValidator.IsEin, length, field, nullable);
		}

		public static ValidationResults SsnCode(string? value, string field, bool nullable = false)
		{
			return SsnCode(value, 0, field, nullable);
		}

		public static ValidationResults SsnCode(string? value, int length, string field, bool nullable = false)
		{
			return Format(value, ErrorDataType.Ssn, ValueValidator.IsSsn, length, field, nullable);
		}

		public static ValidationResults UsZipCode(string? value, string field, bool nullable = false)
		{
			return UsZipCode(value, 0, field, nullable);
		}

		public static ValidationResults UsZipCode(string? value, int length, string field, bool nullable = false)
		{
			return Format(value, ErrorDataType.UsZip, ValueValidator.IsUsZipCode, length, field, nullable);
		}

		public static ValidationResults UsStateCode(string? value, string field, bool nullable = false)
		{
			return Format(value, ErrorDataType.UsState, o => Check.UsStateCode(value, true), 0, field, nullable);
		}

		public static ValidationResults CountryCode(string? value, string field, bool nullable = false)
		{
			return Format(value, ErrorDataType.Country, o => Check.CountryCode(value, true), 0, field, nullable);
		}

		public static ValidationResults PostalCode(string? value, int length, string field, bool nullable = false)
		{
			return Format(value, ErrorDataType.PostalCode, ValueValidator.IsPostalCode, length, field, nullable);
		}

		public static ValidationResults NotNull<T>(T? value, string field)
			where T: class
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return value is null ? ValidationResults.Create(field, ErrorInfo.NullValue()): ValidationResults.Empty;
		}

		public static ValidationResults NotNull<T>(T? value, string field)
			where T: struct
		{
			if (field == null || field.Length <= 0)
				throw new ArgumentNullException(nameof(field));

			return !value.HasValue ? ValidationResults.Create(field, ErrorInfo.NullValue()) : ValidationResults.Empty;
		}

		public static ValidationResults ReferenceKeyDebug(int? value, string reference, string field) => ReferenceKey(value, field);

		public static ValidationResults ReferenceKeyDebug(int value, string reference, string field) => ReferenceKey(value, field);

		public static ValidationResults ReferenceKeyDebug(int? value, string reference, string field, bool nullable) => ReferenceKey(value, field, nullable);

		public static ValidationResults UniqueFieldDebug(string value, int length, string table, string tableField, string? keyField = null, int keyValue = 0, string? field = null, bool nullable = false)
		{
			if (table == null || table.Length <= 0)
				throw new ArgumentNullException(nameof(table));
			if (tableField == null || tableField.Length <= 0)
				throw new ArgumentNullException(nameof(tableField));
			if (field == null || field.Length == 0)
				field = tableField;
			return FieldValue(value, length, field, nullable);
		}

		public static ValidationResults UniqueFieldDebug(int? value, string table, string tableField, string? keyField = null, int keyValue = 0, string? field = null, bool nullable = false)
		{
			if (table == null || table.Length <= 0)
				throw new ArgumentNullException(nameof(table));
			if (tableField == null || tableField.Length <= 0)
				throw new ArgumentNullException(nameof(tableField));

			if (field == null || field.Length == 0)
				field = tableField;
			return value == null && !nullable ? ValidationResults.Create(field, ErrorInfo.NullValue()): ValidationResults.Empty;
		}
	}
}
