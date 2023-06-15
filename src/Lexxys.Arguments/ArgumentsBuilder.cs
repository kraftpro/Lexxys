using System.Collections;
using System.Linq;
using System.Text;

using Lexxys;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

/// <summary>
/// Builder for <see cref="Arguments"/> class.
/// </summary>
public class ArgumentsBuilder
{
	private CommandDefinitionCollection _commands;
	private CommandDefinition _selected;
	private StringComparison _comparison;

	private bool _allowSlash;				// true if slash is a switch prefix
	private bool _strictDdName;				// true if all arguments longer than 1 character must start with --. otherwise - is allowed for any parameter with value.
	private bool _combineOptions;			// true if short options can be combined (i.e. -abc is equivalent to -a -b -c), so all long options must start with --.

	/*
	-- Windows style
	strictLongName: false, combineShort: false
		In the command line behaviors of double dash and single dash are the same. Options can not be combined.
	
	-- Unix compatible style (default)
	strictLongName: false, combineShort: true
		All the long name arguments must start with a double dash except arguments with the value specific (i.e. -size=20). Options can be combined.
	
	strictLongName: true, combineShort: false -- nonsense, but possible.
		All the long name arguments must start with a double dash. Options can not be combined.
		
	-- Strict Unix style
	strictLongName: true, combineShort: true
		All the long name arguments must start with a double dash. Options can be combined.


	 */

	private bool _colonSeparator;			// true if colon is a separator between parameter name and value
	private bool _equalSeparator;			// true if equal sign is a separator between parameter name and value
	private bool _blankSeparator;			// true if blank is a separator between parameter name and value
	private bool _allowUnknown;				// true if unknown parameters are allowed
	private bool _doubleDashSeparator;		// true if double dash is a separator between options and positional parameters
	private bool _ignoreNameSeparators;     // true if delimiters ('-', '_', and '.') in the parameter name must be trimmed before matching


	/// <summary>
	/// Creates new instance of <see cref="ArgumentsBuilder"/> class with default options:<br/>
	/// - Case-insensitive command and parameter names.<br/>
	/// - Don't allow slash as a parameter prefix.<br/>
	/// - Double dash doesn't require for long arguments.<br/>
	/// - Combine short options (i.e., abc is equivalent to -a -b -c).<br/>
	/// - Colon, equal sing, and blank are separators between the argument name and value.<br/>
	/// - Double dash is a separator between the command options and positional parameters.<br/>
	/// - Consider delimiters ('-', '_', and '.') in the parameter name while matching.<br/>
	/// - All not matched arguments are errors.
	/// </summary>
	/// <param name="ignoreCase">Specifies whether the command and parameter names are case-insensitive.</param>
	/// <param name="allowSlash">True if slash is a switch prefix.</param>
	/// <param name="strictDoubleDash">True if all arguments longer than 1 character must start with --. otherwise - is allowed for any parameter with value.</param>
	/// <param name="combineOptions">True if short options can be combined (i.e. -abc is equivalent to -a -b -c), so all long options must start with --.</param>
	/// <param name="colonSeparator">True if colon is a separator between parameter name and value.</param>
	/// <param name="equalSeparator">True if equal sign is a separator between parameter name and value.</param>
	/// <param name="blankSeparator">True if blank is a separator between parameter name and value.</param>
	/// <param name="doubleDashSeparator">True if unknown parameters are allowed.</param>
	/// <param name="ignoreNameSeparators">True if double dash is a separator between options and positional parameters.</param>
	/// <param name="allowUnknown">True if delimiters ('-', '_', and '.') in the parameter name must be trimmed before matching.</param>
	public ArgumentsBuilder(
		bool ignoreCase = false,
		bool allowSlash = false,
		bool strictDoubleDash = false,
		bool combineOptions = true,
		bool colonSeparator = true,
		bool equalSeparator = true,
		bool blankSeparator = true,
		bool doubleDashSeparator = true,
		bool ignoreNameSeparators = false,
		bool allowUnknown = false)
	{
		_comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal;
		_commands = new CommandDefinitionCollection(_comparison);
		_selected = _commands.Default;

		_allowSlash = allowSlash;
		_strictDdName = strictDoubleDash;
		_combineOptions = combineOptions;
		_colonSeparator = colonSeparator;
		_equalSeparator = equalSeparator;
		_blankSeparator = blankSeparator;
		_doubleDashSeparator = doubleDashSeparator;
		_ignoreNameSeparators = ignoreNameSeparators;
		_allowUnknown = allowUnknown;
	}
	
	internal CommandDefinitionCollection Commands => _commands;

	internal StringComparison Comparison => _comparison;

	/// <summary>
	/// Indicates that a slash is a legal option prefix.
	/// </summary>
	public bool UseSlashPrefix => _allowSlash;

	/// <summary>
	/// Indicates that all arguments longer than one character MUST start with a double dash character.
	/// </summary>
	public bool HasStrictDoubleDashNameRule => _strictDdName;

	/// <summary>
	/// Indicates that single character options can be combined (i.e. -abc is equivalent to -a -b -c).
	/// </summary>
	public bool CanCombiningOptions => _combineOptions;
	
	/// <summary>
	/// Indicates that colon is a separator between the argument name and value.
	/// </summary>
	public bool UseColonSeparator => _colonSeparator;

	/// <summary>
	/// Indicates that equal sign is a separator between the argument name and value.
	/// </summary>
	public bool UseEqualSeparator => _equalSeparator;
	
	/// <summary>
	/// Indicates that blank is a separator between the argument name and value.
	/// </summary>
	public bool UseBlankSeparator => _blankSeparator;
	
	/// <summary>
	/// Indicates unknown (not defined ahead) parameters are allowed.
	/// </summary>
	public bool CollectUnknownParameters => _allowUnknown;
	
	/// <summary>
	/// Indicates that double dash is a separator between the command options and positional parameters.
	/// </summary>
	public bool UseDoubleDashSeparator => _doubleDashSeparator;

	/// <summary>
	/// Indicates that delimiters ('-', '_', and '.') in the parameter name will be ignored while matching.
	/// </summary>
	public bool IgnoreNameSeparators => _ignoreNameSeparators;
	
	/// <summary>
	/// Selects Windows style command line arguments:<br/>
	/// - Slash is a legal option prefix.<br/>
	/// - Don't combine short options (i.e. -abc is not equivalent to -a -b -c).<br/>
	/// - Disable double dash as a positional parameters separator.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder WindowsStyle()
	{
		_allowSlash = true;
		_combineOptions = false;
		_doubleDashSeparator = false;
		if (_comparison != StringComparison.OrdinalIgnoreCase)
			_commands = new CommandDefinitionCollection(_commands, _comparison = StringComparison.OrdinalIgnoreCase);
		return this;
	}

	/// <summary>
	/// Selects Unix style command line arguments:<br/>
	/// - Disable colon separator<br/>
	/// - All arguments longer than one character must start with --, except arguments with the value specific (i.e. -size=20).<br/>
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder UnixStyle()
	{
		_colonSeparator = false;
		_strictDdName = true;
		if (_comparison != StringComparison.Ordinal)
			_commands = new CommandDefinitionCollection(_commands, _comparison = StringComparison.Ordinal);
		return this;
	}

	/// <summary>
	/// Selects case-insensitive mode.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder IgnoreCase()
	{
		if (_comparison != StringComparison.OrdinalIgnoreCase)
			_commands = new CommandDefinitionCollection(_commands, _comparison = StringComparison.OrdinalIgnoreCase);
		return this;
	}

	/// <summary>
	/// Allows slash as a parameters prefix.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder AllowSlash()
	{
		_allowSlash = true;
		return this;
	}

	/// <summary>
	/// Sets that all arguments longer than one character MUST start with --.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder StrictLongName()
	{
		_strictDdName = true;
		return this;
	}

	/// <summary>
	/// Don't allow combining one char options (where -abc is equivalent to -a -b -c).
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder DoNotCombineShort()
	{
		_combineOptions = false;
		return this;
	}

	/// <summary>
	/// Disables colon as a separator between parameter name and value.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder DisableColonSeparator()
	{
		_colonSeparator = false;
		return this;
	}

	/// <summary>
	/// Disables equal sign as a separator between parameter name and value.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder DisableEqualSeparator()
	{
		_equalSeparator = false;
		return this;
	}

	/// <summary>
	/// Disables blank as a separator between parameter name and value.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder DisableBlankSeparator()
	{
		_blankSeparator = false;
		return this;
	}

	/// <summary>
	/// Disables double dash as a separator between command options and positional parameters.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder DisablePositionalParameterDelimiter()
	{
		_doubleDashSeparator = false;
		return this;
	}

	/// <summary>
	/// Indicates delimiter chars ('-', '_', and '.') in the parameter name must be removed before the matching.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder IgnoreNameDelimiters()
	{
		_ignoreNameSeparators = true;
		return this;
	}

	/// <summary>
	/// Allows to collect not defined parameters.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder AllowUnknownParameters()
	{
		_allowUnknown = true;
		return this;
	}

	/// <summary>
	/// Specifies a new or an existing command to which parameters will be added.
	/// </summary>
	/// <param name="name">Name of the command or <c>null</c></param>
	/// <param name="description">Description for the new command</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder Command(string? name = null, string? description = null)
	{
		_selected = _commands.GetOrCreate(name ?? String.Empty, description);
		return this;
	}

	/// <summary>
	/// Adds parameter to the current command.
	/// </summary>
	/// <param name="parameter">The parameter to be added.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ArgumentsBuilder Add(ParameterDefinition parameter)
	{
		if (parameter is null)
			throw new ArgumentNullException(nameof(parameter));
		_selected.Add(parameter);
		return this;
	}

	/// <summary>
	/// Adds parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">An optional abbreviation for the parameter.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="positional">Indicates that this is a positional parameter.</param>
	/// <param name="valueRequired">Indicates that a parameter value is required</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ArgumentsBuilder Parameter(string name, string? abbrev = null, string? valueName = null, string? description = null, bool positional = false, bool valueRequired = false, bool required = false)
	{
		_selected.Add(new ParameterDefinition(name, valueRequired ? ParameterValueType.Required: ParameterValueType.Optional, abbrev == null ? null: new[] { abbrev }, valueName, description, positional, required));
		return this;
	}

	/// <summary>
	/// Adds parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">Abbreviations for the parameter.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="positional">Indicates that this is a positional parameter.</param>
	/// <param name="valueRequired">Indicates that a parameter value is required</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ArgumentsBuilder Parameter(string name, string[] abbrev, string? valueName = null, string? description = null, bool positional = false, bool valueRequired = false, bool required = false)
	{
		_selected.Add(new ParameterDefinition(name, valueRequired ? ParameterValueType.Required: ParameterValueType.Optional, abbrev, valueName, description, positional, required));
		return this;
	}

	// /// <summary>
	// /// Adds a parameter to the current command.
	// /// </summary>
	// /// <typeparam name="T">Type of the parameter value.</typeparam>
	// /// <param name="name">Name of the parameter.</param>
	// /// <param name="abbrev">An optional abbreviation for the parameter.</param>
	// /// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	// /// <param name="description">An optional parameter description for the usage message.</param>
	// /// <param name="positional">Indicates that this is a positional parameter.</param>
	// /// <param name="required">Indicates that this is a required parameter.</param>
	// /// <returns>The <see cref="ArgumentsBuilder"/></returns>
	// public ArgumentsBuilder Parameter<T>(string name, string? abbrev = null, string? valueName = null, string? description = null, bool positional = false, bool required = false)
	// {
	// 	_selected.Add(new ParameterDefinition(name, typeof(T), abbrev == null ? null: new[] { abbrev }, valueName, description, positional, required));
	// 	return this;
	// }
	//
	// /// <summary>
	// /// Adds a parameter to the current command.
	// /// </summary>
	// /// <typeparam name="T">Type of the parameter value.</typeparam>
	// /// <param name="name">Name of the parameter.</param>
	// /// <param name="abbrev">Abbreviations for the parameter.</param>
	// /// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	// /// <param name="description">An optional parameter description for the usage message.</param>
	// /// <param name="positional">Indicates that this is a positional parameter.</param>
	// /// <param name="required">Indicates that this is a required parameter.</param>
	// /// <returns>The <see cref="ArgumentsBuilder"/></returns>
	// public ArgumentsBuilder Parameter<T>(string name, string[] abbrev, string? valueName = null, string? description = null, bool positional = false, bool required = false)
	// {
	// 	_selected.Add(new ParameterDefinition(name, typeof(T), abbrev, valueName, description, positional, required));
	// 	return this;
	// }

	/// <summary>
	/// Adds a <see cref="bool"/> parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">An optional abbreviation for the parameter.</param>
	/// <param name="description">An optional parameter description for the usage message</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder Switch(string name, string? abbrev = null, string? description = null)
	{
		_selected.Add(new ParameterDefinition(name, ParameterValueType.None, abbrev == null ? null: new[] { abbrev }, null, description, false, false));
		return this;
	}

	/// <summary>
	/// Adds a <see cref="bool"/> parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">Abbreviations for the parameter.</param>
	/// <param name="description">An optional parameter description for the usage message</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder Switch(string name, string[] abbrev, string? description = null)
	{
		_selected.Add(new ParameterDefinition(name, ParameterValueType.None, abbrev, null, description, false, false));
		return this;
	}

	/// <summary>
	/// Adds a multi-value parameter (parameter of type <see cref="string"/>[]) to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">An optional abbreviation for the parameter.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="positional">Indicates that this is a positional parameter.</param>
	/// <param name="valueRequired">Indicates that a parameter value is required</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder Array(string name, string? abbrev = null, string? description = null, string? valueName = null, bool positional = false, bool valueRequired = false, bool required = false)
	{
		_selected.Add(new ParameterDefinition(name, ParameterValueType.Collection | (valueRequired ? ParameterValueType.Required: ParameterValueType.Optional), abbrev == null ? null: new[] { abbrev }, valueName, description, positional, required));
		return this;
	}

	/// <summary>
	/// Adds a multi-value parameter (parameter of type <see cref="string"/>[]) to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">Abbreviations for the parameter.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="valueRequired">Indicates that a parameter value is required</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder Array(string name, string[] abbrev, string? description = null, string? valueName = null, bool valueRequired = false, bool required = false)
	{
		_selected.Add(new ParameterDefinition(name, ParameterValueType.Collection | (valueRequired ? ParameterValueType.Required: ParameterValueType.Optional), abbrev, valueName, description, false, required));
		return this;
	}

	/// <summary>
	/// Adds a positional parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder Positional(string name, string? description = null, string? valueName = null, bool required = false)
	{
		_selected.Add(new ParameterDefinition(name, ParameterValueType.None, null, valueName, description, true, required));
		return this;
	}

	/// <summary>
	/// Adds a positional multi-value parameter (parameter of type <see cref="string"/>[]) to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder PositionalArray(string name, string? description = null, string? valueName = null, bool required = false)
	{
		_selected.Add(new ParameterDefinition(name, ParameterValueType.Collection, null, valueName, description, true, required));
		return this;
	}

	/// <summary>
	/// Parses the specified arguments and returns an <see cref="Arguments"/> instance.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns></returns>
	public Arguments Build(IEnumerable<string> args) => new(args, this);
}
