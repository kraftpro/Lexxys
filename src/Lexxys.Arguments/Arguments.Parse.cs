using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Lexxys;

public partial class Arguments
{
	internal static readonly ParameterValue SwitchValue = ParameterValue.EmptyArray;

	public static Arguments Parse(IEnumerable<string> args, bool ignoreCase = false, bool allowSlash = false, bool colonSeparator = true, bool equalSeparator = true, bool strict = false, ParameterNaming namingStyle = ParameterNaming.KebabCase, ParameterMatching matchingType = ParameterMatching.Fluent)
		=> Parse(args, new ArgumentsConfig
		{
			IgnoreCase = ignoreCase,
			AllowSlash = allowSlash,
			ColonSeparator = colonSeparator,
			EqualSeparator = equalSeparator,
			Strict = strict,
			NamingStyle = namingStyle,
			MatchingType = matchingType,
			AllowUnknown = true
		});

	public static Arguments Parse(IEnumerable<string> args) => Parse(args, (ArgumentsConfig?)null);

	public static Arguments Parse(IEnumerable<string> args, ArgumentsConfig? config)
	{
		var parser = new AutoArgsParser(config ?? ArgumentsConfig.Default);
		return parser.Parse([.. args]);
	}

	public static Arguments Parse(IEnumerable<string> args, ArgumentsBuilder builder)
	{
		var parser = new BuilderArgsParser(builder);
		return parser.Parse([.. args]);
	}


	private static ArgumentsDictionary CombineParameters(ArgumentCommand command)
	{
		var outer = command.Command;
		if (outer is null)
			return command.Parameters;

		var result = command.Parameters.Clone();
		do
		{
			foreach (var kvp in outer)
			{
				if (result.TryGetValue(kvp.Key, out var existing))
					result[kvp.Key] = existing.Append(kvp.Value);
				else
					result[kvp.Key] = kvp.Value;
			}
			outer = outer.Command;
		} while (outer != null);
		return result;
	}


	abstract class ArgsParser(ArgumentsConfig? config)
	{
		protected ArgumentsConfig Config { get; } = config ?? ArgumentsConfig.Default;

		public abstract Arguments Parse(List<string> args);

		protected (string Name, string? Value) SplitNameValue(ReadOnlySpan<char> arg)
		{
			int j = Config.EqualSeparator ?
				Config.ColonSeparator ? arg.IndexOfAny(Separators): arg.IndexOf('='):
				Config.ColonSeparator ? arg.IndexOf(':'): -1;
			return j < 0 ? (arg.ToString(), null): (arg[..j].ToString(), arg[(j + 1)..].ToString());
		}
		private static readonly char[] Separators = ['=', ':'];

		/// <summary>
		/// Expands a parameter value into a list of items, gathering comma-separated values that may span several
		/// command-line tokens.
		/// </summary>
		/// <param name="args">The full argument list.</param>
		/// <param name="i">
		/// The index of the token <paramref name="value"/> came from. It is advanced past every additional token
		/// consumed as part of the list, so the caller's loop continues after the last gathered value.
		/// </param>
		/// <param name="value">The raw value text of the current token (everything after the option name).</param>
		/// <returns>The list of values parsed from <paramref name="value"/> and any continuation tokens.</returns>
		/// <remarks>
		/// <para>
		/// Items are separated by commas, not whitespace. A single token splits on its commas:
		/// <c>--tag=a,b,c</c> yields <c>[a, b, c]</c>, and <c>--tag=a</c> yields <c>[a]</c>.
		/// </para>
		/// <para>
		/// A list continues into the following tokens only while it is "left open" by a trailing comma. After a token
		/// that ends with a comma, the next token (when it is not an option) is consumed and appended, and gathering
		/// repeats while each consumed token also ends with a comma. This is what lets a list be written with spaces
		/// around the separators, for example <c>--tag a, b, c</c> or <c>--tag a,b, c</c>.
		/// </para>
		/// <para>
		/// Consequently, plain whitespace-separated values are NOT gathered: in <c>--tag a b c</c> the first token
		/// <c>a</c> has no trailing comma, so only <c>a</c> belongs to the option and <c>b</c> and <c>c</c> are left
		/// for the caller to treat as the next positional/option tokens. Use commas to build multi-token lists.
		/// </para>
		/// </remarks>
		protected IReadOnlyList<string> PickUpCollection(List<string> args, ref int i, string value)
		{
			if (value.StartsWith(','))
				value = value[1..];
			// No trailing comma => the value is self-contained; split it and stop.
			if (!value.EndsWith(','))
				return value.Split(',');
			var list = new List<string>();
			value = value[..^1];
			if (value.IndexOf(',') < 0)
				list.Add(value);
			else
				list.AddRange(value.Split(','));
			// Trailing comma keeps the list open: keep consuming following tokens until one does not end with a comma
			// (or an option / end of input is reached).
			bool comma = true;
			while (comma && i < args.Count - 1)
			{
				var arg = args[i + 1];
				if (Config.IsOption(arg))
					break;
				++i;
				comma = arg.EndsWith(',');
				if (comma)
					arg = arg[..^1];
				if (arg.IndexOf(',') < 0)
					list.Add(arg);
				else
					list.AddRange(arg.Split(','));
			}
			return list;
		}
	}

	class AutoArgsParser(ArgumentsConfig? config):
		ArgsParser(
			config is null ? DefaultConfig:
			config.AllowUnknown ? config: new ArgumentsConfig(config) { AllowUnknown = true }
			)
	{
		private static readonly ArgumentsConfig DefaultConfig = new ArgumentsConfig { AllowUnknown = true };

		public override Arguments Parse(List<string> args)
		{
			if (args is null) throw new ArgumentNullException(nameof(args));

			var (command, messages, helpRequested) = ParseArgs(args);
			return new Arguments(command, messages, helpRequested);
		}

		private (ArgumentCommand, List<string>, bool) ParseArgs(List<string> args)
		{
			var command = new ArgumentCommand("", Config, new AutoArgumentsDictionary(Config.MatchingType == ParameterMatching.Fluent, Config.IgnoreCase));
			var parameters = command.Parameters;
			var positional = new List<string>();
			var messages = new List<string>();
			bool positionalOnly = false;
			bool helpRequested = false;
			StringComparison comparison = Config.IgnoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal;
			for (int i = 0; i < args.Count; ++i)
			{
				var arg = args[i];
				if (arg is not { Length: > 0 })
					continue;
				if (positionalOnly || !Config.IsOption(arg))
				{
					var aa = PickUpCollection(args, ref i, arg);
					positional.AddRange(aa);
					continue;
				}
				if (arg == "--")
				{
					positionalOnly = true;
					continue;
				}

				bool longDash = arg[0] == '-' && arg.Length > 1 && arg[1] == '-';
				var (name, value) = SplitNameValue(longDash ? arg.AsSpan(2): arg.AsSpan(1));
				if (String.Equals(name, "help", comparison) || !longDash && (name == "?" || String.Equals(name, "h", comparison)))
				{
					helpRequested = true;
					continue;
				}
				if (value == String.Empty && i + 1 < args.Count && !Config.IsOption(args[i + 1]))
					value = args[++i];

				parameters.TryGetValue(name, out ParameterValue p);
				if (p != null)
				{
					if (p.IsSwitch)
					{
						messages.Add($"Duplicate parameter: {arg}");
						continue;
					}
					if (value is null)
					{
						if (i + 1 >= args.Count || Config.IsOption(args[i + 1]))
						{
							messages.Add($"Missing value for parameter: {arg}");
							continue;
						}
						value = args[++i];
					}
					p = p.Append(PickUpCollection(args, ref i, value));
					parameters[name] = p;
					continue;
				}

				// if now explicit value specified, consider it as a switch
				parameters.Add(name, value is null ? SwitchValue: new ParameterValue(PickUpCollection(args, ref i, value)));
			}

			if (positional.Count > 0)
				parameters.Add(PositionalKey, new ParameterValue(positional));

			return (command, messages, helpRequested);
		}
	}

	class BuilderArgsParser: ArgsParser
	{
		private readonly List<string> _messages;
		private readonly List<ArgumentCommand> _commands;
		private readonly StringComparison _comparison;
		private bool _helpRequested;

		public BuilderArgsParser(ArgumentsBuilder builder): base(builder.Config)
		{
			_messages = [];
			_commands = [new ArgumentCommand(builder.Root)];
			_comparison = Config.IgnoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal;
		}

		public override Arguments Parse(List<string> args)
		{
			if (args is null) throw new ArgumentNullException(nameof(args));
			var (command, messages, helpRequested) = ParseArgs(args);
			return new Arguments(command, messages, helpRequested);
		}

		private (ArgumentCommand command, List<string> Messages, bool HelpRequested) ParseArgs(List<string> args)
		{
			bool positionalOnly = false;

			for (int i = 0; i < args.Count; ++i)
			{
				var arg = args[i];
				if (arg is not { Length: > 0 })
					continue;

				if (positionalOnly)
				{
					ParsePositional(args, ref i);
				}
				else if (arg == "--")
				{
					positionalOnly = true;
				}
				else if (!ParseOption(args, ref i) && !ParseCommand(args, ref i))
				{
					ParsePositional(args, ref i);
				}
			}

			CheckForMissingParameters();

			// Swap the OuterCommand references.
			ArgumentCommand? outer = null;
			for (int i = _commands.Count - 1; i >= 0; --i)
			{
				outer = _commands[i] = _commands[i].WithOuterCommand(outer);
			}

			return (_commands[0], _messages, _helpRequested);
		}

		private void ParsePositional(List<string> args, ref int i)
		{
			var (command, definition) = GetNextPositionalParameter(_commands);
			if (definition is not null)
			{
				var value = definition.IsCollection ?
					new ParameterValue(PickUpCollection(args, ref i, args[i])):
					new ParameterValue(args[i]);
				if (command!.Parameters.TryGetValue(definition.Name, out ParameterValue p1))
					command.Parameters[definition.Name] = p1.Append(value);
				else
					command.Parameters.Add(definition.Name, value);
				return;
			}

			if (!Config.AllowUnknown)
			{
				_messages.Add($"Unknown parameter: {args[i]}");
				return;
			}

			// unknown positional parameter
			var v = PickUpCollection(args, ref i, args[i]);
			var c = _commands[^1];

			if (!c.Parameters.TryGetValue(PositionalKey, out ParameterValue p))
				c.Parameters.Add(PositionalKey, new ParameterValue(v));
			else
				c.Parameters[PositionalKey] = p.Append(v);
			return;

			static (ArgumentCommand?, ParameterDefinition?) GetNextPositionalParameter(List<ArgumentCommand> commands)
			{
				for (int i = commands.Count - 1; i >= 0; --i)
				{
					var parameters = commands[i].Parameters;
					var definitions = commands[i].Definition.Parameters;
					int k = -1;
					for (int j = 0; j < definitions.Count; ++j)
					{
						var d = definitions[j];
						if (!d.IsPositional)
							continue;
						if (!parameters.ContainsKey(d.Name) || d.IsCollection)
							return (commands[i], d);
						k = j;  // last positional parameter
					}

					if (k < 0) continue;
					if (definitions[k].IsCollection)
						return default;
				}

				return default;
			}
		}

		private bool ParseOption(List<string> args, ref int i)
		{
			string arg = args[i];
			if (!Config.IsOption(arg))
				return false;

			var (dash, longDash) = (false, false);
			if (arg[0] == '-')
				if (arg[1] == '-')
					longDash = true;
				else
					dash = true;
			var (name, value) = SplitNameValue(longDash ? arg.AsSpan(2): arg.AsSpan(1));
			if (name.Length == 0)
			{
				_messages.Add($"Invalid parameter: {arg}");
				return true;
			}

			ArgumentCommand? command = null;
			ParameterDefinition? definition = null;

			// For parameters with dash, try to find definition by abbreviation first. If strict mode is on, consider it as an error if no exact match found.
			if (dash)
			{
				(command, definition) = FindByAbbreviation(name, _commands);
				if (definition is null && Config.Strict)
				{
					if (value is null)
						SplitFlags(name, arg);
					else
						_messages.Add($"Unexpected value for flags parameter: {arg}");
					return true;
				}
			}

			if (command is null)
				(command, definition) = FindParameterDefinition(name, arg);

			if (command is null) // Ambiguous parameter name
				return true;

			var parameters = command.Parameters;

			// If no definition found, consider it as an unknown parameter.
			if (definition is null)
			{
				if (IsHelpParameter(name))
				{
					_helpRequested = true;
					return true;
				}
				if (Config.AllowUnknown)
					parameters.Add(name, value is null ? SwitchValue: new ParameterValue(PickUpCollection(args, ref i, value)));
				else
					_messages.Add($"Unknown parameter: {arg}");
				return true;
			}

			bool exists = parameters.TryGetValue(definition.Name, out ParameterValue p);
			if (value is null && definition.IsSwitch)
			{
				if (!exists)
					parameters.Add(definition.Name, SwitchValue);
				_helpRequested |= IsHelpParameter(definition.Name);
				return true;
			}

			// If value is not specified explicitly, try to take the next argument as value if it is not an option.
			if (String.IsNullOrEmpty(value))
			{
				if (i >= args.Count - 1 || Config.IsOption(args[i + 1]) || (value is null && !Config.BlankSeparator))
				{
					_messages.Add($"Missing value for parameter: {arg}");
					return true;
				}
				value = args[++i];
			}

			var collection = definition.IsCollection ? PickUpCollection(args, ref i, value!): null;
			if (!exists)
			{
				parameters.Add(definition.Name, collection == null ? new ParameterValue(value!): new ParameterValue(collection));
				return true;
			}
			if (collection != null)
			{
				parameters[definition.Name] = p.Append(collection);
				return true;
			}

			_messages.Add($"Duplicate parameter: {arg}");
			return true;

			bool IsHelpParameter(string argName) => String.Equals(argName, "help", _comparison) || !longDash && (argName == "?" || String.Equals(argName, "h", _comparison));

			static (ArgumentCommand?, ParameterDefinition?) FindByAbbreviation(string name, List<ArgumentCommand> commands)
			{
				int k;
				for (k = commands.Count - 1; k >= 0; k--)
				{
					var c = commands[k];
					var comparison = c.Definition.Comparison;
					var definition = c.Definition.Parameters.FirstOrDefault(o => o.Abbreviation.Any(a => String.Equals(a, name, comparison)));
					if (definition != null)
						return (c, definition);
				}
				return default;
			}
		}

		// Splits the specified name into individual characters and tries to add them as flags.
		private void SplitFlags(string name, string arg)
		{
			foreach (var c in name)
			{
				var f = c.ToString();
				var r = TryAdd(_commands, f, unknown: Config.AllowUnknown);
				if (r == ParameterDefinitionFindResult.NotFound)
					_messages.Add(name.Length == 1 ? $"Unknown parameter {arg}": $"Unknown flag {f} in parameter: {arg}");
				else if (r == ParameterDefinitionFindResult.Ambiguous)
					_messages.Add(name.Length == 1 ? $"Duplicate parameter {arg}": $"Duplicate flag {f} in parameter: {arg}");
			}

			static ParameterDefinitionFindResult TryAdd(List<ArgumentCommand> commands, string name, bool unknown)
			{
				for (int i = commands.Count - 1; i >= 0; --i)
				{
					var command = commands[i];
					var definition = command.Definition;
					var pp = definition.Parameters;
					var (_, pd) = Arguments.FindParam(pp, name, definition.Comparison, ParameterMatching.Strict);
					if (pd != null)
					{
						if (command.Parameters.TryGetValue(name, out var p))
							return p.IsSwitch ? ParameterDefinitionFindResult.Found: ParameterDefinitionFindResult.Ambiguous;

						command.Parameters[pd.Name] = SwitchValue;
						return ParameterDefinitionFindResult.Found;
					}
				}
				if (!unknown)
					return ParameterDefinitionFindResult.NotFound;

				commands[^1].Parameters[name] = SwitchValue;
				return ParameterDefinitionFindResult.Found;
			}
		}

		private bool ParseCommand(List<string> args, ref int i)
		{
			var ci = _commands[^1].Definition;
			if (ci.Commands is null || !ci.Commands.TryGetCommand(args[i], out var c))
				return false;

			var cm = new ArgumentCommand(c, _commands[^1]);
			_commands.Add(cm);
			return true;
		}

		private void CheckForMissingParameters()
		{
			var missing = new StringBuilder();
			var count = 0;
			foreach (var c in _commands)
			{
				foreach (var mp in c.Definition.Parameters.Where(o => o.IsRequired && !c.Parameters.ContainsKey(o.Name)))
				{
					if (count > 0)
						missing.Append(", ");
					missing.Append(mp.Name);
					++count;
				}
			}
			if (count > 0)
				_messages.Add($"Missing required parameter{(count > 1 ? "s": "")}: {missing}");
		}


		private (ArgumentCommand?, ParameterDefinition?) FindParameterDefinition(string name, string arg)
		{
			int k;
			for (k = _commands.Count - 1; k >= 0; k--)
			{
				var c = _commands[k];
				var (found, definition) = Arguments.FindParam(c.Definition.Parameters, name, c.Definition.Comparison, Config.MatchingType);
				if (found != ParameterDefinitionFindResult.NotFound)
				{
					if (found == ParameterDefinitionFindResult.Found)
						return (c, definition);
					_messages.Add($"Ambiguous parameter: {arg}");
					return default;
				}
			}
			return (_commands[^1], null);
		}
	}

	#region Typed parser

	/// <summary>
	/// Parses the command line arguments and returns the parsed option value of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Option type.</typeparam>
	/// <param name="args">Command line arguments.</param>
	/// <param name="settings">Optional settings for the parser.</param>
	/// <returns></returns>
	public static Arguments<T> Parse<T>(IEnumerable<string> args, ArgumentsConfig? settings = null) where T: class, new()
	{
		if (args is null) throw new ArgumentNullException(nameof(args));

		IReadOnlyCollection<string> argsCollection = args as IReadOnlyCollection<string> ?? args.ToList();

#if NET
		// Prefer the source-generated parser, dispatching through the ICliParameters<T> contract (static abstract
		// members) rather than probing for method signatures. The bridge from the unconstrained T to the
		// interface-constrained helper is resolved once per type and cached in GeneratedParser<T>.
		if (GeneratedParser<T>.Parse is { } generated)
			return generated(argsCollection, settings);
#endif

		Parse(argsCollection, typeof(T), settings, out var arguments, out var value);
		return new Arguments<T>(arguments, (T)value);
	}

#if NET

	/// <summary>
	/// Builds and binds a generated option model through its <see cref="ICliParameters{T}"/> static members.
	/// </summary>
	private static Arguments<T> ParseGenerated<T>(IReadOnlyCollection<string> args, ArgumentsConfig? settings) where T: class, ICliParameters<T>, new()
	{
		var builder = T.CreateBuilder(new ArgumentsBuilder(GetSettings(typeof(T), settings)));
		var arguments = Parse(args, builder);
		var errors = new List<string>();
		var value = T.Parse(arguments, errors);
		return new Arguments<T>(arguments, value, errors);
	}

	/// <summary>
	/// Per-type cache for the generated-parser delegate. The reflective bridge (interface check, generic method
	/// construction) runs once in the static initializer; <see cref="Parse"/> is <c>null</c> when <typeparamref name="T"/>
	/// does not implement <see cref="ICliParameters{T}"/>.
	/// </summary>
	private static class GeneratedParser<T> where T: class, new()
	{
		public static readonly Func<IReadOnlyCollection<string>, ArgumentsConfig?, Arguments<T>>? Parse = Build();

		private static Func<IReadOnlyCollection<string>, ArgumentsConfig?, Arguments<T>>? Build()
		{
			if (!typeof(ICliParameters<T>).IsAssignableFrom(typeof(T)))
				return null;
			var method = typeof(Arguments)
				.GetMethod(nameof(ParseGenerated), BindingFlags.NonPublic | BindingFlags.Static)!
				.MakeGenericMethod(typeof(T));
			return (Func<IReadOnlyCollection<string>, ArgumentsConfig?, Arguments<T>>)
				method.CreateDelegate(typeof(Func<IReadOnlyCollection<string>, ArgumentsConfig?, Arguments<T>>));
		}
	}

#endif

	private static void Parse(IEnumerable<string> args, Type type, ArgumentsConfig? settings, out Arguments arguments, out object value)
	{
		if (!type.IsClass || type == typeof(string)) throw new ArgumentOutOfRangeException(nameof(type), type, null);

		var builder = new ArgumentsBuilder(GetSettings(type, settings)).Use(type);
		var arg = Arguments.Parse(args, builder);
		var errors = new List<string>();
		var val = ConstructValue(arg, type, errors, builder.Config.NamingStyle);
		foreach (var item in errors)
		{
			arg.Errors.Add(item);
		}
		arguments = arg;
		value = val;
	}

	private static object ConstructValue(IArguments command, Type type, List<string> errors, ParameterNaming namingStyle)
	{
		var value = Activator.CreateInstance(type) ?? throw new ArgumentOutOfRangeException(nameof(type), type, null);
		var items = GetProperties(type, namingStyle);
		StringComparison comparison = command.Definition.Comparison;
		foreach (var item in command)
		{
			var field = items.FirstOrDefault(o => String.Equals(o.Name, item.Key, comparison));
			if (field.Type == null) continue;
			if (TryGetArgumentValue(item.Value, field.Type, item.Key, errors, out var v))
				field.Setter(value, v);
			else
				errors.Add($"Invalid value for parameter '{item.Key}'");
		}
		var cmd = command.Command;
		if (cmd == null) return value;

		var cmdField = items.FirstOrDefault(o => String.Equals(o.Name, cmd.Name, comparison));
		if (cmdField.Type == null) return value;

		var commandValue = ConstructValue(cmd, cmdField.Type, errors, namingStyle);
		cmdField.Setter(value, commandValue);
		return value;
	}

	private static ArgumentsConfig GetSettings(Type type, ArgumentsConfig? settings)
		=> settings ?? ArgumentsConfig.FromAttribute(type.GetCustomAttribute<CliParametersAttribute>());

	private static bool TryGetArgumentValue(ParameterValue value, Type type, string name, List<string> errors, out object? result)
	{
		if (TryGetCollectionElementType(type, out var itemType))
			return TryGetCollectionValue(value, type, itemType, name, errors, out result);

		if (type == typeof(bool) && value.IsSwitch)
		{
			result = true;
			return true;
		}

		if (TryGetValue(value.ToString(), type, out result))
			return true;

		errors.Add($"Invalid value for parameter '{name}'");
		return false;
	}

	private static bool TryGetValue(string value, Type type, out object? result)
	{
		var argConverter = ParameterValue.GetConverter(type);
		return argConverter != null ?
			argConverter.Invoke(value, type, out result) :
			Strings.TryGetValue(value, type, out result);
	}

	private static bool TryGetCollectionValue(ParameterValue value, Type type, Type itemType, string name, List<string> errors, out object? result)
	{
		var values = value.ArrayValue ?? [value.StringValue!];
		if (itemType == typeof(string))
		{
			result = type.IsArray || type.IsAssignableFrom(typeof(IReadOnlyList<string>)) ? values: new List<string>(values);
			return true;
		}

		var array = Array.CreateInstance(itemType, values.Length);
		for (var i = 0; i < values.Length; ++i)
		{
			if (!TryGetValue(values[i], itemType, out var item))
			{
				result = null;
				errors.Add($"Invalid value for parameter '{name}.{i + 1}'");
				return false;
			}
			array.SetValue(item, i);
		}

		result = type.IsArray || type.IsAssignableFrom(typeof(IReadOnlyList<>).MakeGenericType(itemType)) ? array:
			Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType), array);
		return true;
	}

	private static bool TryGetCollectionElementType(Type type, [NotNullWhen(true)] out Type? itemType)
	{
		itemType = null;
		if (type == typeof(string))
			return false;

		if (type.IsArray)
			itemType = type.GetElementType()!;
		else if (
			type.IsGenericType && type.GetGenericArguments() is [var genericItemType] &&
			type.IsAssignableFrom(typeof(List<>).MakeGenericType(genericItemType))
			)
			itemType = genericItemType;
		return itemType != null;
	}

	internal static List<ParameterInfo> GetProperties(Type type, ParameterNaming namingStyle)
	{
		return type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
			.Where(o => o.CanWrite && o.GetSetMethod() != null && o.GetIndexParameters().Length == 0)
			.Select(o =>
			{
				var cmd = o.GetCustomAttribute<CliCommandAttribute>();
				var prm = o.GetCustomAttribute<CliOptionAttribute>();
				return new ParameterInfo(cmd, prm, prm?.Name ?? cmd?.Name ?? FormatArgumentName(o.Name, namingStyle), o.PropertyType, o.SetValue);
			})
			.Union(type
				.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField)
				.Where(o => !o.IsInitOnly && !o.IsLiteral)
				.Select(o =>
				{
					var cmd = o.GetCustomAttribute<CliCommandAttribute>();
					var prm = o.GetCustomAttribute<CliOptionAttribute>();
					return new ParameterInfo(cmd, prm, prm?.Name ?? cmd?.Name ?? FormatArgumentName(o.Name, namingStyle), o.FieldType, o.SetValue);
				})
				).ToList();
	}

	/// <summary>
	/// Converts a member name to a command-line name using the specified naming style.
	/// </summary>
	/// <param name="name">The member name to convert.</param>
	/// <param name="namingStyle">The command-line naming style.</param>
	/// <returns>The formatted command-line name.</returns>
	public static string FormatArgumentName(string name, ParameterNaming namingStyle)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));
		if (name.Length == 0) return name;
		if (name.IndexOfAny(['-', '_', '.', ' ']) >= 0)
			return name.Trim().ToLowerInvariant().Replace(' ', namingStyle switch
			{
				ParameterNaming.SnakeCase => '_',
				ParameterNaming.DotCase => '.',
				_ => '-'
			});
		if (namingStyle == ParameterNaming.CamelCase)
			return char.ToLowerInvariant(name[0]) + name.Substring(1);
		if (namingStyle == ParameterNaming.PascalCase)
			return char.ToUpperInvariant(name[0]) + name.Substring(1);
		char separator = namingStyle switch
		{
			ParameterNaming.SnakeCase => '_',
			ParameterNaming.DotCase => '.',
			_ => '-'
		};
		var parts = Strings.SplitByCapitals(name.AsSpan());
		if (parts.Count == 0)
			return name.ToLowerInvariant();
		var result = new StringBuilder();
		var lower = name.ToLowerInvariant();
		for (int i = 0; i < parts.Count; ++i)
		{
			if (i > 0)
				result.Append(separator);
			result.Append(lower, parts[i].Index, parts[i].Length);
		}
		return result.ToString();
	}

	#endregion
}
