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

	public T Parse<T>() where T: class, new()
	{
		if (typeof(T) == typeof(string)) throw new ArgumentOutOfRangeException(nameof(T), typeof(T), null);

		var type = typeof(T);
		var commands = new List<(CliCommandAttribute Attribute, string Name, Type Type, Action<object?, object?> Setter)>();
		var attributes = new List<(CliParamAttribute? Atribute, string Name, Type Type, Action<object?, object?> Setter)>();

		foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty))
		{
			if (!item.CanWrite || item.GetSetMethod() == null || item.GetIndexParameters().Length != 0)
				continue;
			var cmd = item.GetCustomAttribute<CliCommandAttribute>();
			if (cmd != null)
				commands.Add((cmd, item.Name, item.PropertyType, item.SetValue));
			else
				attributes.Add((item.GetCustomAttribute<CliParamAttribute>(), item.Name, item.PropertyType, item.SetValue));
		}
		foreach (var item in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField))
		{
			if (item.IsInitOnly || item.IsLiteral)
				continue;
			var cmd = item.GetCustomAttribute<CliCommandAttribute>();
			if (cmd != null)
				commands.Add((cmd, item.Name, item.FieldType, item.SetValue));
			else
				attributes.Add((item.GetCustomAttribute<CliParamAttribute>(), item.Name, item.FieldType, item.SetValue));
		}

		foreach (var item in attributes)
		{
			Parameters.TryFind(item.Name, out var pd, Definition.Parameters.Comparison);
		}
		return default!;
	}
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
