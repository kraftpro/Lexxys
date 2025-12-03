using System.Collections;

namespace Lexxys;

internal record struct ParameterInfo(CliCommandAttribute? Cmd, CliOptionAttribute? Prm, string Name, Type Type, Action<object, object?> Setter);

public partial class Arguments: IArguments, IDump
{
	public const string PositionalKey = "";
	private readonly ArgumentCommand _command;
	private readonly ArgumentsDictionary _combined;
	private readonly List<string> _messages;

	private Arguments(ArgumentCommand command, List<string>? messages, bool helpRequested)
	{
		if (command is null) throw new ArgumentNullException(nameof(command));

		_command = command;
		_combined = CombineParameters(command);
		_messages = messages ?? [];
		HelpRequested = helpRequested;
	}

	public ArgumentsConfig Config => _command.Config;

	public int Count => _combined.Count;

	public ArgumentCommand? Command => _command.Command;

	string IArguments.Name => _command.Name;

	IArguments? IArguments.Command => _command.Command;

	public ParameterValue this[string name] => _combined[name];

	/// <summary>
	/// Unnamed positional parameters collection, accessible via the empty string key.
	/// If the command definition does not include a positional parameter definition,
	/// this will contain all parameters that do not match any named parameter definitions.
	/// </summary>
	public ParameterValue Positional => this[PositionalKey];

	public bool HasErrors => _messages.Count > 0;

	public IList<string> Errors => _messages;

	public bool HelpRequested { get; }

	CommandDefinition IArguments.Definition => _command.Definition;

	public bool Contains(string name) => _combined.ContainsKey(name);

	public Dictionary<string, ParameterValue>.Enumerator GetEnumerator() => _combined.GetEnumerator();

	IEnumerator<KeyValuePair<string, ParameterValue>> IEnumerable<KeyValuePair<string, ParameterValue>>.GetEnumerator() => GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void DumpContent(IDumpWriter writer)
	{
		writer.Begin('{', "arguments");
		writer.BeginObject("parameters");
		WriteParameters(writer, _command);
		writer.End();
		writer.Write("config", Config);
		if (HelpRequested)
			writer.Write("helpRequested", HelpRequested);
		if (_messages.Count > 0)
			writer.Write("messages", _messages);
		writer.End();
		AddCommand(writer, _command.Command);

		static void AddCommand(IDumpWriter writer, ArgumentCommand? cmd)
		{
			if (cmd == null)
				return;
			writer.Begin('{', cmd.Name);
			WriteParameters(writer, cmd);
			AddCommand(writer, cmd.Command);
			writer.End();
		}

		static void WriteParameters(IDumpWriter writer, ArgumentCommand cmd)
		{
			foreach (var item in cmd.Parameters)
			{
				if (item.Value.IsArray)
					writer.Write(item.Key, item.Value.ArrayValue!);
				else
					writer.Write(item.Key, item.Value.ToString());
			}
		}
	}


	#region Find parameters

	private static (ParameterDefinitionFindResult Result, ParameterDefinition? Value) FindParam(IReadOnlyCollection<ParameterDefinition> parameters, string value, StringComparison comparison, ParameterMatching matching)
	{
		ParameterDefinition? result = parameters.FirstOrDefault(o => String.Equals(value, o.Name, comparison) || o.Abbreviation.Any(a => String.Equals(value, a, comparison)));
		if (result != null)
			return (ParameterDefinitionFindResult.Found, result);
		if (matching == ParameterMatching.Strict)
			return (ParameterDefinitionFindResult.NotFound, null);

		bool found = false;
		bool fluent = matching == ParameterMatching.Fluent;
		foreach (var item in parameters)
		{
			if (CloseEquals(value, item.Name, comparison, fluent || !HasDelimiter(item.Name)))
			{
				if (found)
					return (ParameterDefinitionFindResult.Ambiguous, null);
				found = true;
				result = item;
			}
		}
		return found ? (ParameterDefinitionFindResult.Found, result) : (ParameterDefinitionFindResult.NotFound, null);
	}

	private static bool HasDelimiter(ReadOnlySpan<char> value) => value.IndexOfAny(__delimiters) >= 0;
	private static readonly char[] __delimiters = ['-', '_', '.', ' '];
	private static bool IsDelimiter(char value) => value is '-' or '_' or '.' or ' ';

	private static bool IsCaseSensitive(StringComparison comparison) => ((int)comparison & 1) == 0;

	private static bool CloseEquals(ReadOnlySpan<char> value, ReadOnlySpan<char> name, StringComparison comparison, bool fluent)
		=> CloseEquals(value, name, IsCaseSensitive(comparison), fluent);

	private static bool CloseEquals(ReadOnlySpan<char> value, ReadOnlySpan<char> name, bool caseSensitive, bool fluent)
		=> value.Length != 0 &&
			name.Length != 0 &&
			SameChar(value[0], name[0], caseSensitive) &&
			CloseEquals(value, new WordSlider(name), caseSensitive, fluent);

	private static bool CloseEquals(ReadOnlySpan<char> v, WordSlider n, bool caseSensitive, bool fluent)
	{
		if (n.Length == 0)
			return v.Length == 0;
		if (v.Length == 0)
			return n.SkipWord().Length == 0;
		if (IsDelimiter(v[0]))
			return CloseEquals(SkipDelimiters(v[1..]), n, caseSensitive, fluent) || CloseEquals(SkipDelimiters(v[1..]), n.SkipWord(), caseSensitive, fluent);
		if (n.AtDelimiter())
			return fluent && CloseEquals(v, n.SkipChar(), caseSensitive, fluent);
		if (SameChar(v[0], n.Value[0], caseSensitive))
			return CloseEquals(v[1..], n.SkipChar(), caseSensitive, fluent) || fluent && CloseEquals(v[1..], n.SkipChar().SkipWord(), caseSensitive, fluent);
		return false;
	}

	private static ReadOnlySpan<char> SkipDelimiters(ReadOnlySpan<char> v)
	{
		while (v.Length > 0 && IsDelimiter(v[0]))
			v = v[1..];
		return v;
	}

	private static bool SameChar(char a, char b, bool caseSensitive) => a == b || (!caseSensitive && char.ToUpperInvariant(a) == char.ToUpperInvariant(b));

	private readonly ref struct WordSlider
	{
		private readonly ReadOnlySpan<char> _value;
		private readonly bool _bow = true;

		public WordSlider(ReadOnlySpan<char> value) => _value = value;

		private WordSlider(ReadOnlySpan<char> value, bool bow)
		{
			_value = value;
			_bow = bow;
		}

		public int Length => _value.Length;

		public WordSlider SkipChar() => _value.Length == 0 ? this : new WordSlider(_value[1..], IsDelimiter(_value[0]));

		public bool AtDelimiter() => _value.Length > 0 && IsDelimiter(_value[0]);

		public ReadOnlySpan<char> Value => _value;

		public WordSlider SkipWord()
		{
			if (_bow || _value.Length == 0) return this;

			var v = _value;
			if (v[0] is >= '0' and <= '9')
			{
				do
				{
					v = v[1..];
				} while (v.Length > 0 && v[0] is >= '0' and <= '9');
			}
			else
			{
				int i = 0;
				while (i < v.Length && IsUpper(v[i]))
				{
					++i;
				}
				if (i > 1)
				{
					v = v[(i - 1)..];
				}
				else
				{
					while (v.Length > 0 && IsLower(v[0]))
					{
						v = v[1..];
					}
				}
			}
			while (v.Length > 0 && IsDelimiter(v[0]))
			{
				v = v.Slice(1);
			}
			return new WordSlider(v, true);
		}

		private static bool IsLower(char c) => Char.ToUpperInvariant(c) != c;
		private static bool IsUpper(char c) => Char.ToLowerInvariant(c) != c;

	}

	#endregion
}