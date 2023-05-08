using System.Text.RegularExpressions;

namespace Lexxys;

public class ErrorInfo
{
	public static readonly ErrorInfo Empty = new ErrorInfo(ErrorCode.Default, ErrorDataType.Object, Array.Empty<ErrorAttrib>());
	public ErrorCode Code { get; }
	public ErrorDataType DataType { get; }
	public IReadOnlyList<ErrorAttrib> Attribs { get; }

	public ErrorInfo(ErrorCode code, ErrorDataType dataType, IEnumerable<ErrorAttrib>? attribs)
	{
		Code = code;
		DataType = dataType;
		Attribs = attribs as IReadOnlyList<ErrorAttrib> ?? ReadOnly.WrapCopy(attribs) ?? ReadOnly.Empty<ErrorAttrib>();
	}

	public ErrorInfo(ErrorCode code, ErrorDataType dataType, params ErrorAttrib[]? attribs)
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
	/// The value is out of the specified range.
	/// </summary>
	/// <typeparam name="T">Value type</typeparam>
	/// <param name="value">The value</param>
	/// <param name="dataType"><see cref="ErrorDataType"/> of the value</param>
	/// <param name="min">Minimum possible value or null</param>
	/// <param name="max">Maximum possible value or null</param>
	/// <returns></returns>
	public static ErrorInfo OutOfRange<T>(T value, ErrorDataType dataType, T? min = default, T? max = default) =>
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
	public static ErrorInfo OutOfRange<T>(T value, T? min = default, T? max = default) =>
		OutOfRange(value, ErrorDataType.Object, min, max);

	/// <summary>
	/// The value size is out of the specified range.
	/// </summary>
	/// <typeparam name="T">Value type</typeparam>
	/// <param name="value">The value</param>
	/// <param name="dataType"><see cref="ErrorDataType"/> of the value</param>
	/// <param name="min">Minimum possible value size or null</param>
	/// <param name="max">Maximum possible value size or null</param>
	/// <returns></returns>
	public static ErrorInfo SizeOutOfRange<T>(T value, ErrorDataType dataType, int? max = default, int? min = default) =>
		new ErrorInfo(ErrorCode.OutOfRange, dataType, min == null && max == null ? new[] { new ErrorAttrib(nameof(value), value) } :
			min == null ? new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(max), max) } :
			max == null ? new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(min), min) } :
			new[] { new ErrorAttrib(nameof(value), value), new ErrorAttrib(nameof(min), min), new ErrorAttrib(nameof(max), max) });

	/// <summary>
	/// The value is out of the specified range.
	/// </summary>
	/// <typeparam name="T">Value type</typeparam>
	/// <param name="value">The value</param>
	/// <param name="min">Minimum possible value size or null</param>
	/// <param name="max">Maximum possible value size or null</param>
	/// <returns></returns>
	public static ErrorInfo SizeOutOfRange<T>(T value, int? max = default, int? min = default) =>
		SizeOutOfRange(value, ErrorDataType.Object, max, min);

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


	public static string? FormatMessage(IEnumerable<string> templates, IEnumerable<ErrorAttrib>? parameters, string? field = default)
	{
		if (templates == null)
			throw new ArgumentNullException(nameof(templates));

		var pp = parameters?.ToIReadOnlyList() ?? Array.Empty<ErrorAttrib>();
		string? template = SelectTemplate(templates, pp, field);
		return template == null ? null: FormatMessage(template, pp, field);
	}

	public static string FormatMessage(string template, IReadOnlyList<ErrorAttrib>? parameters, string? field = default)
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
			object? value;
			if (i >= 0)
				value = parameters[i].Value;
			else if (field != null && String.Equals(name, "field", StringComparison.OrdinalIgnoreCase))
				value = field;
			else
				return m.Value;
			var format = m.Groups[2].Value;
			return !String.IsNullOrEmpty(format) && value is IFormattable f ?
				f.ToString(format, null):
				value?.ToString() ?? "";
		});
	}

	private static string? SelectTemplate(IEnumerable<string> templates, IReadOnlyList<ErrorAttrib>? parameters, string? field = default)
	{
		if (parameters is null || parameters.Count == 0)
			return templates.FirstOrDefault(o => __paramRex.Matches(o).Count == 0);
		string? template = null;
		int weight = 0;
		foreach (var item in templates)
		{
			var mm = __paramRex.Matches(item);
			if (mm.Count <= weight)
				continue;
			int count = 0;
			for (var i = 0; i < mm.Count; i++)
			{
				if (mm[i].Value == "{{")
					continue;
				var name = mm[i].Groups[1].Value;
				var attr = parameters.FirstOrDefault(o => String.Equals(name, o.Name, StringComparison.OrdinalIgnoreCase));
				if (attr.Name == null)
					if (field == null || !String.Equals(name, "field", StringComparison.OrdinalIgnoreCase))
					{
						count = 0;
						break;
					}
				++count;
			}
			if (count <= weight)
				continue;

			weight = count;
			template = item;
		}
		return template;
	}
	private static readonly Regex __paramRex = new Regex(@"{{|{\s*(.*?)\s*(?::\s*(.*?)\s*)?}");
}
