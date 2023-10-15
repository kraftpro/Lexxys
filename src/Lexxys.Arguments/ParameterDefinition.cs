using System.Text;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

/// <summary>
/// Represents a command line parameter.
/// </summary>
public class ParameterDefinition
{
	public const char HiddenPrefix = '\r';

	/// <summary>
	/// Creates a new instance of <see cref="ParameterDefinition"/>.
	/// </summary>
	/// <param name="command">The command this parameter belongs to.</param>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbreviations">An optional abbreviations for the parameter.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="positional">Indicates that this is a positional parameter.</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <param name="collection">Indicates that this is a collection parameter.</param>
	/// <param name="toggle">Indicates that this is a switch parameter.</param>
	/// <param name="unknown">Indicates that this is an unknown parameter.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ParameterDefinition(CommandDefinition command, string name, string[]? abbreviations = null, string? valueName = null, string? description = null, bool positional = false, bool required = false, bool collection = false, bool toggle = false, bool unknown = false)
	{
		if (command is null)
			throw new ArgumentNullException(nameof(command));
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		Name = FixName(name);
		Abbreviations = abbreviations ?? [];
		ValueName = valueName.TrimToNull();
		Description = description;
		Command = command;
		IsPositional = positional;
		IsRequired = required;
		IsCollection = collection;
		IsSwitch = toggle;
		IsUnknown = unknown;

		static string FixName(string name) => name.Trim().Replace(' ', '-');
	}

	/// <summary>
	/// Name of the parameter.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Abbreviations of the parameter.
	/// </summary>
	public string[] Abbreviations { get; }

	/// <summary>
	/// Tests if the parameter has any abbreviations.
	/// </summary>
	public bool HasAbbreviation => Abbreviations.Any(o => o.Length > 0 && o[0] != HiddenPrefix);

	/// <summary>
	/// Name of the parameter value to be displayed in the usage message.
	/// </summary>
	public string? ValueName { get; }

	/// <summary>
	/// Description of the parameter.
	/// </summary>
	public string? Description { get; }

	/// <summary>
	/// Reference to the command this parameter belongs to.
	/// </summary>
	public CommandDefinition Command { get; }

	/// <summary>
	/// Indicates that this is a positional parameter.
	/// </summary>
	public bool IsPositional { get; }

	/// <summary>
	/// Indicates that this is a required parameter.
	/// </summary>
	public bool IsRequired { get; }

	/// <summary>
	/// Indicates that this is a collection parameter (i.e. <see cref="Array"/> type).
	/// </summary>
	public bool IsCollection { get; }

	/// <summary>
	/// Indicates that this is a switch parameter (i.e. <see cref="bool"/> type).
	/// </summary>
	public bool IsSwitch { get; }

	/// <summary>
	/// Indicates that the parameter was not defined but founds in the arguments list.
	/// </summary>
	public bool IsUnknown { get; }

	internal StringBuilder GetParameterName(StringBuilder? text = null, char argumentDelimiter = '\0', bool longDash = false, bool excludeAbbreviation = false)
	{
		text ??= new StringBuilder();
		if (!IsPositional)
		{
			if (!excludeAbbreviation)
			{
				foreach (var a in Abbreviations.Where(o => o[0] != HiddenPrefix))
				{
					text.Append('-').Append(a).Append(", ");
				}
			}
			text.Append('-');
			if (longDash && Name.Length > 1)
				text.Append('-');
			text.Append(Name);

			if (IsSwitch || argumentDelimiter == '\0')
				return text;
			text.Append(argumentDelimiter);
		}

		text.Append('<').Append(ValueName is null ? Name: ValueName).Append('>');
		if (IsCollection)
			text.Append("[,<...>]");
		return text;
	}

	internal bool IsSimilar(string value, StringComparison comparison, bool trimDelimiters)
		=> IsSimilar(value.AsSpan(), Name.AsSpan(), Strings.SplitByCapitals(Name), 0, comparison, trimDelimiters);

	internal bool IsReverseSimilar(string value, StringComparison comparison, bool trimDelimiters)
		=> IsSimilar(Name.AsSpan(), value.AsSpan(), Strings.SplitByCapitals(value), 0, comparison, trimDelimiters);

	private static bool IsSimilar(ReadOnlySpan<char> value, ReadOnlySpan<char> name, IList<(int Index, int Length)> parts, int maskIndex, StringComparison comparison, bool trimDelimiters)
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
			foreach (var c in value)
			{
				if (!IsDelimiter(c))
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
}
