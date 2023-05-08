using System.Text;

#nullable enable
namespace Lexxys;

using Xml;


public class Arguments2
{

}

public class Parameters
{
	public delegate bool Parser<T>(string? value, [MaybeNullWhen(false)] out T result);
	public delegate string? Validator<T>(T value);

	private readonly StringComparison _comparison;
	private readonly OrderedBag<string, CommandDefinition> _commands;
	private CommandDefinition _selected;

	public Parameters(bool ignoreCase = false)
	{
		_comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal;
		_selected = new CommandDefinition(String.Empty, null, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);
		CommonParameters = _selected.Parameters;
		_commands = new OrderedBag<string, CommandDefinition>(ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal)
			{ [String.Empty] = _selected };
	}

	public CommandDefinition SelectedCommand => _selected;

	public IReadOnlyList<ParameterDefinition> SelectedParameters => _selected.Parameters;

	public IReadOnlyList<ParameterDefinition> CommonParameters { get; }

	public Parameters Command(string? name = null, string? description = null)
	{
		if (_commands.TryGetValue(name ?? String.Empty, out var cmd))
			_selected = cmd;
		else
			_commands.Add(name!, (_selected = new CommandDefinition(name!, description, _comparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal)));
		return this;
	}

	public Parameters Add(ParameterDefinition parameter)
	{
		if (parameter is null)
			throw new ArgumentNullException(nameof(parameter));
		_selected.Add(parameter);
		return this;
	}

	public Parameters Parameter(string name, string[]? abbrev = null, string? valueName = null, Parser<string>? parser = null, Validator<string>? validator = null, string? description = null, bool positional = false, bool required = false)
	{
		_selected.Add(new ParameterDefinition<string>(name, null, abbrev, valueName, parser, validator, description, positional, required));
		return this;
	}

	public Parameters Parameter(Ref<string> parameter, string name, string[]? abbrev = null, string? valueName = null, Parser<string>? parser = null, Validator<string>? validator = null, string? description = null, bool positional = false, bool required = false)
	{
		_selected.Add(new ParameterDefinition<string>(name, parameter, abbrev, valueName, parser, validator, description, positional, required));
		return this;
	}

	public Parameters Parameter<T>(string name, string[]? abbrev = null, string? valueName = null, Parser<T>? parser = null, Validator<T>? validator = null, string? description = null, bool positional = false, bool required = false)
	{
		_selected.Add(new ParameterDefinition<T>(name, null, abbrev, valueName, parser, validator, description, positional, required));
		return this;
	}

	public Parameters Parameter<T>(Ref<T> parameter, string name, string[]? abbrev = null, string? valueName = null, Parser<T>? parser = null, Validator<T>? validator = null, string? description = null, bool positional = false, bool required = false)
	{
		_selected.Add(new ParameterDefinition<T>(name, parameter, abbrev, valueName, parser, validator, description, positional, required));
		return this;
	}

	public bool Apply(string[] args, out string? message)
	{
		if (args is null)
			throw new ArgumentNullException(nameof(args));

		_selected = _commands[String.Empty];
		var parameters = _selected.Parameters;
		CommandDefinition? cmd = null;

		for (int i = 0; i < args.Length; ++i)
		{
			var arg = args[i];
			if (arg is not { Length: > 0 })
				continue;

			if (arg[0] == '-' || arg[0] == '/')
			{
				var name = arg.Substring(1);
				if (name == "?")
				{
					message = null;
					return false;
				}
				string? value = null;
				int j = name.IndexOfAny(EqualSigns);
				if (j >= 0)
				{
					value = name.Substring(j + 1).TrimStart();
					name = name.Substring(0, j).TrimEnd();
				}
				if (name.Length == 0)
				{
					message = $"Invalid parameter: {arg}";
					return false;
				}

				var parameter = parameters.FirstOrDefault(o => o.IsMatch(name, _comparison));
				if (parameter is null)
				{
					var pp = parameters.Where(o => o.IsSimilar(name, _comparison)).ToList();
					if (pp.Count > 1)
					{
						message = $"Ambiguous parameter: {arg}";
						return false;
					}
					if (pp.Count == 1)
						parameter = pp[0];
				}
				if (parameter is null)
				{
					message = $"Unknown parameter: {arg}";
					return false;
				}

				if (parameter.Value is not null)
				{
					message = $"Duplicate parameter: {arg}";
					return false;
				}

				value ??= parameter.IsSwitch ? "true": i < args.Length - 1 ? args[++i]: String.Empty;
				if (parameter.IsCollection)
				{
					while (value.EndsWith(",", StringComparison.Ordinal) && i < args.Length - 1)
						value += args[++i];
				}
				parameter.Value = value;
				continue;
			}

			if (cmd is null && _commands.TryGetValue(arg, out cmd))
			{
				_selected = cmd;
				var pp = new List<ParameterDefinition>(_selected.Parameters);
				pp.AddRange(_commands[String.Empty].Parameters);
				parameters = pp;
				continue;
			}

			var positional = parameters.FirstOrDefault(o => o.IsPositional && o.Value is null);
			if (positional is null)
			{
				message = $"Unknown parameter: {arg}";
				return false;
			}
			positional.Value = arg;
		}

		if (parameters.Any(o => o.IsRequired && o.Value is null))
		{
			var pp = parameters.Where(o => o.IsRequired && o.Value is null).Select(o => o.PrintName).ToList();
			message = $"Missing required parameter{(pp.Count > 1 ? "s": "")}: {String.Join(", ", pp)}";
			return false;
		}

		message = null;
		return true;
	}
	private static readonly char[] EqualSigns = new[] { ':', '=' };

	const int Indent = 2;
	const int MaxOffset = 32;

	public void Usage(string program, bool details = false, TextWriter? writer = null)
	{
		writer ??= Console.Out;
		UsageString(program, _selected.Name, writer);
		writer.WriteLine();
		if (_selected.Name.Length == 0)
		{
			UsageDetails(details, writer);
			return;
		}

		var offset = GetOffset();
		UsageCommand(writer, _selected, offset, true);
		UsageParameters(writer, _commands[String.Empty].Parameters, Indent, offset, true);
	}

	private void UsageDetails(bool details, TextWriter writer)
	{
		var offset = GetOffset();
		if (_commands.Count > 1)
		{
			bool nl = false;
			foreach (var item in _commands.Keys.OrderBy(o => o))
			{
				if (item.Length == 0)
					continue;
				if (nl && details)
					writer.WriteLine();
				UsageCommand(writer, _commands[item], offset, details);
				nl = true;
			}
		}

		UsageParameters(writer, _commands[String.Empty].Parameters, Indent, offset, _commands.Count > 1);
		}

	private int GetOffset()
	{
		var offset = 1 + _commands.Values.SelectMany(o => o.Parameters).Max(o => o.GetParameterName(true).Length);
		int cmdOffset = 2 + _commands.Keys.Max(o => o.Length + 1);
		if (offset < cmdOffset)
			offset = cmdOffset;
		offset = Math.Min(MaxOffset, offset);
		return offset;
	}

	private void UsageString(string program, string command, TextWriter writer)
	{
		writer.Write($"Usage: {program}");
		Parameters.UsageString(_commands[String.Empty].Parameters, writer);
		if (!String.IsNullOrEmpty(command) && _commands.TryGetValue(command, out var cmd))
		{
			writer.Write(' ');
			writer.Write(cmd.Name);
			Parameters.UsageString(cmd.Parameters, writer);
		}
		else if (_commands.Count > 1)
		{
			writer.Write(" <command> <parameters>");
		}

		writer.WriteLine();
	}

	private static void UsageString(IReadOnlyCollection<ParameterDefinition> parameters, TextWriter writer)
	{
		foreach (var item in parameters.Where(o => o.IsPositional))
		{
			writer.Write(' ');
			string name = item.ValueName ?? item.Name;
			if (item.IsRequired)
				writer.Write($"<{name}>");
			else
				writer.Write($"[<{name}>]");
		}
		foreach (var item in parameters.Where(o => !o.IsPositional))
		{
			writer.Write(' ');
			string name = item.GetParameterName(true);
			if (item.IsRequired)
				writer.Write(name);
			else
				writer.Write($"[{name}]");
		}
	}

	private static void UsageCommand(TextWriter writer, CommandDefinition cmd, int offset, bool details)
	{
		writer.Write(new String(' ', Indent));
		writer.Write(cmd.Name);
		if (cmd.Description != null)
		{
			if (cmd.Name.Length < offset)
			{
				writer.Write(new String(' ', offset - cmd.Name.Length));
			}
			else
			{
				writer.WriteLine();
				writer.Write(new String(' ', Indent + offset));
			}
			writer.Write(cmd.Description);
		}
		writer.WriteLine();

		if (details && cmd.Parameters.Count > 0)
		{
			UsageParameters(writer, cmd.Parameters, Indent + Indent, offset, false);
		}
	}

	private static void UsageParameters(TextWriter writer, IReadOnlyCollection<ParameterDefinition> parameters, int indent, int offset, bool newLine)
	{
		var pad = new String(' ', indent);
		offset -= indent - Indent;
		foreach (var item in parameters.Where(o => !o.IsPositional))
		{
			if (newLine)
			{
				writer.WriteLine();
				newLine = false;
			}
			writer.Write(pad);
			var name = item.GetParameterName(true);
			writer.Write(name);
			if (item.Description != null)
			{
				if (name.Length < offset)
				{
					writer.Write(new String(' ', offset - name.Length));
				}
				else
				{
					writer.WriteLine();
					writer.Write(new String(' ', indent + offset));
				}
				writer.Write(item.Description);
			}
			writer.WriteLine();
		}
	}
}

public class CommandDefinition
{
	private readonly OrderedBag<string, ParameterDefinition> _parameters;

	public string Name { get; }
	public string? Description { get; }
	public IReadOnlyList<ParameterDefinition> Parameters => _parameters;

	public CommandDefinition(string name, string? description = null, StringComparer? comparer = null)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		Name = name;
		Description = description;
		_parameters = new OrderedBag<string, ParameterDefinition>(comparer);
	}

	ParameterDefinition? this[string name]
	{
		get
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			return _parameters.TryGetValue(name, out var value) ? value : null;
		}
	}

	public void Add(ParameterDefinition parameter)
	{
		if (parameter is null)
			throw new ArgumentNullException(nameof(parameter));
		_parameters.Add(parameter.Name, parameter);
	}
}

public class ParameterDefinition
{
	public ParameterDefinition(string name, IValueRef? parameter = null, string[]? abbrev = null, string? valueName = null, string? description = null, bool positional = false, bool required = false)
	{
		Name = name?.Trim().Replace(' ', '-') ?? throw new ArgumentNullException(nameof(name));
		Parameter = parameter;
		Abbrev = abbrev;
		ValueName = valueName;
		Description = description;
		IsPositional = positional;
		IsRequired = required;
	}

	public string? Value { get; set; }
	public IValueRef? Parameter { get; }
	public string Name { get; }
	public virtual Type Type => typeof(string);
	public bool IsSwitch => Type == typeof(bool);
	public string[]? Abbrev { get; }
	public string? ValueName { get; }
	public string? Description { get; }
	public bool IsPositional { get; }
	public bool IsRequired { get; }
	public bool IsCollection => Type.IsArray;
	public string PrintName => IsPositional ? $"<{Name}>": $"-{Name}";

	public string GetParameterName(bool includeValueName = false)
	{
		var text = new StringBuilder();
		if (!IsPositional)
			text.Append('-');
		text.Append(Name);

		if (Abbrev is { Length: > 0 })
		{
			char sep = '(';
			foreach (var a in Abbrev)
			{
				text.Append(sep)
					.Append(a);
				sep = '/';
			}
			text.Append(')');
		}
		if (!includeValueName)
			return text.ToString();
		var type = Type.GetElementType() ?? Type;
		if (ValueName is { Length: > 0 })
			text.Append(" <").Append(ValueName).Append('>');
		else if (type == typeof(bool))
			text.Append(" <y/n>");
		else
			text.Append(" <").Append(type.Name.ToLowerInvariant()).Append('>');
		if (IsCollection)
			text.Append("[,<...>]");
		return text.ToString();
	}

	public bool IsMatch(string value, StringComparison comparison)
		=> String.Equals(value, Name, comparison) || (Abbrev != null && Array.IndexOf(Abbrev, value) >= 0);

	public bool IsSimilar(string value, StringComparison comparison)
		=> IsSimilar(value.AsSpan(), Name.AsSpan(), Strings.SplitByCapitals(Name), 0, comparison);

	private static bool IsSimilar(ReadOnlySpan<char> value, ReadOnlySpan<char> name, IList<(int Index, int Length)> parts, int maskIndex, StringComparison comparison)
	{
		while (value.Length > 0 && IsDelimiter(value[0]))
			value = value.Slice(1);
		if (value.Length == 0)
			return maskIndex == parts.Count;
		ReadOnlySpan<char> mask = name.Slice(parts[maskIndex].Index, parts[maskIndex].Length);
		if (IsDelimiters(mask))
			return IsSimilar(value, name, parts, maskIndex + 1, comparison);
		if (maskIndex == parts.Count - 1)
			return mask.StartsWith(value, StringComparison.OrdinalIgnoreCase);

		for (int i = 1; i <= mask.Length; ++i)
		{
			if (i > value.Length)
				return false;
			if (!mask.StartsWith(value.Slice(0, i), comparison))
				return false;
			if (IsSimilar(value.Slice(i), name, parts, maskIndex + 1, comparison))
				return true;
		}
		return false;

		static bool IsDelimiters(ReadOnlySpan<char> value)
		{
			for (int i = 0; i < value.Length; ++i)
				if (!IsDelimiter(value[i]))
					return false;
			return true;
		}

		static bool IsDelimiter(char value)
			=> value is '-' or '_' or ' ';
	}

}

public class ParameterDefinition<T>: ParameterDefinition
{
	public ParameterDefinition(string name, Ref<T>? parameter, string[]? abbrev = null, string? valueName = null, Parameters.Parser<T>? parser = null, Parameters.Validator<T>? validator = null, string? description = null, bool positional = false, bool required = false)
		: base(name, parameter, abbrev, valueName, description, positional, required)
	{
		Parameter = parameter;
		Parser = parser ?? XmlTools.TryGetValue<T>;
		Validator = validator;
	}

	public override Type Type => typeof(T);

	public new Ref<T>? Parameter { get; }

	public Parameters.Parser<T> Parser { get; }

	public Parameters.Validator<T>? Validator { get; }

	public bool TryParse(string value, [MaybeNullWhen(false)] out T result)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));
		return Parser(value, out result);
	}

	public string? Validate(T value) => Validator?.Invoke(value);
}
