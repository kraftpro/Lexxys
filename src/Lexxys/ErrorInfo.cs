using Lexxys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lexxys
{
	public class ErrorInfo
	{
		public static readonly ErrorInfo Empty = new ErrorInfo(ErrorCode.Default, ErrorDataType.Object, Array.Empty<ErrorAttrib>());
		public ErrorCode Code { get; }
		public ErrorDataType DataType { get; }
		public IReadOnlyList<ErrorAttrib> Attribs { get; }

		public ErrorInfo(ErrorCode code, ErrorDataType dataType, IEnumerable<ErrorAttrib> attribs)
		{
			Code = code;
			DataType = dataType;
			Attribs = attribs as IReadOnlyList<ErrorAttrib> ?? ReadOnly.WrapCopy(attribs, true);
		}

		public ErrorInfo(ErrorCode code, ErrorDataType dataType, params ErrorAttrib[] attribs)
		{
			Code = code;
			DataType = dataType;
			Attribs = attribs ?? Array.Empty<ErrorAttrib>();
		}

		private static readonly ErrorInfo __nullValue = new ErrorInfo(ErrorCode.NullValue, ErrorDataType.Object, Array.Empty<ErrorAttrib>());

		/// <summary>
		/// The value is null or empty.
		/// </summary>
		public static ErrorInfo NullValue() => __nullValue;

		/// <summary>
		/// The value of the specifies <paramref name="dataType"/> is null or empty.
		/// </summary>
		/// <param name="dataType">Type of empty value</param>
		/// <returns></returns>
		public static ErrorInfo NullValue(ErrorDataType dataType) => new ErrorInfo(ErrorCode.NullValue, dataType, Array.Empty<ErrorAttrib>());

		/// <summary>
		/// Foreign key reference to <paramref name="reference"/> for the specified <paramref name="value"/> not found.
		/// </summary>
		/// <typeparam name="T">Reference value type</typeparam>
		/// <param name="value">Reference value</param>
		/// <param name="reference">Referenced entity</param>
		/// <returns></returns>
		public static ErrorInfo BadReference<T>(T value, string reference) =>
			new ErrorInfo(ErrorCode.BadReference, ErrorDataType.Object, new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(reference), reference) });

		/// <summary>
		/// Foreign key reference for the specified <paramref name="value"/> not found.
		/// </summary>
		/// <typeparam name="T">Reference value type</typeparam>
		/// <param name="value">Reference value</param>
		/// <returns></returns>
		public static ErrorInfo BadReference<T>(T value) =>
			new ErrorInfo(ErrorCode.BadReference, ErrorDataType.Object, new[] { new ErrorAttrib(nameof(value), value) });

		/// <summary>
		/// The value is not unique.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="dataType"><see cref="ErrorDataType"/> of the value</param>
		/// <returns></returns>
		public static ErrorInfo NotUniqueValue<T>(T value, ErrorDataType dataType) =>
			new ErrorInfo(ErrorCode.NotUniqueValue, dataType, new[] { new ErrorAttrib(nameof(value), value) });

		/// <summary>
		/// The value is not unique.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <returns></returns>
		public static ErrorInfo NotUniqueValue<T>(T value) =>
			new ErrorInfo(ErrorCode.NotUniqueValue, ErrorDataType.Object, new[] { new ErrorAttrib(nameof(value), value) });

		/// <summary>
		/// The value is out of range of the valid values. parameters: value [, min] [, max]
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="dataType"><see cref="ErrorDataType"/> of the value</param>
		/// <returns></returns>
		public static ErrorInfo OutOfRange<T>(T value, ErrorDataType dataType) =>
			new ErrorInfo(ErrorCode.OutOfRange, dataType, new[] { new ErrorAttrib(nameof(value), value) });

		/// <summary>
		/// The value is out of range of the valid values.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <returns></returns>
		public static ErrorInfo OutOfRange<T>(T value) =>
			new ErrorInfo(ErrorCode.OutOfRange, ErrorDataType.Object, new[] { new ErrorAttrib(nameof(value), value) });

		/// <summary>
		/// The value is out of the specified range.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="dataType"><see cref="ErrorDataType"/> of the value</param>
		/// <param name="min">Minimum possible value or null</param>
		/// <param name="max">Maximum possible value or null</param>
		/// <returns></returns>
		public static ErrorInfo OutOfRange<T>(T? value, ErrorDataType dataType, T? min = null, T? max = null) where T: struct =>
			new ErrorInfo(ErrorCode.OutOfRange, dataType, min == null && max == null ? new[] { new ErrorAttrib(nameof(value), value) }:
				min == null ? new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(max), max) }:
				max == null ? new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(min), min) }:
				new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(min), min), new ErrorAttrib(nameof(max), max) });

		/// <summary>
		/// The value is out of the specified range.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="min">Minimum possible value or null</param>
		/// <param name="max">Maximum possible value or null</param>
		/// <returns></returns>
		public static ErrorInfo OutOfRange<T>(T? value, T? min = null, T? max = null) where T : struct =>
			OutOfRange(value, ErrorDataType.Object, min, max);

		/// <summary>
		/// The value is out of the specified range.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="dataType"><see cref="ErrorDataType"/> of the value</param>
		/// <param name="min">Minimum possible value or null</param>
		/// <param name="max">Maximum possible value or null</param>
		/// <returns></returns>
		public static ErrorInfo OutOfRange<T>(T value, ErrorDataType dataType, T min = null, T max = null) where T: class =>
			new ErrorInfo(ErrorCode.OutOfRange, dataType, min == null && max == null ? new[] { new ErrorAttrib(nameof(value), value) } :
				min == null ? new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(max), max) } :
				max == null ? new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(min), min) } :
				new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(min), min), new ErrorAttrib(nameof(max), max) });

		/// <summary>
		/// The value is out of the specified range.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="min">Minimum possible value or null</param>
		/// <param name="max">Maximum possible value or null</param>
		/// <returns></returns>
		public static ErrorInfo OutOfRange<T>(T value, T min = null, T max = null) where T : class =>
			OutOfRange(value, ErrorDataType.Object, min, max);

		/// <summary>
		/// Size of the value exceeds the allowed size.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="dataType"><see cref="ErrorDataType"/> of the value</param>
		/// <param name="size">The maximum possible size for the value</param>
		/// <returns></returns>
		public static ErrorInfo SizeOverflow<T>(T value, ErrorDataType dataType, int size) =>
			new ErrorInfo(ErrorCode.SizeOverflow, dataType, new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(size), size) });

		/// <summary>
		/// Size of the value exceeds the allowed size.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="size">The maximum possible size for the value</param>
		/// <returns></returns>
		public static ErrorInfo SizeOverflow<T>(T value, int size) =>
			new ErrorInfo(ErrorCode.SizeOverflow, ErrorDataType.Object, new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(size), size) });

		/// <summary>
		/// Invalid format of the value.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <param name="dataType"><see cref="ErrorDataType"/> of the value</param>
		/// <returns></returns>
		public static ErrorInfo BadFormat<T>(T value, ErrorDataType dataType) =>
			new ErrorInfo(ErrorCode.BadFormat, dataType, new[] { new ErrorAttrib(nameof(value), value) });

		/// <summary>
		/// Invalid format of the value.
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="value">The value</param>
		/// <returns></returns>
		public static ErrorInfo BadFormat<T>(T value) =>
			new ErrorInfo(ErrorCode.BadFormat, ErrorDataType.Object, new[] { new ErrorAttrib(nameof(value), value) });


		public static string FormatMessage(IReadOnlyList<string> templates, IEnumerable<ErrorAttrib> parameters, string field = default)
		{
			if (templates == null)
				throw new ArgumentNullException(nameof(templates));

			var pp = parameters?.ToIReadOnlyList() ?? Array.Empty<ErrorAttrib>();
			string template = SelectTemplate(templates, pp, field);
			return template == null ? null: FormatMessage(template, pp, field);
		}

		public static string FormatMessage(string template, IReadOnlyList<ErrorAttrib> parameters, string field = default)
		{
			if (template == null)
				throw new ArgumentNullException(nameof(template));

			if (parameters == null || parameters.Count == 0)
				return template;

			return __paramRex.Replace(template, m =>
			{
				if (m.Value == "{{")
					return "{";
				var name = m.Groups[1].Value;
				var i = parameters.FindIndex(o => String.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));
				object value;
				if (i >= 0)
					value = parameters[i].Value;
				else if (field != null && String.Equals(name, "field", StringComparison.OrdinalIgnoreCase))
					value = field;
				else
					return m.Value;
				var format = m.Groups[2].Value;
				return !String.IsNullOrEmpty(format) && value is IFormattable f ?
					f.ToString(format, null):
					value?.ToString();
			});
		}

		private static string SelectTemplate(IReadOnlyList<string> templates, IReadOnlyList<ErrorAttrib> parameters, string field = default)
		{
			string template = null;
			int weight = 0;
			foreach (var item in templates)
			{
				var mm = __paramRex.Matches(item).Cast<Match>().Where(o => o.Value != "{{").ToList();
				if (mm.Any(o => parameters.All(p =>
						!String.Equals(o.Groups[1].Value, p.Name, StringComparison.OrdinalIgnoreCase) &&
						!(field != null && String.Equals(o.Groups[1].Value, "field"))
					)))
					continue;
				if (mm.Count > weight)
				{
					weight = mm.Count;
					template = item;
				}
			}
			return template;
		}
		private static readonly Regex __paramRex = new Regex(@"{{|{\s*(.*?)\s*(?::\s*(.*?)\s*)?}");
	}
}
