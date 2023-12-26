using System.Reflection;

namespace Lexxys;

/// <summary>
/// Builder for <see cref="Arguments"/> class.
/// </summary>
public class ArgumentsBuilder
{
	private CommandDefinition _current;

	private bool _allowSlash;				// true if slash is a switch prefix
	private bool _strictDoubleDash;			// true if all arguments longer than 1 character must start with --. otherwise - is allowed for any parameter with value.
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
	private bool _splitPositional;		// true if positional parameters shouldn't be combined.


	/// <summary>
	/// Creates new instance of <see cref="ArgumentsBuilder"/> class with default options:<br/>
	/// - Case-insensitive command and parameter names.<br/>
	/// - Don't allow slash as a parameter prefix.<br/>
	/// - Double dash doesn't require for long arguments.<br/>
	/// - Don't combine short options (i.e., -abc is equivalent to -a -b -c).<br/>
	/// - Colon, equal sing, and blank are separators between the argument name and value.<br/>
	/// - Double dash is a separator between the command options and positional parameters.<br/>
	/// - Consider delimiters ('-', '_', and '.') in the parameter name while matching.<br/>
	/// - All not matched arguments are errors.
	/// </summary>
	/// <param name="ignoreCase">Specifies whether the command and parameter names are case-insensitive.</param>
	/// <param name="allowSlash">True if slash is a switch prefix.</param>
	/// <param name="strictDoubleDash">True if all arguments longer than 1 character must start with --. otherwise - is allowed for any parameter with value.</param>
	/// <param name="combineOptions">True if short options can be combined (i.e. -abc is equivalent to -a -b -c), so all long options must start with --.</param>
	/// <param name="doubleDashSeparator">True if double dash is a separator between options and positional parameters.</param>
	/// <param name="ignoreNameSeparators">True if delimiters ('-', '_', and '.') in the parameter name must be trimmed before matching.</param>
	/// <param name="allowUnknown">True if unknown parameters are allowed.</param>
	/// <param name="splitPositional">True if the last positional parameters shouldn't be combined.</param>
	/// <param name="colonSeparator">True if colon is a separator between parameter name and value.</param>
	/// <param name="equalSeparator">True if equal sign is a separator between parameter name and value.</param>
	/// <param name="blankSeparator">True if blank is a separator between parameter name and value.</param>
	public ArgumentsBuilder(
		bool ignoreCase = false,
		bool allowSlash = false,
		bool strictDoubleDash = false,
		bool combineOptions = false,
		bool doubleDashSeparator = false,
		bool ignoreNameSeparators = false,
		bool allowUnknown = false,
		bool splitPositional = false,
		bool colonSeparator = true,
		bool equalSeparator = true,
		bool blankSeparator = true)
	{
		Root = new CommandDefinition(null, String.Empty, comparison: ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		_current = Root;

		_allowSlash = allowSlash;
		_strictDoubleDash = strictDoubleDash;
		_combineOptions = combineOptions;
		_colonSeparator = colonSeparator;
		_equalSeparator = equalSeparator;
		_blankSeparator = blankSeparator;
		_doubleDashSeparator = doubleDashSeparator;
		_ignoreNameSeparators = ignoreNameSeparators;
		_allowUnknown = allowUnknown;
		_splitPositional = splitPositional;
	}

	public CommandDefinition Root { get; }

	/// <summary>
	/// Indicates that a slash is a legal option prefix.
	/// </summary>
	public bool UseSlashPrefix => _allowSlash;

	/// <summary>
	/// Indicates that all arguments longer than one character MUST start with a double dash character.
	/// </summary>
	public bool HasStrictDoubleDashNameRule => _strictDoubleDash;

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
	/// Indicates that the last positional parameters shouldn't be combined.
	/// </summary>
	public bool SplitPositional => _splitPositional;
	
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
		if (_current.Comparison != StringComparison.OrdinalIgnoreCase)
			_current = new CommandDefinition(_current, StringComparison.OrdinalIgnoreCase);
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
		_strictDoubleDash = true;
		if (_current.Comparison != StringComparison.Ordinal)
			_current = new CommandDefinition(_current, StringComparison.Ordinal);
		return this;
	}

	/// <summary>
	/// Selects case-insensitive mode.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder IgnoreCase()
	{
		if (_current.Comparison != StringComparison.OrdinalIgnoreCase)
			_current = new CommandDefinition(_current, StringComparison.OrdinalIgnoreCase);
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
		_strictDoubleDash = true;
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
	/// Enables double dash as a separator between command options and positional parameters.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder EnablePositionalParameterDelimiter()
	{
		_doubleDashSeparator = true;
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
	public ArgumentsBuilder EnableUnknownParameters()
	{
		_allowUnknown = true;
		return this;
	}
	
	/// <summary>
	/// Specifies that the last positional parameters shouldn't be combined.
	/// </summary>
	/// <returns></returns>
	public ArgumentsBuilder SeparatePositionalParameters()
	{
		_splitPositional = false;
		return this;
	}

	/// <summary>
	/// Specifies a new or an existing command to which parameters will be added.
	/// </summary>
	/// <param name="name">Name of the command or <c>null</c></param>
	/// <param name="description">Description for the new command</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder BeginCommand(string name, string? description = null)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));
		_current = new CommandDefinition(_current, name, description: description);
		return this;
	}

	/// <summary>
	/// Specifies a new or an existing command to which parameters will be added.
	/// </summary>
	/// <param name="name">Name of the command or <c>null</c></param>
	/// <param name="description">Description for the new command</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder BeginCommand(string name, string[]? abbreviation, string? description = null)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));
		_current = new CommandDefinition(_current, name, abbreviation, description);
		return this;
	}

	/// <summary>
	/// Ends the current command and returns to the parent command.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public ArgumentsBuilder EndCommand()
	{
		_current = _current.Parent ?? throw new InvalidOperationException("There is no command to end.");
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
		_current.Add(parameter);
		return this;
	}

	/// <summary>
	/// Adds parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">An optional abbreviation for the parameter.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="collection">Indicates that this parameter is a collection.</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ArgumentsBuilder Parameter(string name, string? abbrev = null, string? valueName = null, string? description = null, bool collection = false, bool required = false)
	{
		_current.Add(new ParameterDefinition(_current, name, abbrev == null ? null : [abbrev], valueName, description: description, collection: collection, required: required));
		return this;
	}

	/// <summary>
	/// Adds parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">Abbreviations for the parameter.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="collection">Indicates that this parameter is a collection.</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ArgumentsBuilder Parameter(string name, string[]? abbrev, string? valueName = null, string? description = null, bool collection = false, bool required = false)
	{
		_current.Add(new ParameterDefinition(_current, name, abbrev, valueName, description: description, collection: collection, required: required));
		return this;
	}

	/// <summary>
	/// Adds help parameter to the current command.
	/// </summary>
	/// <param name="description">An optional description for the usage command (default: "show help and exit")</param>
	/// <returns></returns>
	public ArgumentsBuilder Help(string? description = null)
	{
		_current.Add(new ParameterDefinition(_current, "help", ["\r?", "h"], description: description ?? "show help and exit", toggle: true));
		return this;
	}

	/// <summary>
	/// Adds a <see cref="bool"/> parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="abbrev">An optional abbreviation for the parameter.</param>
	/// <param name="description">An optional parameter description for the usage message</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder Switch(string name, string? abbrev = null, string? description = null)
	{
		_current.Add(new ParameterDefinition(_current, name, abbrev == null ? null : [abbrev], description: description, toggle: true));
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
		_current.Add(new ParameterDefinition(_current, name, abbrev, description: description, toggle: true));
		return this;
	}

	/// <summary>
	/// Adds a positional parameter to the current command.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="description">An optional parameter description for the usage message.</param>
	/// <param name="valueName">An optional name for the parameter value to be displayed in the usage message.</param>
	/// <param name="collection">Indicates that this is a multi-value parameter.</param>
	/// <param name="required">Indicates that this is a required parameter.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	public ArgumentsBuilder Positional(string name, string? description = null, string? valueName = null, bool collection = false, bool required = false)
	{
		_current.Add(new ParameterDefinition(_current, name, null, valueName, description: description, positional: true, collection: collection, required: required));
		return this;
	}

	/// <summary>
	/// Adds parameters and commands from the specified type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to be used.</typeparam>
	/// <returns></returns>
	public ArgumentsBuilder Use<T>() where T: class, new() => Use(typeof(T));

	/// <summary>
	/// Adds parameters and commands from the specified <paramref name="type"/>.
	/// </summary>
	/// <param name="type">The type to be used.</param>
	/// <returns></returns>
	public ArgumentsBuilder Use(Type type)
	{
		List<ParameterDef> items = GetProperties(type);
		var commands = new List<ParameterDef>();

		foreach (var item in items)
		{
			if (item.CmdAttrib != null || IsInternal(item.PropertyType, type))
			{
				commands.Add(item);
				continue;
			}
			var attr = item.PrmAttrib;
			Parameter(attr?.Name ?? item.PropertyName, attr?.Alias, attr?.ValueName, attr?.Description, IsCollection(item.PropertyType), attr?.Required ?? false);
		}

		foreach (var item in commands)
		{
			var attr = item.CmdAttrib;
			BeginCommand(attr?.Name ?? item.PropertyName, attr?.Alias, attr?.Description);
			Use(item.PropertyType);
			EndCommand();
		}
		return this;

		static bool IsCollection(Type type)
		{
			if (type == typeof(string)) return false;
			if (type.IsArray) return true;
			if (!type.IsGenericType) return false;
			var args = type.GetGenericArguments();
			return args.Length == 1 && type.IsAssignableFrom(typeof(List<>).MakeGenericType(args));
		}

		static bool IsInternal(Type itemType, Type type) => type.Namespace == null ? TopDeclaringType(itemType) == TopDeclaringType(type): type.Namespace == itemType.Namespace;

		static Type? TopDeclaringType(Type type) => type.DeclaringType is null ? type: TopDeclaringType(type.DeclaringType);
	}

	private static List<ParameterDef> GetProperties(Type type)
	{
		return type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
			.Where(o => o.CanWrite && o.GetSetMethod() != null && o.GetIndexParameters().Length == 0)
			.Select(o => new ParameterDef(o.GetCustomAttribute<CliCommandAttribute>(), o.GetCustomAttribute<CliParamAttribute>(), o.Name, o.PropertyType, o.SetValue))
			.Union(type
				.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField)
				.Where(o => !o.IsInitOnly && !o.IsLiteral)
				.Select(o => new ParameterDef(o.GetCustomAttribute<CliCommandAttribute>(), o.GetCustomAttribute<CliParamAttribute>(), o.Name, o.FieldType, o.SetValue))
				).ToList();
	}

	private record struct ParameterDef(CliCommandAttribute? CmdAttrib, CliParamAttribute? PrmAttrib, string PropertyName, Type PropertyType, Action<object, object?> Setter);

	/// <summary>
	/// Parses the specified arguments and returns an <see cref="Arguments"/> instance.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns></returns>
	public Arguments Build(IEnumerable<string> args) => new Arguments(args, this);
}
