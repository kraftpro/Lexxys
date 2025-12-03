using System.Text;

namespace Lexxys;

public partial class Arguments
{
	#region Usage

	const int Indent = 2;
	const int MaxOffset = 32;

	/// <summary>
	/// Prints the usage string.
	/// </summary>
	/// <param name="application">Name of the application.</param>
	/// <param name="detailed">Include detailed command information.</param>
	/// <param name="width">Width of the output. (0 - screen width or 80; &lt;0 - no limit)</param>
	/// <param name="alignAbbreviation">Align parameters names with and without abbreviations</param>
	/// <param name="excludePositional">Exclude positional parameters</param>
	/// <param name="writer">An optional <see cref="TextWriter"/> (by default - <see cref="Console.Out"/>)</param>
	public void Usage(string? application = null, bool detailed = false, int width = 0, bool alignAbbreviation = false, bool excludePositional = false, TextWriter? writer = null)
	{
		writer ??= Console.Out;
		if (width == 0 && writer == Console.Out)
			width = Console.IsOutputRedirected ? int.MaxValue: Console.WindowWidth;

		width = FixWidth(width);
		var text = UsageString(application, width, new StringBuilder("Usage: "));
		if (_command.IsAuto)
		{
			writer.WriteLine(text);
			return;
		}

		text.AppendLine();

		if (_command.Command is null)
			UsageDetails(detailed, width, alignAbbreviation, excludePositional, text);
		else
			UsageCommand(_command.Command, detailed, width, alignAbbreviation, excludePositional, text);

		writer.Write(text);
	}

	/// <summary>
	/// Builds usage parameters and commands details.
	/// </summary>
	/// <param name="detailed">Include detailed command information</param>
	/// <param name="width">Width of the output. (0 - screen width or 80; &lt;0 - no limit)</param>
	/// <param name="alignAbbreviation">Align parameters names with and without abbreviations</param>
	/// <param name="excludePositional">Exclude positional parameters</param>
	/// <param name="text">The <see cref="StringBuilder"/></param>
	/// <returns></returns>
	public StringBuilder UsageDetails(bool detailed = false, int width = 0, bool alignAbbreviation = false, bool excludePositional = false, StringBuilder? text = null)
	{
		text ??= new StringBuilder();
		width = FixWidth(width);
		var offset = GetOffset(Indent, width);
		if (_command.Definition.Parameters.Any(o => !o.IsPositional))
			text.AppendLine().AppendLine("Parameters:");
		UsageParameters(text, _command.Definition.Parameters, Indent, offset, width, alignAbbreviation, !excludePositional);
		if (_command.Definition.Commands is not { Count: > 0 })
			return text;

		text.AppendLine().AppendLine("Commands:");
		bool nl = false;
		foreach (var item in _command.Definition.Commands!)
		{
			if (item.Name.Length == 0)
				continue;
			if (nl && detailed)
				text.AppendLine();
			UsageCommand(text, item, Indent, offset, detailed, true, width, alignAbbreviation, !excludePositional);
			nl = true;
		}
		return text;
	}

	/// <summary>
	/// Builds usage details text for the parameters and the specified <paramref name="command"/>.
	/// </summary>
	/// <param name="command">The <see cref="ArgumentCommand"/> to print.</param>
	/// <param name="detailed">Include detailed command information</param>
	/// <param name="width">Width of the output. (0 - screen width or 80; &lt;0 - no limit)</param>
	/// <param name="alignAbbreviation">Align parameters names with and without abbreviations</param>
	/// <param name="excludePositional">Exclude positional parameters</param>
	/// <param name="text">The <see cref="StringBuilder"/></param>
	/// <returns></returns>
	public StringBuilder UsageCommand(ArgumentCommand? command, bool detailed = false, int width = 0, bool alignAbbreviation = false, bool excludePositional = false, StringBuilder? text = null)
	{
		text ??= new StringBuilder();
		width = FixWidth(width);
		var offset = GetOffset(Indent, width);
		if (_command.Definition.Parameters.Any(o => !o.IsPositional))
			text.AppendLine().AppendLine("Parameters:");
		UsageParameters(text, _command.Definition.Parameters, Indent, offset, width, alignAbbreviation, !excludePositional);
		var indent = Indent;
		if (command is null)
			return text;

		text.AppendLine().AppendLine("Commands:");
		var cmd = command;
		bool nl = false;
		while (cmd != null)
		{
			if (nl && detailed)
				text.AppendLine();
			UsageCommand(text, cmd.Definition, indent, offset, detailed, false, width, alignAbbreviation, !excludePositional);
			nl = true;
			indent += Indent;
			cmd = cmd.Command;
		}
		return text;
	}

	/// <summary>
	/// Generates the usage string including selected command if present.
	/// </summary>
	/// <param name="application">Name of the application</param>
	/// <param name="width">Width of the output. (0 - screen width or 80; &lt;0 - no limit)</param>
	/// <param name="text">The <see cref="StringBuilder"/></param>
	/// <returns></returns>
	public StringBuilder UsageString(string? application = null, int width = 0, StringBuilder? text = null)
	{
		application ??= Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
		text ??= new StringBuilder();
		text.Append(application);
		if (_command.IsAuto)
			return text.Append(" [<options>]");

		width = FixWidth(width);

		var patch = new List<(int Index, int Length, string Value)>();
		int k0 = text.Length;

		UsageStringParameters(text, patch, k0, _command, " [<options>]");

		if (_command.Command is null)
		{
			if (_command.Definition.Commands is { Count: > 0 })
				text.Append(" <command> [<arguments>]");
		}
		else
		{
			var cmd = _command.Command;
			while (cmd is not null)
			{
				text.Append(' ').Append(cmd.Name);
				if (cmd.Definition.Parameters.Any())
					UsageStringParameters(text, patch, k0, cmd, " [<arguments>]");
				cmd = cmd.Command;
			}
		}

		if (text.Length <= width)
			return text;

		var line = new char[text.Length];
		text.CopyTo(k0, line, 0, text.Length - k0);
		text.Length = k0;
		int k = 0;
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

	private void UsageStringParameters(StringBuilder text, List<(int Index, int Length, string Value)> patch, int start, ArgumentCommand cmd, string options)
	{
		var (begin, end) = Split(cmd.Definition.Parameters);

		if (!(begin ^ end))
		{
			UsageParametersPart(text, cmd.Definition.Parameters, patch, start, options, true, true);
		}
		else if (begin)
		{
			UsageParametersPart(text, cmd.Definition.Parameters, patch, start, String.Empty, true, false);
			UsageParametersPart(text, cmd.Definition.Parameters, patch, start, options, false, true);
		}
		else
		{
			UsageParametersPart(text, cmd.Definition.Parameters, patch, start, options, false, true);
			UsageParametersPart(text, cmd.Definition.Parameters, patch, start, String.Empty, true, false);
		}
		return;

		static (bool, bool) Split(ParameterDefinitionCollection parameters)
		{
			bool begin = false;
			bool end = false;
			bool named = false;
			foreach (var item in parameters)
			{
				if (item.IsPositional)
					if (named)
						end = true;
					else
						begin = true;
				else if (!end)
					named = true;
				else
					return (false, false);
			}
			return (begin, end);
		}

		void UsageParametersPart(StringBuilder text, ParameterDefinitionCollection parameters, List<(int Index, int Length, string Value)> patch, int start, string options, bool positional, bool named)
		{
			int k = text.Length;
			foreach (var item in parameters)
			{
				if (item.IsPositional ? !positional: !named)
					continue;
				text.Append(' ');
				if (!item.IsRequired)
					text.Append('[');
				item.GetParameterName(text, argumentDelimiter: '=', longDash: Config.Strict, excludeAbbreviation: true);
				if (!item.IsRequired)
					text.Append(']');
			}
			int n = text.Length - k;
			if (n > options.Length)
				patch.Add((k - start, n, options));
		}
	}

	private static int FixWidth(int width) => width >= 60 ? width: width == 0 ? 80: width < 0 ? int.MaxValue: 60;

	private int GetOffset(int indent, int maxWidth)
	{
		var offset = 1 + GetOffset(indent, _command.Definition);
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

	private void UsageCommand(StringBuilder text, CommandDefinition cmd, int indent, int offset, bool detailed, bool commands, int maxWidth, bool alignAbbreviation, bool positional)
	{
		text.Append(' ', indent).Append(cmd.Name);
		AddDescription(text, cmd.Description, offset - indent - cmd.Name.Length, offset, maxWidth);

		if (!detailed)
			return;

		if (cmd.Parameters.Count > 0)
			UsageParameters(text, cmd.Parameters, indent + Indent, offset, maxWidth, alignAbbreviation, positional);

		if (!commands || cmd.Commands is not { Count: > 0 })
			return;

		foreach (var item in cmd.Commands!)
		{
			if (item.Name.Length == 0)
				continue;
			if (detailed)
				text.AppendLine();
			UsageCommand(text, item, indent + Indent, offset, detailed, commands, maxWidth, alignAbbreviation, positional);
		}
	}

	private void UsageParameters(StringBuilder text, ParameterDefinitionCollection parameters, int indent, int offset, int width, bool alignAbbreviation, bool positional)
	{
		if (!parameters.Any(o => !o.IsPositional))
			return;

		bool abbrev = alignAbbreviation && parameters.Any(o => o.HasAbbreviation);
		foreach (var item in positional ? parameters: parameters.Where(o => !o.IsPositional))
		{
			AddParameterInfo(text, item, indent, offset, abbrev, width);
		}

		void AddParameterInfo(StringBuilder text, ParameterDefinition parameter, int indent, int offset, bool abbrev, int width)
		{
			int n0 = text.Length;
			text.Append(' ', indent);

			if (abbrev && parameter is { Name.Length: > 1, IsPositional: false, HasAbbreviation: false })
				text.Append(' ', 4);

			parameter.GetParameterName(text, argumentDelimiter: '=', longDash: Config.Strict);
			int pad = offset - (text.Length - n0);
			AddDescription(text, parameter.Description, pad, offset, width);
		}
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
	private static readonly char[] NewLine = ['\r', '\n'];

	#endregion

}
