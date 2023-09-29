// Lexxys Infrastructural library.
// file: Arguments.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using System.Text;

namespace Lexxys;


/// <summary>
/// Command line arguments parser.
/// </summary>
public class Arguments: IDumpJson, IDumpXml
{
	private readonly bool _allowSlash;				// true if slash is a switch prefix
	private readonly bool _strictDoubleDash;		// true if all arguments longer than 1 character must start with --. otherwise - is allowed for any parameter with value.
	private readonly bool _combineOptions;			// true if short options can be combined (i.e. -abc is equivalent to -a -b -c)
	private readonly bool _colonSeparator;			// true if colon is a separator between parameter name and value
	private readonly bool _equalSeparator;			// true if equal sign is a separator between parameter name and value
	private readonly bool _blankSeparator;			// true if blank is a separator between parameter name and value
	private readonly bool _allowUnknown;			// true if unknown parameters are allowed
	private readonly bool _doubleDashSeparator;		// true if double dash is a separator between options and positional parameters
	private readonly bool _ignoreNameSeparators;    // true if delimiters in the parameter name must be trimmed before matching
	private readonly bool _dynamic;
	private readonly bool _splitPositional;

	private readonly StringComparison _comparison;
	private readonly bool _helpRequested;
	private readonly List<string> _messages;
	private readonly List<ArgumentCommand> _commands;
	private readonly List<string> _args;

	private bool PreferLongDash => _strictDoubleDash || _combineOptions;

	/// <summary>
	/// Creates new instance of <see cref="Arguments"/> class for dynamic parsing with default options:<br/>
	/// - Don't combine short options (i.e., -abc is not equivalent to -a -b -c).<br/>
	/// - Blank is not allowed as a separator between the argument name and value.<br/>
	/// - Consider delimiters ('-', '_', and '.') in the parameter name while matching.<br/>
	/// </summary>
	/// <param name="args">Command line arguments</param>
	/// <param name="ignoreCase">Specifies whether the command and parameter names are case-insensitive.</param>
	/// <param name="allowSlash">True if slash is a switch prefix.</param>
	/// <param name="colonSeparator">True if colon is a separator between parameter name and value.</param>
	/// <param name="equalSeparator">True if equal sign is a separator between parameter name and value.</param>
	/// <param name="doubleDashSeparator">True if double dash is a separator between options and positional parameters.</param>
	/// <param name="splitPositional">True if the all positional parameters shouldn't be combined.</param>
	/// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c></exception>
	public Arguments(IEnumerable<string> args, bool ignoreCase = false, bool allowSlash = true, bool colonSeparator = true, bool equalSeparator = true, bool doubleDashSeparator = true, bool splitPositional = false)
	{
		if (args is null)
			throw new ArgumentNullException(nameof(args));

		_args = args.ToList();
		_allowSlash = allowSlash;
		_strictDoubleDash = false;
		_combineOptions = false;
		_colonSeparator = colonSeparator;
		_equalSeparator = equalSeparator;
		_blankSeparator = false;
		_doubleDashSeparator = doubleDashSeparator;
		_ignoreNameSeparators = true;
		_allowUnknown = true;
		_dynamic = true;
		_comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal;
		_splitPositional = splitPositional;

		var (command, messages, helpRequested) = ParseAutoArguments();
		_commands = new List<ArgumentCommand> { command };
		_messages = messages;
		_helpRequested = helpRequested;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Arguments"/> class.
	/// </summary>
	/// <param name="args">Command line arguments</param>
	/// <param name="builder">Arguments definition</param>
	/// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c></exception>
	/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c></exception>
	public Arguments(IEnumerable<string> args, ArgumentsBuilder builder)
	{
		if (args is null)
			throw new ArgumentNullException(nameof(args));
		if (builder is null)
			throw new ArgumentNullException(nameof(builder));

		_args = args.ToList();
		_allowSlash = builder.UseSlashPrefix;
		_strictDoubleDash = builder.HasStrictDoubleDashNameRule;
		_combineOptions = builder.CanCombiningOptions;
		_colonSeparator = builder.UseColonSeparator;
		_equalSeparator = builder.UseEqualSeparator;
		_blankSeparator = builder.UseBlankSeparator;
		_allowUnknown = builder.CollectUnknownParameters;
		_doubleDashSeparator = builder.UseDoubleDashSeparator;
		_ignoreNameSeparators = builder.IgnoreNameSeparators;
		_splitPositional = builder.SplitPositional;
		_comparison = builder.Root.Comparison;

		(_commands, _messages, _helpRequested) = ParseArguments(builder.Root);
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
	public bool HasErrors => _messages.Count > 0;

	/// <summary>
	/// Returns the list of the command line errors if any.
	/// </summary>
	public IReadOnlyList<string> Errors => _messages;

	/// <summary>
	/// Returns the root (not named) command.
	/// </summary>
	public CommandDefinition CommandInfo => _commands[_commands.Count - 1].Definition;

	public ArgumentParameterCollection Parameters => _commands[_commands.Count - 1].Parameters;

	public ArgumentCommand? Command => _commands[_commands.Count - 1].Command;

	/// <summary>
	/// Returns the list of the selected commands if any.
	/// </summary>
	/// <param name="includeRootCommand">True if the root command must be included in the list.</param>
	/// <returns></returns>
	public IList<CommandDefinition> GetSelectedCommands(bool includeRootCommand = false)
	{
		return includeRootCommand ? _commands.ConvertAll(o => o.Definition):
			_commands.Count == 1 ? Array.Empty<CommandDefinition>():
			_commands.Count == 2 ? new [] { _commands[1].Definition }:
			_commands.Skip(1).Select(c => c.Definition).ToList();
	}

	/// <summary>
	/// Returns the number of matched parameters.
	/// </summary>
	public int Count => _commands[_commands.Count - 1].Parameters.Count;

	/// <summary>
	/// Returns first <see cref="ParameterDefinition"/> for the specified <paramref name="name"/> if detected in the command line.
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ParameterValue this[string name] => _commands[_commands.Count - 1].Parameters.TryGet(name, out var p, _dynamic, _ignoreNameSeparators) ? p!.Value: default;

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/> or <c>null</c> if the parameter not found.
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	public string? Value(string name) => this[name].StringValue;

	/// <summary>
	/// Checks if the specified flag was found in the command line.  (i.e. -flag or -flag=true ...)
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	public bool Switch(string name) => Strings.GetBoolean(Value(name), false);

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/> as <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Type of the result</typeparam>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	public T? Value<T>(string name) => Strings.TryGetValue<T>(Value(name), out var value) ? value: default;

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/> as <typeparamref name="T"/> or <paramref name="defaultValue"/> if the parameter was not found.
	/// </summary>
	/// <typeparam name="T">Type of the result</typeparam>
	/// <param name="name">Name of the parameter</param>
	/// <param name="defaultValue">Default value</param>
	/// <returns></returns>
	public T Value<T>(string name, T defaultValue) => Strings.GetValue(Value(name), defaultValue);

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
		=> this[name].ArrayValue?.ConvertAll(o => Strings.GetValue(o, defaultItem)) ?? Array.Empty<T>();

	/// <summary>
	/// Returns the array of values of the parameter with the specified <paramref name="name"/> or an empty array if the parameter was not found.
	/// </summary>
	/// <param name="name">Name of the parameter</param>
	/// <returns></returns>
	public string[] Collection(string name) => this[name].ArrayValue ?? Array.Empty<string>();

	/// <summary>
	/// Enumerates all the positional arguments.
	/// </summary>
	public IEnumerable<ParameterValue> Positional => _commands[_commands.Count - 1].Parameters.Where(o => o.Definition.IsPositional).Select(o => o.Value);

	string IDumpXml.XmlElementName => "args";

	/// <summary>
	/// Converts the command line parameters to JSON.
	/// </summary>
	/// <param name="json"><see cref="JsonBuilder"/></param>
	/// <returns></returns>
	public JsonBuilder ToJsonContent(JsonBuilder json)
	{
		AddCommand(json, _commands[0]);
		return json;

		static void AddCommand(JsonBuilder json, ArgumentCommand cmd)
		{
			foreach (var item in cmd.Parameters)
			{
				if (item.Definition.IsCollection)
					json.Item(item.Name).Val(item.Value.ArrayValue);
				else
					json.Item(item.Name).Val(item.Value.StringValue);
			}
			if (cmd.Command != null)
			{
				json.Item(cmd.Command.Name).Obj();
				AddCommand(json, cmd.Command);
				json.End();
			}
		}
	}

	/// <summary>
	/// Converts the command line parameters to XML.
	/// </summary>
	/// <param name="xml"><see cref="XmlBuilder"/></param>
	/// <returns></returns>
	public XmlBuilder ToXmlContent(XmlBuilder xml)
	{
		AddCommand(xml, _commands[0]);
		return xml;

		static void AddCommand(XmlBuilder xml, ArgumentCommand cmd)
		{
			foreach (var item in cmd.Parameters.Where(o => !o.Definition.IsCollection))
			{
				xml.Item(item.Name, item.Value.StringValue!);
			}
			foreach (var item in cmd.Parameters.Where(o => o.Definition.IsCollection))
			{
				xml.ToInnerElements();
				foreach (var value in item.Value.ArrayValue!)
					xml.Item(item.Name, value);
			}
			if (cmd.Command != null)
			{
				xml.Element(cmd.Command!.Name);
				AddCommand(xml, cmd.Command);
				xml.End();
			}
		}
	}

	#region Usage

	const int Indent = 2;
	const int MaxOffset = 32;

    /// <summary>
    /// Prints the usage string.
    /// </summary>
    /// <param name="application">Name of the application.</param>
    /// <param name="brief">Do not include commands details.</param>
    /// <param name="width">Width of the output. (0 - screen width or 80; &lt;0 - no limit)</param>
    /// <param name="alignAbbreviation">Align parameters names with and without abbreviations</param>
    /// <param name="writer">An optional <see cref="TextWriter"/> (by default - <see cref="Console.Out"/>)</param>
    public void Usage(string? application = null, bool brief = false, int width = 0, bool alignAbbreviation = false, TextWriter? writer = null)
	{
		if (writer == null)
		{
			writer = Console.Out;
			if (width == 0)
				width = Console.IsOutputRedirected ? int.MaxValue: Console.WindowWidth;
		}

		var text = UsageString(application, _commands[_commands.Count - 1].Definition, width, new StringBuilder("Usage: "));
		if (_dynamic)
		{
			writer.WriteLine(text);
			return;
		}

		text.AppendLine();

		if (_commands.Count == 1)
			UsageDetails(brief, width, alignAbbreviation, text);
		else
			UsageCommand(_commands[_commands.Count - 1].Definition, brief, width, alignAbbreviation, text);

		writer.Write(text);
	}

    /// <summary>
    /// Builds usage parameters and commands details.
    /// </summary>
    /// <param name="brief">Do not include command details</param>
    /// <param name="width">Width of the output. (0 - screen width or 80; &lt;0 - no limit)</param>
    /// <param name="alignAbbreviation">Align parameters names with and without abbreviations</param>
    /// <param name="text">The <see cref="StringBuilder"/></param>
    /// <returns></returns>
    public StringBuilder UsageDetails(bool brief = false, int width = 0, bool alignAbbreviation = false, StringBuilder? text = null)
	{
		text ??= new StringBuilder();
		width = FixWidth(width);
		var offset = GetOffset(Indent, width);
		if (_commands[0].Definition.Parameters.HasNamedParameters)
			text.AppendLine().AppendLine("Parameters:");
		UsageParameters(text, _commands[0].Definition.Parameters, Indent, offset, width, alignAbbreviation);
		if (!_commands[0].Definition.HasCommands)
			return text;

		text.AppendLine().AppendLine("Commands:");
		bool nl = false;
		foreach (var item in _commands[0].Definition.Commands!)
		{
			if (item.Name.Length == 0)
				continue;
			if (nl && !brief)
				text.AppendLine();
			UsageCommand(text, item, Indent, offset, brief, true, width, alignAbbreviation);
			nl = true;
		}
		return text;
	}

    /// <summary>
    /// Builds usage details text for the parameters and the specified <paramref name="command"/>.
    /// </summary>
    /// <param name="command">The <see cref="CommandDefinition"/> to print.</param>
    /// <param name="brief">Do not include command details</param>
    /// <param name="width">Width of the output. (0 - screen width or 80; &lt;0 - no limit)</param>
    /// <param name="alignAbbreviation">Align parameters names with and without abbreviations</param>
    /// <param name="text">The <see cref="StringBuilder"/></param>
    /// <returns></returns>
    public StringBuilder UsageCommand(CommandDefinition command, bool brief = false, int width = 0, bool alignAbbreviation = false, StringBuilder? text = null)
	{
		text ??= new StringBuilder();
		width = FixWidth(width);
		var offset = GetOffset(Indent, width);
		if (_commands[0].Definition.Parameters.HasNamedParameters)
			text.AppendLine().AppendLine("Parameters:");
		UsageParameters(text, _commands[0].Definition.Parameters, Indent, offset, width, alignAbbreviation);
		var indent = Indent;
		if (command.Parent == null)
			return text;

		text.AppendLine().AppendLine("Commands:");
		foreach (var cmd in GetCommands(command.Parent))
		{
			UsageCommand(text, cmd, indent, offset, brief, false, width, alignAbbreviation);
			indent += Indent;
		}
		UsageCommand(text, command, indent, offset, brief, false, width, alignAbbreviation);
		return text;
	}

	/// <summary>
	/// Generates the usage string including the specified <paramref name="command"/> if present.
	/// </summary>
	/// <param name="application">Name of the application</param>
	/// <param name="command">Selected command</param>
	/// <param name="width">Width of the output. (0 - screen width or 80; &lt;0 - no limit)</param>
	/// <param name="text">The <see cref="StringBuilder"/></param>
	/// <returns></returns>
	public StringBuilder UsageString(string? application = null, CommandDefinition? command = null, int width = 0, StringBuilder? text = null)
	{
		application ??= Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
		text ??= new StringBuilder();
		text.Append(application);
		if (_dynamic)
			return text.Append(" [<options>]");

		width = FixWidth(width);

		var patch = new List<(int Index, int Length, string Value)>();
		int k0 = text.Length;
		int k = text.Length;
		UsageNamedParameters(text, _commands[0].Definition.Parameters);
		int n = text.Length - k;
		if (n > 12)
			patch.Add((k - k0, n, " [<options>]"));
		k = text.Length;
		Arguments.UsagePositionalParameters(text, _commands[0].Definition.Parameters);
		n = text.Length - k;
		if (n > 0)
			patch.Add((k - k0, n, ""));

		if (command?.Parent == null)
		{
			if (_commands[0].Definition.HasCommands)
				text.Append(" <command> [<arguments>]");
		}
		else
		{
			foreach (var cmd in GetCommands(command))
			{
				text.Append(' ').Append(cmd.Name);
				if (cmd.HasParameters)
				{
					k = text.Length;
					UsageNamedParameters(text, cmd.Parameters);
					n = text.Length - k;
					if (n > 13)
						patch.Add((k - k0, n, " [<arguments>]"));

					Arguments.UsagePositionalParameters(text, cmd.Parameters);
					n = text.Length - k;
					if (n > 0)
						patch.Add((k - k0, n, ""));
				}
			}
		}

		if (text.Length <= width)
			return text;

		var line = new char[text.Length];
		text.CopyTo(k0, line, 0, text.Length - k0);
		text.Length = k0;
		k = 0;
		foreach (var (index, length, value) in patch)
		{
			text.Append(line, k, index - k);
			text.Append(value);
			k = index + length;
			if (text.Length + line.Length - k <= width)
				break;
		}
		text.Append(line, k, line.Length - k);
		return text;
	}

	private static int FixWidth(int width) => width >= 60 ? width: width == 0 ? 80: width < 0 ? int.MaxValue: 60;

	private static List<CommandDefinition> GetCommands(CommandDefinition command)
	{
		var commands = new List<CommandDefinition>();
		while (command.Parent != null)
		{
			commands.Add(command);
			command = command.Parent;
		}
		commands.Reverse();
		return commands;
	}

	private int GetOffset(int indent, int maxWidth, bool padAttributes = false)
	{
		var offset = 1 + GetOffset(indent, _commands[0].Definition);
		if (offset > maxWidth - 12)
			offset = Math.Max(0, maxWidth - 12);
		if (offset > MaxOffset)
			offset = MaxOffset;
		return offset;
	}

	private static int GetOffset(int indent, CommandDefinition command)
	{
		var sb = new StringBuilder();
		int offset = Indent + command.Parameters.Max(o =>
		{
			sb.Clear();
			return (int?)o.GetParameterName(sb, argumentDelimiter: '=').Length;
		}) ?? 0;
		offset = indent + Math.Max(offset, command.Name.Length);
		return Math.Max(offset, command.Commands?.Max(o => (int?)GetOffset(indent + Indent, o)) ?? 0);
	}

	private void UsageCommand(StringBuilder text, CommandDefinition cmd, int indent, int offset, bool brief, bool commands, int maxWidth, bool alignAbbreviation)
	{
		text.Append(' ', indent).Append(cmd.Name);
		AddDescription(text, cmd.Description, offset - indent - cmd.Name.Length, offset, maxWidth);

		if (brief)
			return;

		if (cmd.Parameters.Count > 0)
			UsageParameters(text, cmd.Parameters, indent + Indent, offset, maxWidth, alignAbbreviation);

		if (!commands || !cmd.HasCommands)
			return;

		foreach (var item in cmd.Commands!)
		{
			if (item.Name.Length == 0)
				continue;
			UsageCommand(text, item, indent + Indent, offset, brief, commands, maxWidth, alignAbbreviation);
		}
	}

	private void UsageNamedParameters(StringBuilder text, ParameterDefinitionCollection parameters)
	{
		foreach (var item in parameters.Where(o => !o.IsPositional))
		{
			text.Append(' ');
			if (!item.IsRequired)
				text.Append('[');
			item.GetParameterName(text, argumentDelimiter: '=', longDash: PreferLongDash, excludeAbbreviation: true);
			if (!item.IsRequired)
				text.Append(']');
		}
	}

	private static void UsagePositionalParameters(StringBuilder text, ParameterDefinitionCollection parameters)
	{
		foreach (var item in parameters.Where(o => o.IsPositional))
		{
			text.Append(' ')
				.Append(item.IsRequired ? '<' : '[')
				.Append(item.ValueName ?? item.Name)
				.Append(item.IsRequired ? '>' : ']');
			if (item.IsCollection)
				text.Append("...");
		}
	}

	private void UsageParameters(StringBuilder text, ParameterDefinitionCollection parameters, int indent, int offset, int width, bool alignAbbreviation)
	{
		if (!parameters.HasNamedParameters)
			return;

		bool abbrev = alignAbbreviation && parameters.Any(o => o.HasAbbreviation);
		foreach (var item in parameters.Where(o => !o.IsPositional))
		{
			AddParameterInfo(text, item, indent, offset, abbrev, width);
		}
	}

	private void AddParameterInfo(StringBuilder text, ParameterDefinition parameter, int indent, int offset, bool abbrev, int width)
	{
		int n0 = text.Length;
		text.Append(' ', indent);

		if (abbrev && parameter.Name.Length > 1 && !parameter.HasAbbreviation)
			text.Append(' ', 4);

		parameter.GetParameterName(text, argumentDelimiter: '=', longDash: PreferLongDash);
		int pad = offset - (text.Length - n0);
		AddDescription(text, parameter.Description, pad, offset, width);
	}

	private static void AddDescription(StringBuilder text, string? description, int pad, int offset, int width)
	{
		if (description == null)
		{
			text.AppendLine();
			return;
		}
		var lines = description.Split(NewLine, StringSplitOptions.RemoveEmptyEntries);
		if (pad < 2)
		{
			text.AppendLine();
			pad = offset;
		}
		foreach (var line in lines)
		{
			if (offset + line.Length <= width)
			{
				text.Append(' ', pad).AppendLine(line);
				pad = offset;
				continue;
			}

			var items = Strings.SplitByWordBound(line, width - offset);
			var span = line.AsSpan();
			foreach ((int index, int length) in items)
			{
				text.Append(' ', pad).Append(span.Slice(index, length)).AppendLine();
				pad = offset;
			}
		}
	}
	private static readonly char[] NewLine = new[] { '\r', '\n' };

	#endregion

	#region Parse

	private (ArgumentCommand, List<string>, bool) ParseAutoArguments()
	{
		var command = new ArgumentCommand(new CommandDefinition(null, String.Empty, comparison: _comparison));
		var parameters = command.Parameters;
		List<string> positional = new List<string>();
		var messages = new List<string>();
		bool positionalOnly = false;
		bool helpRequested = false;
		var args = _args;
		int positionalIndex = -1;
		for (int i=0; i < args.Count; ++i)
		{
			var arg = args[i];
            if (arg is not { Length: > 0 })
                continue;
            if (positionalOnly || !IsOption(arg))
			{
				var aa = GrabCollection(args, ref i, arg);
				if (_splitPositional)
					parameters.Add(new ParameterDefinition(command.Definition, ++positionalIndex == 0 ? "positional": $"positional.{positionalIndex}", positional: true, collection: aa.Count > 1), new ParameterValue(aa));
				else
					positional.AddRange(aa);
				continue;
			}
			if (_doubleDashSeparator && arg == "--")
			{
				positionalOnly = true;
				continue;
			}

			var (name, value) = SplitNameValue(arg[0] == '-' && arg.Length > 1 && arg[1] == '-' ? arg.Substring(2) : arg.Substring(1));
			if (name.Length == 0)
			{
				messages.Add($"Invalid parameter: {arg}");
				continue;
			}
			if (name == "?" || String.Equals(name, "help", _comparison) || String.Equals(name, "h", _comparison))
			{
				helpRequested = true;
				continue;
			}
            if (value == String.Empty && i + 1 < args.Count && !IsOption(args[i + 1]))
                value = args[++i];

            var p = parameters.FirstOrDefault(o => String.Equals(o.Name, name, _comparison));
			if (p != null)
			{
				if (value is null || p.Definition.IsSwitch)
				{
					messages.Add($"Duplicate parameter: {arg}");
					continue;
				}
				p.Value = p.Value.Append(GrabCollection(args, ref i, value));
				continue;
			}

			if (value is null)
				parameters.Add(new ParameterDefinition(command.Definition, name, toggle: true), "true");
			else
				parameters.Add(new ParameterDefinition(command.Definition, name), new ParameterValue(GrabCollection(args, ref i, value)));
		}

		if (positional.Count > 0)
			parameters.Add(new ParameterDefinition(command.Definition, "positional", positional: true, collection: positional.Count > 1), new ParameterValue(positional));

		return (command, messages, helpRequested);
	}

	private (List<ArgumentCommand>, List<string>, bool) ParseArguments(CommandDefinition root)
	{
		var args = _args;
		List<ArgumentCommand> commands = new List<ArgumentCommand> { new ArgumentCommand(root) };
		var messages = new List<string>();
		bool options = true;
		bool helpRequested = false;

		for (int i = 0; i < args.Count; ++i)
		{
			var arg = args[i];
			if (arg is not { Length: > 0 })
				continue;

			if (options)
			{
				if (_doubleDashSeparator && arg == "--")
				{
					options = false;
					continue;
				}
				if (ParseCommandOrOption(args, ref i, commands, messages, ref helpRequested))
					continue;
			}

			ParsePositional(args, ref i, commands, messages);
		}

		CheckForMissingParameters(commands, messages);

		return (commands, messages, helpRequested);
	}

	private bool ParseCommandOrOption(List<string> args, ref int i, List<ArgumentCommand> commands, List<string> messages, ref bool helpRequested)
	{
		if (IsOption(args[i]))
		{
			ParseOption(args, ref i, commands, messages, ref helpRequested);
			return true;
		}

		var ci = commands[commands.Count - 1].Definition;
		if (ci.Commands is not null && ci.Commands.TryGetCommand(args[i], out var c))
		{
			var cm = new ArgumentCommand(c);
			commands[commands.Count - 1].Command = cm;
			commands.Add(cm);
			return true;
		}
		return false;
	}

	private void CheckForMissingParameters(List<ArgumentCommand> commands, List<string> messages)
	{
		var missing = new StringBuilder();
		var count = 0;
		foreach (var c in commands)
		{
			foreach (var mp in c.Definition.Parameters.Where(o => o.IsRequired && c.Parameters[o] is null))
			{
				if (count > 0)
					missing.Append(", ");
				mp.GetParameterName(missing, longDash: PreferLongDash, excludeAbbreviation: true);
				++count;
			}
		}
		if (count > 0)
			messages.Add($"Missing required parameter{(count > 1 ? "s" : "")}: {missing}");
	}

	private void ParsePositional(List<string> args, ref int i, List<ArgumentCommand> commands, List<string> messages)
	{
		var (command, definition) = GetNextPositionalParameter(commands, _splitPositional);
		if (definition is not null)
		{
			if (!_splitPositional)
			{
				command!.Parameters.Add(definition, definition.IsCollection ?
					new ParameterValue(GrabCollection(args, ref i, args[i])):
					new ParameterValue(args[i]));
				return;
			}

			var parameters = command!.Parameters;
			if (!definition.IsCollection)
			{
				parameters.Add(definition, new ParameterValue(args[i]));
				return;
			}
			var value = GrabCollection(args, ref i, args[i]);
			var pd = parameters[definition];
			if (pd is null)
				parameters.Add(definition, new ParameterValue(value));
			else
				pd.Value = pd.Value.Append(value);
			return;
		}

		if (!_allowUnknown)
		{
			messages.Add($"Unknown argument: {args[i]}");
			return;
		}

		// unknown positional argument
		var v = GrabCollection(args, ref i, args[i]);
		var c = commands[commands.Count - 1];

		var name = "positional";
		var k = 0;
		while (c.Parameters.Any(o => o.Name == name) || c.Definition.Parameters.Any(o => o.Name == name))
			name = $"positional.{++k}";
		
		var d = new ParameterDefinition(c.Definition, name, positional: true, unknown: true, collection: _splitPositional || v.Count > 1);
		c.Parameters.Add(d, new ParameterValue(v));
		return;

		static (ArgumentCommand?, ParameterDefinition?) GetNextPositionalParameter(List<ArgumentCommand> commands, bool combineLastParameter)
		{
			for (int i = commands.Count - 1; i >= 0; i--)
			{
				var parameters = commands[i].Parameters;
				var definitions = commands[i].Definition.Parameters;
				int k = -1;
				for (int j = 0; j < definitions.Count; ++j)
				{
					var d = definitions[j];
					if (!d.IsPositional)
						continue;
					if (parameters[d] is null)
						return (commands[i], d);
					k = j;	// last positional parameter
				}

				if (k < 0) continue;
				if (definitions[k].IsCollection)
					return combineLastParameter ? (commands[i], definitions[k]): default;
			}

			if (!combineLastParameter) return default;
			
			var unknown = commands[commands.Count - 1].Parameters.LastOrDefault(o => o.Definition is { IsUnknown: true, IsCollection: true });
			return unknown is null ? default: (commands[commands.Count - 1], unknown.Definition);
		}
	}

	private static ParameterDefinitionFindResult TryAdd(List<ArgumentCommand> commands, string name, string value, bool anonymous = false)
	{
		if (commands.Count == 1)
			return commands[0].Parameters.TryAdd(name, value, anonymous);

		for (int i = commands.Count - 1; i >= 0; i--)
        {
			var pp = commands[i].Parameters;
			var a = pp.TryAdd(name, value, false);
			if (a != ParameterDefinitionFindResult.NotFound)
				return a;
		}
		return anonymous ? commands[commands.Count - 1].Parameters.TryAdd(name, value, true): ParameterDefinitionFindResult.NotFound;
	}

	private void ParseOption(List<string> args, ref int i, List<ArgumentCommand> commands, List<string> messages, ref bool helpRequested)
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
			messages.Add($"Invalid parameter: {arg}");
			return;
		}

		var (command, definition) = dash ? GetSingleDashParameter(name, value, arg, commands, messages): FindParameterDefinition(name, arg, commands, messages);
        if (command is null)
			return;

		var parameters = command.Parameters;
		if (definition is null)
		{
			if (name is "?" or "h" or "help")
			{
				helpRequested = true;
				return;
			}
			if (_allowUnknown)
				GrabUnknownParameter(args, ref i, name, value, command, parameters);
			else
				messages.Add($"Unknown parameter: {arg}");
			return;
		}

		if (value is null)
		{
			if (CheckSwitchParameter(args, i, command, definition, messages, ref helpRequested))
				return;
			value = args[++i];
		}

		var p = parameters[definition];
		if (p is null)
		{
			parameters.Add(definition, definition.IsCollection ? new ParameterValue(GrabCollection(args, ref i, value)): new ParameterValue(value));
			return;
		}
		if (definition.IsCollection)
		{
			p.Value = p.Value.Append(GrabCollection(args, ref i, value));
			return;
		}

		messages.Add($"Duplicate parameter: {arg}");
		return;


		static void GrabUnknownParameter(List<string> args, ref int i, string name, string? value, ArgumentCommand command, ArgumentParameterCollection parameters)
		{
			ParameterValue v = value is null ? new ParameterValue("true"): new ParameterValue(GrabCollection(args, ref i, value));
			parameters.Add(new ParameterDefinition(command.Definition, name, collection: v.IsArray, unknown: true, toggle: value is null), v);
		}

		(ArgumentCommand?, ParameterDefinition?) GetSingleDashParameter(string name, string? value, string arg, List<ArgumentCommand> commands, List<string> messages)
        {
            var (command, definition) = FindParameterDefinition(name, arg, commands, messages, abbreviationOnly: true);

            if (definition is not null)
                return (command, definition);

            if (_combineOptions && value == null)
            {
                GrabFlags(name, arg, commands, messages);
                return default;
            }
            if (_strictDoubleDash && name.Length > 1)       // TODO: Do we need to support this?
            {
                messages.Add($"Invalid parameter: {arg}");
                return default;
            }

            return FindParameterDefinition(name, arg, commands, messages);
        }
    }

	private bool CheckSwitchParameter(List<string> args, int i, ArgumentCommand command, ParameterDefinition definition, List<string> messages, ref bool helpRequested)
	{
		if (definition.IsSwitch)
		{
			var parameters = command.Parameters;
			if (parameters[definition] is null)
				parameters.Add(definition, "true");
			helpRequested |= definition.Name == "?" || String.Equals(definition.Name, "help", command.Definition.Comparison);
			return true;
		}
		if (!_blankSeparator || i >= args.Count - 1 || IsOption(args[i + 1]))
		{
			messages.Add($"Missing value for parameter: {args[i]}");
			return true;
		}
		return false;
	}

	private (ArgumentCommand?, ParameterDefinition?) FindParameterDefinition(string name, string arg, List<ArgumentCommand> commands, List<string> messages, bool abbreviationOnly = false)
	{
		int k;
		for (k = commands.Count - 1; k >= 0; k--)
		{
			var found = commands[k].Definition.Parameters.TryFindDefinition(name, out var definition, commands[k].Definition.Comparison, similar: true, ignoreDelimiters: _ignoreNameSeparators, abbreviationOnly: abbreviationOnly);
			if (found == ParameterDefinitionFindResult.Found)
				return (commands[k], definition);
			if (found != ParameterDefinitionFindResult.Ambiguous)
				continue;
			messages.Add($"Ambiguous parameter: {arg}");
			return default;
		}
		return (commands[commands.Count - 1], null);
	}

	private bool IsOption(string name) => name.Length > 0 && (name[0] == '-' || (_allowSlash && name[0] == '/'));

	private (string Name, string? Value) SplitNameValue(string arg)
	{
		int j = _equalSeparator ?
			_colonSeparator ? arg.IndexOfAny(Separators): arg.IndexOf('='):
			_colonSeparator ? arg.IndexOf(':') : -1;
		return j < 0 ? (arg, null): (arg.Substring(0, j), arg.Substring(j + 1));
	}
	private static readonly char[] Separators = new[] { '=', ':' };

	private static IReadOnlyList<string> GrabCollection(List<string> args, ref int i, string value)
	{
		if (!value.EndsWith(",", StringComparison.Ordinal))
			return value.Split(',');
		var list = new List<string>();
		value = value.Substring(0, value.Length - 1);
		if (value.IndexOf(',') < 0)
			list.Add(value);
		else
			list.AddRange(value.Split(','));
		bool comma = true;
		while (comma && i < args.Count - 1)
		{
			var arg = args[++i];
			comma = arg.EndsWith(",", StringComparison.Ordinal);
			if (comma)
				arg = arg.Substring(0, arg.Length - 1);
			if (arg.IndexOf(',') < 0)
				list.Add(arg);
			else
				list.AddRange(arg.Split(','));
		}
		return list;
	}

	private void GrabFlags(string name, string arg, List<ArgumentCommand> commands, List<string> messages)
	{
		foreach (var c in name)
		{
			var f = c.ToString();
			var r = TryAdd(commands, f, "true", anonymous: _allowUnknown);
			if (r == ParameterDefinitionFindResult.NotFound)
				messages.Add(name.Length == 1 ? $"Unknown parameter {arg}" : $"Unknown flag {f} in parameter: {arg}");
			else if (r == ParameterDefinitionFindResult.Ambiguous)
				messages.Add(name.Length == 1 ? $"Duplicate parameter {arg}" : $"Duplicate flag {f} in parameter: {arg}");
		}
	}

	#endregion
}
