using System.Text;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

/// <summary>
/// Represents a command line parameter.
/// </summary>
public class ParameterDefinition
{
	/// <summary>
	/// Creates a new instance of <see cref="ParameterDefinition"/>.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbreviations">An optional abbreviations for the parameter.</param>
	/// <param name="type">Type of the parameter value (default <see cref="string"/>).</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="positional">Indicates that this is a positional parameter.</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ParameterDefinition(string name, ParameterValueType? type, string[]? abbreviations = null, string? valueName = null, string? description = null, bool positional = false, bool required = false)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		Name = FixName(name);
		Abbreviations = abbreviations;
		ValueName = valueName;
		Description = description;
		Type = type ?? ParameterValueType.Optional;
		IsPositional = positional;
		IsRequired = required;
	}

	/// <summary>
	/// Value of the command line argument.
	/// </summary>
	public string? Value { get; private set; }

	/// <summary>
	/// Name of the parameter.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Abbreviations of the parameter.
	/// </summary>
	public string[]? Abbreviations { get; }

	/// <summary>
	/// Name of the parameter value to be displayed in the usage message.
	/// </summary>
	public string? ValueName { get; }

	/// <summary>
	/// Description of the parameter.
	/// </summary>
	public string? Description { get; }

	/// <summary>
	/// Type of the parameter value.
	/// </summary>
	public ParameterValueType Type { get; }

	/// <summary>
	/// Indicates that this is a positional parameter.
	/// </summary>
	public bool IsPositional { get; }

	/// <summary>
	/// Indicates that this is a required parameter.
	/// </summary>
	public bool IsRequired { get; }

	/// <summary>
	/// Indicates that this is a switch parameter (i.e. <see cref="bool"/> type).
	/// </summary>
	public bool IsSwitch => Type == ParameterValueType.None;

	/// <summary>
	/// Indicates that this is a collection parameter (i.e. <see cref="Array"/> type).
	/// </summary>
	public bool IsCollection => (Type & ParameterValueType.Collection) != 0;

	/// <summary>
	/// Indicates that the parameter value is required.
	/// </summary>
	public bool IsValueRequired => (Type & ParameterValueType.Required) != 0;

	/// <summary>
	/// Indicates that the parameter value is set.
	/// </summary>
	public bool IsSet => Value is not null;
	
	/// <summary>
	/// Indicates that the parameter was not defined but founds in the arguments list.
	/// </summary>
	public bool IsUnknown => (Type & (ParameterValueType.Optional | ParameterValueType.Required)) == (ParameterValueType.Optional | ParameterValueType.Required);
	
	/// <summary>
	/// Sets the value of the parameter.
	/// </summary>
	/// <param name="value">The value of the parameter</param>
	/// <returns></returns>
	public ParameterDefinition WithValue(string? value)
	{
		Value = value;
		return this;
	}

	internal void AddValue(string? value)
	{
		if (!IsCollection)
			Value = value;
		else if (Value is null)
			Value = value;
		else if (value is not null)
			Value += $",{value}";
	}
	
	internal string GetParameterName(char argumentDelimiter = '\0', bool longDash = false, bool excludeAbbreviation = false)
	{
		if (IsPositional)
			return ValueName ?? Name;

		var text = new StringBuilder();

		if (!excludeAbbreviation && Abbreviations is { Length: > 0 })
		{
			foreach (var a in Abbreviations)
			{
				text.Append('-').Append(a).Append(", ");
			}
		}
		text.Append('-');
		if (longDash && Name.Length > 1)
			text.Append('-');
		text.Append(Name);

		if (Type == ParameterValueType.None || argumentDelimiter == '\0')
			return text.ToString();
		text.Append(argumentDelimiter);
		text.Append(ValueName is { Length: > 0 } ? $"<{ValueName}>" : "<value>");
		if (IsCollection)
			text.Append("[,<...>]");
		return text.ToString();
	}

	internal bool IsMatch(string value, IEqualityComparer<string> comparer)
		=> comparer.Equals(value, Name) || (Abbreviations != null && Array.FindIndex(Abbreviations, o => comparer.Equals(o, value)) >= 0);

	internal bool IsMatch(string value, StringComparison comparison)
		=> String.Equals(value, Name, comparison) || (Abbreviations != null && Array.FindIndex(Abbreviations, o => String.Equals(o, value, comparison)) >= 0);

	internal bool IsSimilar(string value, StringComparison comparison, bool trimDelimiters)
		=> IsSimilar(value.AsSpan(), Name.AsSpan(), Strings.SplitByCapitals(Name), 0, comparison, trimDelimiters);

	internal static bool IsSimilar(ReadOnlySpan<char> value, ReadOnlySpan<char> name, IList<(int Index, int Length)> parts, int maskIndex, StringComparison comparison, bool trimDelimiters)
	{
		if (value.Length == 0)
			return maskIndex == parts.Count;

		bool hasDelimiter = IsDelimiter(value[0]);
		if (hasDelimiter)
			value = TrimDelimiters(value);

		var mask = name.Slice(parts[maskIndex].Index, parts[maskIndex].Length);
		if (IsDelimiters(mask))
			return (hasDelimiter || trimDelimiters) && IsSimilar(value, name, parts, maskIndex + 1, comparison, trimDelimiters);

		if (maskIndex == parts.Count - 1)
			return mask.StartsWith(value, comparison);

		for (int i = 1; i <= mask.Length && i <= value.Length; ++i)
		{
			if (!mask.StartsWith(value.Slice(0, i), comparison))
				return false;
			if (IsSimilar(value.Slice(i), name, parts, maskIndex + 1, comparison, trimDelimiters))
				return true;
		}
		return false;

		static bool IsDelimiters(ReadOnlySpan<char> value)
		{
			for (int i = 0; i < value.Length; ++i)
			{
				if (!IsDelimiter(value[i]))
					return false;
			}
			return true;
		}

		static ReadOnlySpan<char> TrimDelimiters(ReadOnlySpan<char> value)
		{
			while (value.Length > 0 && IsDelimiter(value[0]))
				value = value.Slice(1);
			return value;
		}

		static bool IsDelimiter(char value)
			=> value is '-' or '_' or ' ';
	}

	private static string FixName(string name) => name.Trim().Replace(' ', '-');
}
