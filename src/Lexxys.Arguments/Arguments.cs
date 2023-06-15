// Lexxys Infrastructural library.
// file: Arguments.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable VariableHidesOuterVariable
namespace Lexxys;
using Xml;

/// <summary>
/// Command line arguments parser.
/// </summary>
public class Arguments
{
	private readonly List<string> _args;
	private readonly CommandDefinitionCollection? _commands;

	private CommandDefinition? _command;
	private ParameterDefinitionCollection? _parameters;
	private List<string>? _messages;
	private bool _helpRequested;

	private readonly bool _allowSlash;				// true if slash is a switch prefix
	private readonly bool _strictDdName;			// true if all arguments longer than 1 character must start with --. otherwise - is allowed for any parameter with value.
	private readonly bool _combineOptions;			// true if short options can be combined (i.e. -abc is equivalent to -a -b -c)
	private readonly bool _colonSeparator;			// true if colon is a separator between parameter name and value
	private readonly bool _equalSeparator;			// true if equal sign is a separator between parameter name and value
	private readonly bool _blankSeparator;			// true if blank is a separator between parameter name and value
	private readonly bool _allowUnknown;			// true if unknown parameters are allowed
	private readonly bool _doubleDashSeparator;		// true if double dash is a separator between options and positional parameters
	private readonly bool _ignoreNameSeparators;    // true if delimiters in the parameter name must be trimmed before matching

	private readonly StringComparison _comparison;

	private bool PreferLongDash => _strictDdName || _combineOptions;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="Arguments"/> class.
	/// </summary>
	/// <param name="args">Command line arguments</param>
	/// <exception cref="ArgumentNullException"></exception>
	public Arguments(IEnumerable<string> args)
	{
		if (args is null)
			throw new ArgumentNullException(nameof(args));

		_args = args.ToList();
		_allowSlash = true;
		_strictDdName = false;
		_combineOptions = true;
		_colonSeparator = true;
		_equalSeparator = true;
		_blankSeparator = true;
		_doubleDashSeparator = false;
		_ignoreNameSeparators = true;
		_allowUnknown = true;

		_comparison = StringComparison.OrdinalIgnoreCase;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Arguments"/> class.
	/// </summary>
	/// <param name="args"></param>
	/// <param name="builder"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public Arguments(IEnumerable<string> args, ArgumentsBuilder builder)
	{
		if (args is null)
			throw new ArgumentNullException(nameof(args));

		_args = args.ToList();
		_allowSlash = builder.UseSlashPrefix;
		_strictDdName = builder.HasStrictDoubleDashNameRule;
		_combineOptions = builder.CanCombiningOptions;
		_colonSeparator = builder.UseColonSeparator;
		_equalSeparator = builder.UseEqualSeparator;
		_blankSeparator = builder.UseBlankSeparator;
		_allowUnknown = builder.CollectUnknownParameters;
		_doubleDashSeparator = builder.UseDoubleDashSeparator;
		_ignoreNameSeparators = builder.IgnoreNameSeparators;
		_comparison = builder.Comparison;
		_commands = builder.Commands;

		ParseArguments();
	}

	/// <summary>
	/// Returns all the command line arguments.
	/// </summary>
	public IReadOnlyList<string> Args => _args;

	/// <summary>
	/// Indicates that the help was requested (i.e. -? or -h or --help argument wad found in the command line).
	/// </summary>
	public bool HelpRequested => _helpRequested;

	/// <summary>
	/// Indicates that the command line has errors.
	/// </summary>
	public bool HasErrors => _messages?.Count > 0;

	/// <summary>
	/// Returns the list of the command line errors if any.
	/// </summary>
	public IReadOnlyList<string> Errors => _messages ?? (IReadOnlyList<string>)Array.Empty<string>();

	/// <summary>
	/// Returns the name of the command if detected in the command line.
	/// </summary>
	public string? Command => _command?.Name;

	/// <summary>
	/// Returns the number of matched parameters.
	/// </summary>
	public int Count => _parameters?.Count ?? 0;

	/// <summary>
	/// Enumerates all the matched parameters.
	/// </summary>
	public ParameterDefinitionCollection? Parameters => _parameters;

	/// <summary>
	/// Returns <see cref="ParameterDefinition"/> for the specified <paramref name="name"/> if detected in the command line.
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ParameterDefinition? this[string name] => name is null || _parameters is null ? null : _parameters.FindExact(name);

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	public string? Value(string name) => _parameters is null ? RawValue(name): this[name]?.Value;

	/// <summary>
	/// Checks if the specified flag was found in the command line.  (i.e. -flag or -flag=true ...)
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	public bool Switch(string name) => XmlTools.GetBoolean(Value(name, "true"), false);

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/> as <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Type of the result</typeparam>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	public T? Value<T>(string name) => XmlTools.TryGetValue<T>(Value(name), out var value) ? value: default;

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/> as <typeparamref name="T"/> or <paramref name="defaultValue"/> if the parameter was not found.
	/// </summary>
	/// <typeparam name="T">Type of the result</typeparam>
	/// <param name="name">Name of the parameter</param>
	/// <param name="defaultValue">Default value</param>
	/// <returns></returns>
	public T Value<T>(string name, T defaultValue) => XmlTools.GetValue(Value(name), defaultValue);

	/// <summary>
	/// Returns the array of values of <typeparamref name="T"/> of the parameter with the specified <paramref name="name"/> or an empty array if the parameter was not found.
	/// </summary>
	/// <typeparam name="T">Type of the array element</typeparam>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	public T?[] Collection<T>(string name) => Collection<T?>(name, default);

	/// <summary>
	/// Returns the array of values of <typeparamref name="T"/> of the parameter with the specified <paramref name="name"/> or an empty array if the parameter was not found.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="name">Name of the parameter</param>
	/// <param name="defaultItem">Default value of the array element</param>
	/// <returns></returns>
	public T[] Collection<T>(string name, T defaultItem)
	{
		var parameter = this[name];
		return
			String.IsNullOrEmpty(parameter?.Value) ? Array.Empty<T>():
			parameter.IsCollection ? parameter.Value.Split(',').ConvertAll(o => XmlTools.GetValue(o, defaultItem)):
			new[] { XmlTools.GetValue(parameter.Value, defaultItem) };
	}

	/// <summary>
	/// Returns the array of values of the parameter with the specified <paramref name="name"/> or an empty array if the parameter was not found.
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	public string?[] Collection(string name) => Collection(name, default!);

	/// <summary>
	/// Returns the array of values of the parameter with the specified <paramref name="name"/> or an empty array if the parameter was not found.
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	/// <param name="defaultItem">Default value of the array element</param>
	/// <returns></returns>
	public string[] Collection(string name, string defaultItem)
	{
		var parameter = this[name];
		return
			String.IsNullOrEmpty(parameter?.Value) ? Array.Empty<string>():
			parameter.IsCollection ? parameter.Value.Split(','): new[] { parameter.Value ?? defaultItem };
	}

	/// <summary>
	/// Returns the first positional argument or the <paramref name="defaultValue"/>.
	/// </summary>
	/// <param name="defaultValue">Default value</param>
	/// <returns></returns>
	public string? First(string? defaultValue = null) => Positional.FirstOrDefault() ?? defaultValue;

	/// <summary>
	/// Enumerates all the positional arguments.
	/// </summary>
	public IEnumerable<string> Positional => _parameters == null ? new PositionalArguments(this): _parameters.GetPositionalArguments();

	#region Simple Parsing

	private string? Value(string name, string? empty) => _parameters is null ? RawValue(name, empty): this[name]?.Value;

	private string? RawValue(string argument, string? empty = null)
	{
		if (argument is null)
			throw new ArgumentNullException(nameof(argument));

		var mask = argument.AsSpan();
		var parts = Strings.SplitByCapitals(mask);

		for (int i = 0; i < _args.Count; ++i)
		{
			string arg = _args[i];
			if (arg == null || (arg = arg.Trim()).Length <= 1 || arg[0] != '/' && arg[0] != '-')
				continue;
			arg = arg.Substring(arg.Length > 1 && arg[0] == '-' && arg[1] == '-' ? 2 : 1).TrimStart();
			string? v = empty;
			int k = arg.IndexOfAny(Separators);
			if (k >= 0 && arg.Length > 1)
			{
				v = arg.Substring(k + 1);
				arg = arg.Substring(0, k);
				if (v.Length == 0 && i < _args.Count - 1)
					v = _args[++i];
			}
			if (ParameterDefinition.IsSimilar(arg.AsSpan(), mask, parts, 0, StringComparison.OrdinalIgnoreCase, false))
				return v;
		}
		return null;
	}

	readonly struct PositionalArguments: IEnumerable<string>
	{
		private readonly List<string> _args;

		public PositionalArguments(Arguments args)
		{
			_args = args._args;
		}

		public IEnumerator<string> GetEnumerator()
		{
			for (int i = 0; i < _args.Count; ++i)
			{
				string item = _args[i];
				if (item == null)
					continue;
				string arg = item.Trim();
				if (arg.Length > 1 && (arg[0] == '-' || arg[0] == '/'))
				{
					int k = arg.IndexOfAny(Separators);
					if (k > 0 && k == arg.Length - 1 && i < _args.Count - 1)
						++i;
					continue;
				}
				yield return item;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	private static readonly char[] Separators = new[] { '=', ':' };

	#endregion

	#region Parse

	private void ParseArguments()
	{
		var args = _args;
		var parameters = _commands!.Default.Parameters;

		List<string>? messages = null;
		CommandDefinition? foundCommand = null;
		var foundParameters = new ParameterDefinitionCollection(_comparison);
		bool helpRequested = false;
		bool options = true;
		var unknownParameters = new ParameterDefinitionCollection(_comparison);
		int positionalCount = 0;

		for (int i = 0; i < args.Count; ++i)
		{
			var arg = args[i];
			if (arg is not { Length: > 0 })
				continue;
			if (_doubleDashSeparator && arg == "--")
			{
				options = false;
				continue;
			}
			
			if (options && IsOption(arg))
			{
				helpRequested |= ParseOption(args, ref i, parameters, foundParameters, unknownParameters, ref messages);
			}
			else if (foundCommand is null && (foundCommand = _commands.TryGetCommand(arg)) is not null)
			{
				parameters = foundCommand.Parameters.Combine(parameters);
			}
			else
			{
				var parameter = parameters.GetNextPositionalParameter();
				if (parameter is not null)
					parameter.AddValue(parameter.IsCollection ? GrabCollection(args, ref i, args[i]) : args[i]);
				else if (_allowUnknown)
					unknownParameters.Add(new ParameterDefinition($"${++positionalCount}", ParameterValueType.Optional | ParameterValueType.Required, positional: true).WithValue(arg));
				else
					(messages ??= new List<string>()).Add($"Unknown argument: {args[i]}");
			}
		}

		if (parameters.GetMissingParameters(true).Any())
		{
			var pp = parameters.GetMissingParameters(true)
				.Select(o => o.GetParameterName(longDash: PreferLongDash, excludeAbbreviation: true)).ToList();
			(messages ??= new List<string>()).Add($"Missing required parameter{(pp.Count > 1 ? "s": "")}: {String.Join(", ", pp)}");
		}

		_messages = messages;
		_command = foundCommand;
		_parameters = unknownParameters.Count == 0 ? foundParameters: foundParameters.Combine(unknownParameters);
		_helpRequested = helpRequested;
	}

	private bool ParseOption(List<string> args, ref int i, ParameterDefinitionCollection parameters, ParameterDefinitionCollection foundParameters, ParameterDefinitionCollection unknownParameters, ref List<string>? messages)
	{
		string arg = args[i];
		var (dash, doubleDash) = (false, false);
		if (arg[0] == '-')
			if (arg.Length > 1 && arg[1] == '-')
				doubleDash = true;
			else
				dash = true;
		var (name, value) = SplitNameValue(doubleDash ? arg.Substring(2) : arg.Substring(1));
		if (name.Length == 0)
		{
			(messages ??= new List<string>()).Add($"Invalid parameter: {arg}");
			return false;
		}

		switch (dash)
		{
			case true when _combineOptions && value == null:
				CollectFlags(name, arg, parameters, foundParameters, ref messages);
				return false;
			// TODO: Do we need to support this?
			case true when _strictDdName && name.Length > 1:
				(messages ??= new List<string>()).Add($"Invalid parameter: {arg}");
				return false;
		}

		var parameter = FindParameter(parameters, name, arg, ref messages);
		if (parameter is null)
		{
			if (name is "?" or "h" or "help")
				return true;
			if (_allowUnknown)
				unknownParameters.Add(new ParameterDefinition(name, ParameterValueType.Optional | ParameterValueType.Required).WithValue(value));
			else
				(messages ??= new List<string>()).Add($"Unknown parameter: {arg}");
			return false;
		}

		if (value is null && parameter.IsSwitch)
		{
			foundParameters.Add(parameter.WithValue("true"));
			return parameter.Name == "?" || String.Equals(parameter.Name, "help", foundParameters.Comparison);
		}

		if (value is null && _blankSeparator && i < args.Count - 1 && !IsOption(args[i + 1]))
			value = args[++i];

		if (value is null && parameter.IsValueRequired)
		{
			(messages ??= new List<string>()).Add($"Missing value for parameter: {arg}");
			return false;
		}

		if (value is not null && parameter.IsCollection)
			value = GrabCollection(args, ref i, value);

		if (parameter.Value is not null)
		{
			if (parameter.IsCollection)
				parameter.AddValue(value);
			else
				(messages ??= new List<string>()).Add($"Duplicate parameter: {arg}");
			return false;
		}

		foundParameters.Add(parameter.WithValue(value));
		return false;
	}

	private bool IsOption(string name) => name.Length > 0 && (name[0] == '-' || (_allowSlash && name[0] == '/'));

	private ParameterDefinition? FindParameter(ParameterDefinitionCollection parameters, string name, string arg, ref List<string>? messages)
	{
		var parameter = parameters.FindExact(name);
		if (parameter is not null)
			return parameter;

		var pp = parameters.FindSimilar(name, _ignoreNameSeparators);
		if (pp.Count > 1)
			(messages ??= new List<string>()).Add($"Ambiguous parameter: {arg}");
		else if (pp.Count == 1)
			parameter = pp[0];

		return parameter;
	}

	private (string Name, string? Value) SplitNameValue(string arg)
	{
		int j = _equalSeparator ?
			_colonSeparator ? arg.IndexOfAny(Separators) : arg.IndexOf('=') :
			_colonSeparator ? arg.IndexOf(':') : -1;
		return j < 0 ? (arg, null): (arg.Substring(0, j), arg.Substring(j + 1));
	}

	private string GrabCollection(List<string> args, ref int i, string value)
	{
		while (value.EndsWith(",", StringComparison.Ordinal) && i < args.Count - 1 && !IsOption(args[i + 1]))
		{
			value += args[++i];
		}
		return value;
	}

	private static void CollectFlags(string name, string arg, ParameterDefinitionCollection parameters, ParameterDefinitionCollection foundParameters, ref List<string>? messages)
	{
		foreach (var c in name)
		{
			var f = c.ToString();
			var p = parameters.FindExact(c.ToString()); //.FirstOrDefault(o => o.IsSwitch && o.IsMatch(f, comparison));
			if (p is not null)
				foundParameters.Add(p.WithValue("true"));
			else
				(messages ??= new List<string>()).Add($"Invalid flag {f} in parameter: {arg}");
		}
	}
	#endregion

	#region Usage

	const int Indent = 2;
	const int MaxOffset = 24;

	/// <summary>
	/// Prints the usage string.
	/// </summary>
	/// <param name="application">Name of the application.</param>
	/// <param name="details">Prints the details of each command parameter, if any.</param>
	/// <param name="brief">Prints the command line only without the parameter details.</param>
	/// <param name="writer">An optional <see cref="TextWriter"/> (by default - <see cref="Console.Out"/>)</param>
	public void Usage(string application, bool details = false, bool brief = false, TextWriter? writer = null)
	{
		writer ??= Console.Out;
		UsageString(application, _command?.Name, writer);
		if (_commands is null || brief)
			return;

		writer.WriteLine();

		if (_command?.Name is null)
			UsageDetails(details, writer);
		else
			UsageCommand(writer);
		return;

		void UsageDetails(bool details, TextWriter writer)
		{
			var offset = GetOffset();
			if (_commands.Count > 1)
			{
				bool nl = false;
				foreach (var item in _commands.OrderBy(o => o.Name))
				{
					if (item.Name.Length == 0)
						continue;
					if (nl && details)
						writer.WriteLine();
					UsageCommandOnly(writer, item, offset, details);
					nl = true;
				}
			}

			UsageParametersOnly(writer, _commands.Default.Parameters, Indent, offset, _commands.Count > 1);
		}

		void UsageCommand(TextWriter writer)
		{
			var offset = GetOffset();
			UsageCommandOnly(writer, _command!, offset, true);
			UsageParametersOnly(writer, _commands.Default.Parameters, Indent, offset, true);
		}
	}

	private int GetOffset()
	{
		var offset = 1 + _commands!.SelectMany(o => o.Parameters).Max(o => o.GetParameterName(argumentDelimiter: '=').Length);
		int cmdOffset = 2 + _commands!.Max(o => o.Name.Length + 1);
		if (offset < cmdOffset)
			offset = cmdOffset;
		offset = Math.Min(MaxOffset, offset);
		return offset;
	}

	private void UsageCommandOnly(TextWriter writer, CommandDefinition cmd, int offset, bool details)
	{
		writer.Write(new String(' ', Indent));
		writer.Write(cmd.Name);
		WriteDescription(writer, cmd.Description, offset - cmd.Name.Length, Indent + offset);

		if (details && cmd.Parameters.Count > 0)
			UsageParametersOnly(writer, cmd.Parameters, Indent + Indent, offset, false);
	}

	private void UsageString(string program, string? commandName, TextWriter writer)
	{
		writer.Write($"Usage: {program}");
		if (_commands is null)
		{
			writer.WriteLine(" [<options>]");
			return;
		}

		UsageString(_commands.Default.Parameters, writer);
		CommandDefinition? cmd;
		if (!String.IsNullOrEmpty(commandName) && (cmd = _commands.TryGetCommand(commandName)) is not null)
		{
			writer.Write(' ');
			writer.Write(cmd.Name);
			UsageString(cmd.Parameters, writer);
		}
		else if (_commands.Count > 1)
		{
			writer.Write(" <command> [<arguments>]");
		}
		writer.WriteLine();
	}

	private void UsageString(ParameterDefinitionCollection parameters, TextWriter writer)
	{
		foreach (var item in parameters.Where(o => !o.IsPositional))
		{
			writer.Write(' ');
			string name = item.GetParameterName(argumentDelimiter: '=', longDash: PreferLongDash, excludeAbbreviation: true);
			writer.Write(item.IsRequired ? name : $"[{name}]");
		}
		foreach (var item in parameters.Where(o => o.IsPositional))
		{
			writer.Write(' ');
			string name = item.ValueName ?? item.Name;
			writer.Write(item.IsRequired ? $"<{name}>" : $"[{name}]");
			if (item.IsCollection)
				writer.Write("...");
		}
	}

	private void UsageParametersOnly(TextWriter writer, ParameterDefinitionCollection parameters, int indent, int offset, bool newLine)
	{
		var pad = new String(' ', indent);
		offset -= indent - Indent;
		bool abbrev = parameters.Any(o => o.Abbreviations is { Length: >0 });
		if (parameters.FindExact("?") is null && parameters.FindExact("h") is null && parameters.FindExact("help") is null)
		{
			if (newLine)
			{
				writer.WriteLine();
				newLine = false;
			}
			writer.Write(pad);
			writer.Write("-h, --help");
			WriteDescription(writer, "show help and exit", offset - 10, indent + offset);
		}
			

		foreach (var item in parameters.Where(o => !o.IsPositional))
		{
			if (newLine)
			{
				writer.WriteLine();
				newLine = false;
			}
			writer.Write(pad);
			var name = item.GetParameterName(argumentDelimiter: '=', longDash: PreferLongDash);
			var space = offset - name.Length;
			if (abbrev && item.Name.Length > 1 && item.Abbreviations is not { Length: >0 })
			{
				space -= 4;
				writer.Write("    ");
			}
			writer.Write(name);
			WriteDescription(writer, item.Description, space, indent + offset);
		}
	}

	private static void WriteDescription(TextWriter writer, string? description, int indent, int offset)
	{
		if (description == null)
		{
			writer.WriteLine();
			return;
		}	
		var lines = description.Split(NewLine, StringSplitOptions.RemoveEmptyEntries);
		if (indent < 2)
		{
			writer.WriteLine();
			indent = offset;
		}
		foreach (var line in lines)
		{
			writer.Write(new String(' ', indent));
			writer.WriteLine(line);
			indent = offset;
		}
	}
	private static readonly char[] NewLine = new[] { '\r', '\n' };

	#endregion
}
