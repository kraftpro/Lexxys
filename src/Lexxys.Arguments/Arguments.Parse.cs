using System.Reflection;

namespace Lexxys;

public partial class Arguments
{
	/// <summary>
	/// Parses the command line arguments and returns the parsed option value of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Option type.</typeparam>
	/// <param name="args">Command line arguments.</param>
	/// <returns></returns>
	public static Arguments<T> Parse<T>(IEnumerable<string> args) where T: class, new()
	{
		Parse(args, typeof(T), out var arguments, out var value);
		return new Arguments<T>(arguments, (T)value);
	}

	//public static Arguments<T> Create<T>(IEnumerable<string> args) where T: ICliOption<T>
	//{
	//	var build = T.Build();
	//	var x = T.Parse(T.Build(new ArgumentsBuilder()).Build(args).Container);
	//}

	private static void Parse(IEnumerable<string> args, Type type, out Arguments arguments, out object value)
	{
		if (!type.IsClass || type == typeof(string)) throw new ArgumentOutOfRangeException(nameof(type), type, null);

		var builder = new ArgumentsBuilder().Use(type);
		var arg = new Arguments(args, builder);
		var errors = new List<string>();
		var val = ConstructValue(arg.Root, type, errors);
		foreach (var item in errors)
		{
			arg.Errors.Add(item);
		}
		arguments = arg;
		value = val;
	}

	private static object ConstructValue(ArgumentCommand command, Type type, List<string> errors)
	{
		var value = Activator.CreateInstance(type) ?? throw new ArgumentOutOfRangeException(nameof(type), type, null);
		var items = GetProperties(type);
		StringComparison comparison = command.Definition.Comparison;
		foreach (var item in command.Parameters)
		{
			var field = items.FirstOrDefault(o => String.Equals(o.Name, item.Name, comparison));
			if (field.Type == null) continue;
			if (Strings.TryGetValue((string?)item.Value, field.Type, out var v))
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

	private record struct ParameterDef(CliCommandAttribute? Cmd, CliParamAttribute? Prm, string Name, Type Type, Action<object, object?> Setter);

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
}
