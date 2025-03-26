using Lexxys;

using System.Collections.Generic;
using System.Reflection;

namespace Lexxys;

public partial class Arguments
{
	/// <summary>
	/// Parses the command line arguments and returns the parsed option value of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Option type.</typeparam>
	/// <param name="args">Command line arguments.</param>
	/// <param name="settings">Optional settings for the parser.</param>
	/// <returns></returns>
	public static ParsedArguments<T> Parse<T>(IEnumerable<string> args, ArgumentsBuilderSettings? settings = null) where T: class, new()
	{
		if (args is null) throw new ArgumentNullException(nameof(args));

		var parser = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, [typeof(IEnumerable<string>), typeof(ArgumentsBuilder)], null);
		if (parser?.ReturnType == typeof(ParsedArguments<T>))
		{
			var obj = parser.Invoke(null, [args, null]);
			if (obj is ParsedArguments<T> result)
				return result;
		}

		Parse(args, typeof(T), settings, out var arguments, out var value);
		return new ParsedArguments<T>(arguments, (T)value);
	}

	private static void Parse(IEnumerable<string> args, Type type, ArgumentsBuilderSettings? settings, out Arguments arguments, out object value)
	{
		if (!type.IsClass || type == typeof(string)) throw new ArgumentOutOfRangeException(nameof(type), type, null);

		var builder = new ArgumentsBuilder(settings).Use(type);
		var arg = new Arguments(args, builder);
		var errors = new List<string>();
		var val = ConstructValue(arg, type, errors);
		foreach (var item in errors)
		{
			arg.Errors.Add(item);
		}
		arguments = arg;
		value = val;
	}

	private static object ConstructValue(IArgumentCommand command, Type type, List<string> errors)
	{
		var value = Activator.CreateInstance(type) ?? throw new ArgumentOutOfRangeException(nameof(type), type, null);
		var items = GetProperties(type);
		StringComparison comparison = command.Definition.Comparison;
		foreach (var item in command.Parameters)
		{
			var field = items.FirstOrDefault(o => String.Equals(o.Name, item.Name, comparison));
			if (field.Type == null) continue;
			if (Strings.TryGetValue(item.Value, field.Type, out var v))
				field.Setter(value, v);
			else
				errors.Add($"Invalid value for parameter '{item.Name}'");
		}
		var cmd = command.Command;
		if (cmd == null) return value;

		var cmdField = items.FirstOrDefault(o => String.Equals(o.Name, cmd.Name, comparison));
		if (cmdField.Type == null) return value;

		var commandValue = ConstructValue(cmd, cmdField.Type, errors);
		cmdField.Setter(value, commandValue);
		return value;
	}

	private record struct ParameterDef(CliCommandAttribute? Cmd, CliOptionAttribute? Prm, string Name, Type Type, Action<object, object?> Setter);

	private static List<ParameterDef> GetProperties(Type type)
	{
		return type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
			.Where(o => o.CanWrite && o.GetSetMethod() != null && o.GetIndexParameters().Length == 0)
			.Select(o => new ParameterDef(o.GetCustomAttribute<CliCommandAttribute>(), o.GetCustomAttribute<CliOptionAttribute>(), o.Name, o.PropertyType, o.SetValue))
			.Union(type
				.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField)
				.Where(o => !o.IsInitOnly && !o.IsLiteral)
				.Select(o => new ParameterDef(o.GetCustomAttribute<CliCommandAttribute>(), o.GetCustomAttribute<CliOptionAttribute>(), o.Name, o.FieldType, o.SetValue))
				).ToList();
	}
}
