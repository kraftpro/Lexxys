using System.Reflection;

namespace Lexxys;

public class ArgumentCommand
{
	public ArgumentCommand(CommandDefinition definition)
	{
		Parameters = new ArgumentParameterCollection(definition.Parameters);
		Definition = definition;
	}

	public CommandDefinition Definition { get; }

	public ArgumentParameterCollection Parameters { get; }

	public ArgumentCommand? Command { get; internal set; }

	public string Name => Definition.Name;

	//public object Parse(Type type, out object? result)
	//{
	//	if (!type.IsClass || type == typeof(string))
	//	{
	//		result = null;
	//		return false;
	//	}

	//	result = Activator.CreateInstance(type) ?? throw new ArgumentOutOfRangeException(nameof(type), type, null);
	//	var parameters = new List<(CliParamAttribute? Atrribute, string Name, Type Type, Action<object?, object?> Setter)>();
	//	var commands = new List<(CliCommandAttribute? Attribute, string Name, Type Type, Action<object?, object?> Setter)>();

	//	foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty))
	//	{
	//		if (!item.CanWrite || item.GetSetMethod() == null || item.GetIndexParameters().Length != 0)
	//			continue;
	//		var cmd = item.GetCustomAttribute<CliCommandAttribute>();
	//		if (cmd != null || item.PropertyType.DeclaringType == type)
	//			commands.Add((cmd, item.Name, item.PropertyType, item.SetValue));
	//		else
	//			parameters.Add((item.GetCustomAttribute<CliParamAttribute>(), item.Name, item.PropertyType, item.SetValue));
	//	}
	//	foreach (var item in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField))
	//	{
	//		if (item.IsInitOnly || item.IsLiteral)
	//			continue;
	//		var cmd = item.GetCustomAttribute<CliCommandAttribute>();
	//		if (cmd != null || item.FieldType.DeclaringType == type)
	//			commands.Add((cmd, item.Name, item.FieldType, item.SetValue));
	//		else
	//			parameters.Add((item.GetCustomAttribute<CliParamAttribute>(), item.Name, item.FieldType, item.SetValue));
	//	}

	//	var comparison = Definition.Comparison;
	//	foreach (ArgumentParameter item in Parameters)
	//	{
	//		var field = parameters.FirstOrDefault(o => String.Equals(o.Name, item.Name, comparison));
	//		if (field.Type == null) continue;
	//		if (Strings.TryGetValue((string?)item.Value, field.Type, out var value))
	//			field.Setter(result, value);
	//		else
	//			commands.Add((null, item.Name, field.Type, field.Setter));
	//	}
	//	if (Command != null)
	//	{
	//		var command = commands.FirstOrDefault(o => String.Equals(o.Name, Command.Name, comparison));
	//		if (command.Type != null)
	//			command.Setter(result, Command.Parse(command.Type));
	//	}
	//	return result;
	//}
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CliArgumentsAttribute: Attribute
{
	public bool IgnoreCase { get; init; }
	public bool AllowSlash { get; init; }
	public bool StrictDoubleDash { get; init; }
	public bool DoubleDashSeparator { get; init; }
	public bool IgnoreNameSeparators { get; init; }
	public bool AllowUnknown { get; init; }
	public bool SplitPositional { get; init; }
	public bool CombineOptions { get; init; }
	public bool ColonSeparator { get; init; } = true;
	public bool EqualSeparator { get; init; } = true;
	public bool BlankSeparator { get; init; } = true;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CliCommandAttribute: Attribute
{
	public string[]? Alias { get; init; }
	public string? Description { get; init; }

	public CliCommandAttribute() { }

	public CliCommandAttribute(params string[]? alias) => Alias = alias;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CliParamAttribute: Attribute
{
	public string[]? Alias { get; init; }
	public string? ValueName { get; init; }
	public string? Description { get; init; }
	public bool Required { get; init; }
	public bool Positional { get; init; }

	public CliParamAttribute() { }

	public CliParamAttribute(params string[]? alias) => Alias = alias;
}

#pragma warning disable CA2252

public interface ICliOption<T>
{
	static abstract ArgumentsBuilder Build(ArgumentsBuilder? builder = null);
	static abstract T Parse(ArgumentCommand c);
}

public static class ICliOptionExtensions
{
	public static ArgumentsBuilder AddCommand<T>(this ArgumentsBuilder builder, string name, string description) where T: ICliOption<T>
	{
		builder.BeginCommand(name, description);
		T.Build(builder);
		return builder.EndCommand();
	}
}