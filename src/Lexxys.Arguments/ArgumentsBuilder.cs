namespace Lexxys;

/// <summary>
/// Builder for <see cref="Arguments"/> class.
/// </summary>
public class ArgumentsBuilder
{
	private CommandDefinition _current;

	/// <summary>
	/// Creates a new instance of the <see cref="ArgumentsBuilder"/> class.
	/// </summary>
	/// <param name="config">Parser behavior settings, or <c>null</c> to use defaults.</param>
	public ArgumentsBuilder(ArgumentsConfig? config = null)
	{
		config ??= ArgumentsConfig.Default;
		Root = new CommandDefinition(null, String.Empty, config: config);
		Config = config;

		_current = Root;
	}

	/// <summary>
	/// Gets the root command definition.
	/// </summary>
	internal CommandDefinition Root { get; }

	public ArgumentsConfig Config { get; }

	/// <summary>
	/// Specifies a new or an existing command to which parameters will be added.
	/// </summary>
	/// <param name="name">Name of the command.</param>
	/// <param name="description">Description for the new command</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
	public ArgumentsBuilder BeginCommand(string name, string? description = null)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));
		_current = new CommandDefinition(_current, name, description: description, config: _current.Config);
		return this;
	}

	/// <summary>
	/// Specifies a new or an existing command to which parameters will be added.
	/// </summary>
	/// <param name="name">Name of the command.</param>
	/// <param name="abbreviation">Alternative command names.</param>
	/// <param name="description">Description for the new command</param>
	/// <returns>The <see cref="ArgumentsBuilder"/></returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
	public ArgumentsBuilder BeginCommand(string name, string[]? abbreviation, string? description = null)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));
		_current = new CommandDefinition(_current, name, abbreviation, description, config: _current.Config);
		return this;
	}

	/// <summary>
	/// Ends the current command and returns to the parent command.
	/// </summary>
	/// <returns>The <see cref="ArgumentsBuilder"/>.</returns>
	/// <exception cref="InvalidOperationException">There is no current child command to end.</exception>
	public ArgumentsBuilder EndCommand()
	{
		_current = _current.Parent ?? throw new InvalidOperationException("There is no command to end.");
		return this;
	}

	/// <summary>
	/// Adds a command with the specified name and description to the current command.
	/// </summary>
	/// <param name="name">Name of the command.</param>
	/// <param name="description">Description for the command.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/>.</returns>
	public ArgumentsBuilder Command(string name, string? description = null) => BeginCommand(name, description).EndCommand();

	/// <summary>
	/// Adds a command with the specified name, abbreviation, and description to the current command.
	/// </summary>
	/// <param name="name">Name of the command.</param>
	/// <param name="abbreviation">Alternative command names.</param>
	/// <param name="description">Description for the command.</param>
	/// <returns>The <see cref="ArgumentsBuilder"/>.</returns>
	public ArgumentsBuilder Command(string name, string[]? abbreviation, string? description = null) => BeginCommand(name, abbreviation, description).EndCommand();

#if NET

	/// <summary>
	/// Adds a generated option model as a subcommand.
	/// </summary>
	/// <typeparam name="T">The generated option model type for the command.</typeparam>
	/// <param name="builder">The builder to extend.</param>
	/// <param name="name">The command name.</param>
	/// <param name="abbreviation">Alternative command names.</param>
	/// <param name="description">Text shown for the command in usage output.</param>
	/// <returns>The configured argument builder.</returns>
	public ArgumentsBuilder Command<T>(string name, string[]? abbreviation, string? description = null) where T: ICliOptionBase<T>
		=> T.CreateBuilder(BeginCommand(name, abbreviation, description)).EndCommand();

	/// <summary>
	/// Adds a generated option model as a subcommand.
	/// </summary>
	/// <typeparam name="T">The generated option model type for the command.</typeparam>
	/// <param name="builder">The builder to extend.</param>
	/// <param name="name">The command name.</param>
	/// <param name="description">Text shown for the command in usage output.</param>
	/// <returns>The configured argument builder.</returns>
	public ArgumentsBuilder Command<T>(string name, string? description = null) where T: ICliOptionBase<T>
		=> T.CreateBuilder(BeginCommand(name, description)).EndCommand();

#endif

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
		_current.Add(new ParameterDefinition(_current, name, abbrev == null ? null: [abbrev], valueName, description: description, collection: collection, required: required));
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
		_current.Add(new ParameterDefinition(_current, name, abbrev == null ? null: [abbrev], description: description, toggle: true));
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
		List<ParameterInfo> items = Arguments.GetProperties(type, Config.NamingStyle);
		var commands = new List<ParameterInfo>();

		foreach (var item in items)
		{
			if (item.Cmd != null || IsInternal(item.Type, type))
			{
				commands.Add(item);
				continue;
			}
			var attr = item.Prm;
			var name = attr?.Name ?? Arguments.FormatArgumentName(item.Name, Config.NamingStyle);
			if (attr?.Positional == true)
				Positional(name, attr.Description, attr.ValueName, IsCollection(item.Type), attr.Required);
			else if (item.Type == typeof(bool) || item.Type == typeof(bool?))
				Switch(name, attr?.Alias ?? [], attr?.Description);
			else
				Parameter(name, attr?.Alias, attr?.ValueName, attr?.Description, IsCollection(item.Type), attr?.Required ?? false);
		}

		foreach (var item in commands)
		{
			var attr = item.Cmd;
			BeginCommand(attr?.Name ?? Arguments.FormatArgumentName(item.Name, Config.NamingStyle), attr?.Alias, attr?.Description);
			Use(item.Type);
			EndCommand();
		}
		return this;

		static bool IsCollection(Type type)
		{
			if (type == typeof(string)) return false;
			if (type.IsArray) return true;
			if (!type.IsGenericType) return false;
			var args = type.GetGenericArguments();
			if (args.Length != 1) return false;
			var list = typeof(List<>).MakeGenericType(args);
			return type.IsAssignableFrom(list) || list.IsAssignableFrom(type);
		}

		static bool IsInternal(Type itemType, Type type) => type.Namespace == null ? TopDeclaringType(itemType) == TopDeclaringType(type): type.Namespace == itemType.Namespace;

		static Type? TopDeclaringType(Type type) => type.DeclaringType is null ? type: TopDeclaringType(type.DeclaringType);
	}

	///// <summary>
	///// Parses the specified arguments and returns an <see cref="Arguments"/> instance.
	///// </summary>
	///// <param name="args">Command line arguments.</param>
	///// <returns></returns>
	public Arguments Parse(IEnumerable<string> args) => Arguments.Parse(args, this);
}
