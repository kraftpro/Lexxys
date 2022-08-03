// Lexxys Infrastructural library.
// file: ValidationResults.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lexxys;
using System.Collections;

namespace Lexxys
{
	public class ValidationResults: IEnumerable<ValidationResults.ValidationResultsItem>
	{
		public const char ErrorSeparator = ';';
		public const char FieldSeparator = ':';
		public static readonly ValidationResults Empty = new ValidationResults(Array.Empty<ValidationResultsItem>());

		private readonly IList<ValidationResultsItem> _items;

		private ValidationResults(IList<ValidationResultsItem> items)
		{
			_items = items;
		}

		public static ValidationResults Create(string field)
		{
			field = CleanName(field);
			if (field == null)
				return Empty;

			if (field.Contains(ErrorSeparator))
				throw new ArgumentOutOfRangeException(nameof(field), field, null);

			return new ValidationResults(ReadOnly.Wrap(new[] { new ValidationResultsItem(field) }));
		}

		public static ValidationResults Create(string field, string message)
		{
			field = CleanName(field);
			message = CleanMessage(message);
			if (field == null && message == null)
				return Empty;

			if (field != null && field.Contains(ErrorSeparator))
				throw new ArgumentOutOfRangeException(nameof(field), field, null);

			return new ValidationResults(ReadOnly.Wrap(new[] { new ValidationResultsItem(field, message) }));
		}

		public static ValidationResults Create(string field, ErrorInfo errorInfo)
		{
			field = CleanName(field);
			if (field == null)
				return Empty;

			if (field.Contains(ErrorSeparator))
				throw new ArgumentOutOfRangeException(nameof(field), field, null);

			return new ValidationResults(ReadOnly.Wrap(new[] { new ValidationResultsItem(field, null, errorInfo) }));
		}

		public static ValidationResults Create(string field, ErrorInfo errorInfo, string message)
		{
			field = CleanName(field);
			message = CleanMessage(message);
			if (field == null && message == null)
				return Empty;

			if (field != null && field.Contains(ErrorSeparator))
				throw new ArgumentOutOfRangeException(nameof(field), field, null);

			return new ValidationResults(ReadOnly.Wrap(new[] { new ValidationResultsItem(field, message, errorInfo) }));
		}

		public static ValidationResults Create(ValidationResultsItem value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return new ValidationResults(ReadOnly.Wrap(new[] { value }));
		}

		public static ValidationResults Create(params ValidationResults[] value)
		{
			if (value == null)
				return Empty;

			List<ValidationResultsItem> items = null;
			for (int i = 0; i < value.Length; ++i)
			{
				var item = value[i];
				if (item != null && !item.Success)
				{
					if (items == null)
						items = new List<ValidationResultsItem>(item._items);
					else
						items.AddRange(item._items);
				}
			}

			return items == null ? Empty:
				new ValidationResults(ReadOnly.Wrap(items));
		}

		public static ValidationResults Create(IEnumerable<ValidationResults> value)
		{
			if (value == null)
				return Empty;

			List<ValidationResultsItem> items = null;
			foreach (var item in value)
			{
				if (item != null && !item.Success)
				{
					if (items == null)
						items = new List<ValidationResultsItem>(item._items);
					else
						items.AddRange(item._items);
				}
			}
			return items == null ? Empty: new ValidationResults(ReadOnly.Wrap(items));
		}

		public static ValidationResults Assert(bool success, ValidationResults value)
		{
			return success ? Empty: value;
		}

		public static ValidationResults Assert(bool success, string field)
		{
			return success ? Empty: Create(field);
		}

		public static ValidationResults Assert(bool success, string field, string message)
		{
			return success ? Empty: Create(field, message);
		}

		public static ValidationResults Assert(bool success, string field, ErrorInfo errorInfo)
		{
			return success ? Empty: Create(field, errorInfo);
		}

		public static ValidationResults Assert(bool success, string field, ErrorInfo errorInfo, string message)
		{
			return success ? Empty: Create(field, errorInfo, message);
		}

		public static ValidationResults AssertNotNull<T>(T? value, string field) where T: struct
		{
			return value is null ? Create(field, ErrorInfo.NullValue()): Empty;
		}

		public static ValidationResults AssertNotNull<T>(T value, string field) where T: class
		{
			return value is null ? Create(field, ErrorInfo.NullValue()): Empty;
		}

		public static ValidationResults AssertNull<T>(T? value, string field) where T : struct
		{
			return value is null ? Empty: Create(field, ErrorInfo.OutOfRange(value));
		}

		public static ValidationResults AssertNull<T>(T value, string field) where T : class
		{
			return value is null ? Empty: Create(field, ErrorInfo.OutOfRange(value));
		}


		public static ValidationResults Parse(string value)
		{
			List<ValidationResultsItem> items = ParseItems(value);
			return items == null ? Empty: new ValidationResults(ReadOnly.Wrap(items));
		}

		public int Length => _items.Count;

		public ICollection<ValidationResultsItem> Items => _items;

		public bool Success => _items.Count == 0;

		public ValidationResults Add(ValidationResults value)
		{
			if (value == null || value.Success)
				return this;
			if (Success)
				return value;

			var items = new ValidationResultsItem[_items.Count + value._items.Count];
			_items.CopyTo(items, 0);
			value._items.CopyTo(items, _items.Count);

			return new ValidationResults(ReadOnly.Wrap(items));
		}

		public ValidationResults Add(ValidationResultsItem value)
		{
			if (value == null)
				return this;
			if (Success)
				return Create(value);

			var items = new ValidationResultsItem[_items.Count + 1];
			_items.CopyTo(items, 0);
			items[items.Length - 1] = value;
			return new ValidationResults(ReadOnly.Wrap(items));
		}

		public ValidationResults Add(string value)
		{
			if (Success)
				return Parse(value);

			List<ValidationResultsItem> items = ParseItems(value);
			if (items == null)
				return this;

			var tmp = new ValidationResultsItem[_items.Count + items.Count];
			_items.CopyTo(tmp, 0);
			items.CopyTo(tmp, _items.Count);
			return new ValidationResults(ReadOnly.Wrap(tmp));
		}

		public ValidationResults AndAlso(ValidationResults value)
		{
			return Success ? this: Add(value);
		}

		public ValidationResults AndAlso(string value)
		{
			return Success ? this: Add(ValidationResults.Create(value));
		}

		public ValidationResults WithPrefix(string value)
		{
			if (Success)
				return this;
			value = CleanName(value);
			if (value == null)
				return this;

			var items = new ValidationResultsItem[_items.Count];
			_items.CopyTo(items, 0);

			for (int i = 0; i < items.Length; ++i)
			{
				if (items[i].Field != null)
					items[i] = items[i].WithField(value + items[i].Field);
			}
			return new ValidationResults(ReadOnly.Wrap(items));
		}

		public ValidationResults WithMessage(string message, bool replace = false)
		{
			message = CleanMessage(message);
			var items = new ValidationResultsItem[_items.Count];

			for (int i = 0; i < _items.Count; ++i)
			{
				if (replace || _items[i].Message == null)
					items[i] = _items[i].WithMassage(message);
			}
			return new ValidationResults(ReadOnly.Wrap(items));
		}

		public ValidationResults WithMessage(string field, string message, bool replace = false)
		{
			string[] fields = SplitName(field, out int n);
			if (fields == null || n == 0)
				return this;
			int[] ii = new int[n];
			bool found = false;
			for (int i = 0; i < ii.Length; i++)
			{
				var f = fields[i];
				ii[i] = _items.FindIndex(o => o.Field == f);
				found |= ii[i] >= 0;
			}
			if (!found)
				return this;

			var items = new ValidationResultsItem[_items.Count];
			_items.CopyTo(items, 0);
			for (int i = 0; i < ii.Length; i++)
			{
				int j = ii[i];
				if (j >= 0)
					if (replace || items[j].Message == null)
						items[j] = items[i].WithMassage(message);
			}
			return new ValidationResults(ReadOnly.Wrap(items));
		}

		public ValidationResults Remove(ValidationResults value)
		{
			if (Success || value == null || value.Success)
				return this;

			var items = new List<ValidationResultsItem>(_items.Count);
			IList<ValidationResultsItem> subs = value._items;
			foreach (var item in _items)
			{
				string field = item.Field;
				if (subs.All(o => o.Field != field))
					items.Add(item);
			}
			return items.Count == _items.Count ? this:
				items.Count == 0 ? Empty: new ValidationResults(ReadOnly.Wrap(items));
		}

		public ValidationResults Remove(ValidationResultsItem value)
		{
			if (Success || value == null)
				return this;

			if (!_items.Contains(value))
				return this;
			if (_items.Count == 1)
				return Empty;
			var items = new List<ValidationResultsItem>(_items.Count - 1);
			foreach (var item in _items)
			{
				if (item != value)
					items.Add(item);
			}
			return new ValidationResults(items);
		}

		public ValidationResults Remove(Func<ValidationResultsItem, bool> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			if (Success)
				return this;

			var items = new List<ValidationResultsItem>(_items.Where(o => !predicate(o)));
			return items.Count == _items.Count ? this:
				items.Count == 0 ? Empty: new ValidationResults(ReadOnly.Wrap(items));
		}

		public ValidationResults Remove(string value)
		{
			if (Success)
				return this;
			value = CleanName(value);
			if (value == null)
				return this;

			if (value.Contains(ErrorSeparator))
				throw new ArgumentOutOfRangeException(nameof(value), value, null);

			var items = new List<ValidationResultsItem>(_items.Count - 1);
			foreach (var item in _items)
			{
				if (item.Field != value)
					items.Add(item);
			}
			return items.Count == _items.Count ? this:
				items.Count == 0 ? Empty: new ValidationResults(ReadOnly.Wrap(items));
		}

		public ValidationResults Replace(string source, string target)
		{
			source = CleanName(source);
			target = CleanName(target);
			if (source == null)
				return this;

			if (target == null)
				return Remove(source);

			if (source.Contains(ErrorSeparator))
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
			if (target.Contains(ErrorSeparator))
				throw new ArgumentOutOfRangeException(nameof(target), target, null);

			var items = new ValidationResultsItem[_items.Count];
			bool found = false;
			for (int i = 0; i < _items.Count; ++i)
			{
				if (_items[i].Field == source)
				{
					items[i] = _items[i].WithField(target);
					found = true;
				}
				else
				{
					items[i] = _items[i];
				}
			}
			return found ? new ValidationResults(ReadOnly.Wrap(items)): this;
		}

		public bool HasError()
		{
			return !Success;
		}

		public bool HasError(string field)
		{
			field = CleanName(field);
			if (field == null)
				return false;
			if (field.IndexOf(ErrorSeparator) < 0)
				return _items.FindIndex(o => o.Field == field) >= 0;

			List<ValidationResultsItem> fields = ParseItems(field);
			return fields.All(o => _items.FindIndex(p => p.Field == o.Field) >= 0);
		}

		public bool HasMessage()
		{
			return _items.Any(o => o.Message != null);
		}

		public bool Contains(string value)
		{
			value = CleanName(value);
			return value != null && _items.Any(o => o.Field == value);
		}

		public bool ContainsAny(IEnumerable<string> fields)
		{
			return fields != null && fields.Any(Contains);
		}

		public bool ContainsAll(IEnumerable<string> fields)
		{
			return fields == null || fields.All(Contains);
		}

		public bool ContainsAny(IEnumerable<ValidationResultsItem> value)
		{
			if (value == null)
				return false;
			foreach (var item in value)
			{
				string field = item.Field;
				if (_items.Any(o => o.Field == field))
					return true;
			}
			return false;
		}

		public bool ContainsAll(IEnumerable<ValidationResultsItem> value)
		{
			if (value == null)
				return false;
			foreach (var item in value)
			{
				string field = item.Field;
				if (!_items.Any(o => o.Field == field))
					return false;
			}
			return true;
		}

		public void Invariant(string source = null, Func<string> dump = null)
		{
			if (Success)
				return;
			Exception flaw = InvariantException(source);
			if (dump != null)
				flaw.Add("Dump", dump());
			throw flaw;
		}

		public void Invariant(string source, params object[] data)
		{
			if (Success)
				return;
			Exception flaw = InvariantException(source);
			if (data != null && data.Length > 0)
			{
				for (int i = 1; i < data.Length; i += 2)
				{
					flaw.Add((data[i - 1] ?? "<null>").ToString(), data[i]);
				}
				if ((data.Length & 1) != 0)
					flaw.Add("<last>", data[data.Length - 1]);
			}
			throw flaw;
		}

		public void Invariant(string source, params ErrorAttrib[] data)
		{
			if (Success)
				return;
			Exception flaw = InvariantException(source);
			if (data != null)
			{
				foreach (var item in data)
				{
					flaw.Add(item.Name, item.Value);
				}
			}
			throw flaw;
		}

		public Exception InvariantException(string source = null)
		{
			return new InvalidOperationException(SR.CheckInvariantFailed(this, source));
		}

		public IEnumerator<ValidationResultsItem> GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		public override string ToString()
		{
			if (Success)
				return "";

			var text = new StringBuilder();
			var tmp = new ValidationResultsItem[_items.Count];
			_items.CopyTo(tmp, 0);
			Array.Sort(tmp, (p, q) => { int k = String.Compare(p.Field, q.Field, StringComparison.OrdinalIgnoreCase); return k == 0 ? String.Compare(p.Message, q.Message, StringComparison.OrdinalIgnoreCase): k; });
			string fld = null;
			string msg = null;
			foreach (var item in tmp)
			{
				if (item.Field != fld)
				{
					fld = item.Field;
					msg = null;
					text.Append(ErrorSeparator).Append(item.Field);
				}
				if (item.Message != msg)
				{
					msg = item.Message;
					if (item.Message != null)
						text.Append(ErrorSeparator).Append(FieldSeparator).Append(item.Message).Append(':');
				}
			}
			return text.Append(ErrorSeparator).ToString();
		}

		public static explicit operator ValidationResults(string value)
		{
			return Create(value);
		}

		public static ValidationResults operator +(ValidationResults left, ValidationResults right)
		{
			return left != null ? left.Add(right): right ?? Empty;
		}

		public static ValidationResults operator +(ValidationResults left, ValidationResultsItem right)
		{
			return left != null ? left.Add(right):
				right != null ? Create(right): Empty;
		}

		public static ValidationResults operator +(ValidationResults left, string right)
		{
			return left != null ? left.Add(right):
				right != null ? Parse(right): Empty;
		}

		public static ValidationResults operator -(ValidationResults left, string right)
		{
			return left == null ? Empty: left.Remove(right);
		}

		public static ValidationResults operator -(ValidationResults left, ValidationResults right)
		{
			return left == null ? Empty: left.Remove(right);
		}

		public static ValidationResults operator &(ValidationResults left, ValidationResults right)
		{
			return left != null ? left.Add(right) : right ?? Empty;
		}

		public static bool operator true(ValidationResults value)
		{
			return value == null || value.Success;
		}

		public static bool operator false(ValidationResults value)
		{
			return value != null && !value.Success;
		}

		private static string CleanName(string value)
		{
			return value == null || (value = value.Trim(__nameWhiteSpace)).Length == 0 ? null: value.ToUpperInvariant();
		}
		private static readonly char[] __nameWhiteSpace = { ErrorSeparator, ' ', '\t', '\n', '\r', '\f' };

		private static string CleanMessage(string value)
		{
			return value == null || (value = value.Trim(__messageWhiteSpace)).Length == 0 ? null: value;
		}
		private static readonly char[] __messageWhiteSpace = { FieldSeparator, ErrorSeparator, ' ', '\t', '\n', '\r', '\f' };

		private static string[] SplitName(string value, out int length)
		{
			value = CleanName(value);
			if (value == null)
			{
				length = 0;
				return null;
			}
			if (value.IndexOf(ErrorSeparator) < 0)
			{
				length = 1;
				return new[] { value };
			}

			string[] result = value.Split(__itemSeparators, StringSplitOptions.RemoveEmptyEntries);
			int k = 0;
			for (int i = 0; i < result.Length; ++i)
			{
				string s = CleanName(result[i]);
				if (s != null)
					result[k++] = s;
			}
			length = k;
			return result;
		}
		private static readonly char[] __itemSeparators = { ErrorSeparator };

		private static List<ValidationResultsItem> ParseItems(string value)
		{
			var result = new List<ValidationResultsItem>();
			int i = 0;
			string fld = null;
			while (value != null && i < value.Length)
			{
				while (value[i] == ErrorSeparator || Char.IsWhiteSpace(value, i))
				{
					if (++i >= value.Length)
						goto Break2;
				}

				bool message = value[i] == FieldSeparator;
				int k;
				if (!message)
				{
					k = value.IndexOf(ErrorSeparator, i);
				}
				else
				{
					while (value[i] == FieldSeparator || value[i] == ErrorSeparator || Char.IsWhiteSpace(value, i))
					{
						if (++i >= value.Length)
							goto Break2;
					}
					k = value.IndexOf(":;", i, StringComparison.Ordinal);
				}
				if (k < 0)
				{
					if (message)
					{
						string s = value.Substring(i).TrimEnd(__messageWhiteSpace);
						if (fld != null || s.Length > 0)
							result.Add(new ValidationResultsItem(fld, s));
					}
					else
					{
						if (fld != null)
							result.Add(new ValidationResultsItem(fld));
						fld = value.Substring(i).TrimEnd().ToUpperInvariant();
						if (fld.Length > 0)
							result.Add(new ValidationResultsItem(fld));
					}
					return result.Count == 0 ? null: result;
				}
				if (message)
				{
					string s = value.Substring(i, k - i).TrimEnd(__messageWhiteSpace);
					if (s.Length > 0)
					{
						result.Add(new ValidationResultsItem(fld, s));
						fld = null;
					}
					i = k + 2;
				}
				else
				{
					if (fld != null)
						result.Add(new ValidationResultsItem(fld));
					fld = value.Substring(i, k - i).TrimEnd().ToUpperInvariant();
					if (fld.Length == 0)
						fld = null;
					i = k + 1;
				}
			}

		Break2:
			if (fld != null)
				result.Add(new ValidationResultsItem(fld));
			return result.Count == 0 ? null: result;
		}

		public class ValidationResultsItem
		{
			public readonly string Field;
			public readonly string Message;
			public readonly ErrorInfo ErrorInfo;

			internal ValidationResultsItem(string field)
			{
				Field = field;
				ErrorInfo = ErrorInfo.Empty;
			}
			internal ValidationResultsItem(string field, string message)
			{
				Field = field;
				Message = message;
				ErrorInfo = ErrorInfo.Empty;
			}
			internal ValidationResultsItem(string field, string message, ErrorInfo errorInfo)
			{
				Field = field;
				Message = message;
				ErrorInfo = errorInfo ?? ErrorInfo.Empty;
			}

			public ValidationResultsItem WithField(string field) => new ValidationResultsItem(field, Message, ErrorInfo);
			public ValidationResultsItem WithMassage(string message) => new ValidationResultsItem(Field, message, ErrorInfo);
			public ValidationResultsItem WithError(ErrorInfo errorInfo) => new ValidationResultsItem(Field, Message, errorInfo);

			public override string ToString()
			{
				return Message != null ? Field + FieldSeparator + Message : Field;
			}
		}

		public Exception ValidationException()
		{
			return EX.Validation(SR.ValidationFailed(this));
		}
	}
}
