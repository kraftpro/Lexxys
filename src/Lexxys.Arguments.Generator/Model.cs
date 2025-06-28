namespace Lexxys.Arguments.Generator;

public record CliArgumentsModel(string? IgnoreCase, string? AllowSlash, string? StrictDoubleDash, string? DoubleDashSeparator, string? IgnoreNameSeparators, string? AllowUnknown, string? SplitPositional, string? CombineOptions, string? ColonSeparator, string? EqualSeparator, string? BlankSeparator)
{
	public CliArgumentsModel(): this(null, null, null, null, null, null, null, null, null, null, null) { }
}

public record CliCommandModel(string? Name, string[] Alias, string? Description)
{
	public CliCommandModel(string? name, string? description): this(name, [], description)
	{
	}
}

public record CliParamModel(string? Name, string[] Alias, string? ValueName, string? Description, string? Required, string? Positional)
{
	public CliParamModel(string? name, string? valueName, string? description, string? required, string? positional):
		this(name, [], valueName, description, required, positional)
	{
	}
}

public record ArgumentClassModel(string Name, string NameSpace, CliArgumentsModel? Attribute = null)
{
	public List<ArgumentPropertyModel> Properties { get; } = [];

	public string FullName => $"{NameSpace}+{Name}";
}

public class ArgumentPropertyModel
{
	private bool? _isCommand;

	public ArgumentPropertyModel() { }
	
	public ArgumentPropertyModel(string? name, string? type, CliParamModel? paramAttribute = null, CliCommandModel? commandAttribute = null)
	{
		Name = paramAttribute?.Name ?? commandAttribute?.Name ?? name;
		Type = type;
		ParamAttribute = paramAttribute;
		CommandAttribute = commandAttribute;
	}

	public string? Name { get; init; }
	public string? Type { get; init; }
	public CliParamModel? ParamAttribute { get; init; }
	public CliCommandModel? CommandAttribute { get; init; }
	
	public bool IsCommand
	{
		get => _isCommand ?? CommandAttribute != null;
		set => _isCommand = value;
	}
}
