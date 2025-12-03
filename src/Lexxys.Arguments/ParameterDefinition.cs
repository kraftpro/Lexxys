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
	/// Prefix character for hidden abbreviations (not displayed in the usage message).
	/// </summary>
	public const char HiddenPrefix = '\r';

	/// <summary>
	/// Creates a new instance of <see cref="ParameterDefinition"/>.
	/// </summary>
	/// <param name="command">The command this parameter belongs to.</param>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbreviation">An optional abbreviations for the parameter.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="positional">Indicates that this is a positional parameter.</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <param name="collection">Indicates that this is a collection parameter.</param>
	/// <param name="toggle">Indicates that this is a switch parameter.</param>
	/// <exception cref="ArgumentNullException"></exception>
	internal ParameterDefinition(CommandDefinition command, string name, string[]? abbreviation = null, string? valueName = null, string? description = null, bool positional = false, bool required = false, bool collection = false, bool toggle = false)
	{
		if (command is null) throw new ArgumentNullException(nameof(command));
		if (name is null) throw new ArgumentNullException(nameof(name));

		Name = FixName(name);
		Abbreviation = abbreviation ?? [];
		ValueName = valueName.TrimToNull();
		Description = description;
		Command = command;
		IsPositional = positional;
		IsRequired = required;
		IsCollection = collection;
		IsSwitch = toggle;

		static string FixName(string name) => name.Trim().Replace(' ', '-');
	}

	/// <summary>
	/// Name of the parameter.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Abbreviations of the parameter.
	/// </summary>
	public IReadOnlyList<string> GetAbbreviations() => Abbreviation;

	internal string[] Abbreviation { get; }

	/// <summary>
	/// Tests if the parameter has any abbreviations.
	/// </summary>
	public bool HasAbbreviation => Abbreviation.Any(o => o.Length > 0 && o[0] != HiddenPrefix);

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

	internal StringBuilder GetParameterName(StringBuilder? text = null, char argumentDelimiter = '\0', bool longDash = false, bool excludeAbbreviation = false)
	{
		text ??= new StringBuilder();
		if (!IsPositional)
		{
			if (!excludeAbbreviation)
			{
				foreach (var a in Abbreviation.Where(o => o[0] != HiddenPrefix))
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
}
