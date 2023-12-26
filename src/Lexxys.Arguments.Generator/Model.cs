namespace Lexxys.Arguments.Generator;

public class CliArgumentsModel
{
	public CliArgumentsModel() { }
	
	public CliArgumentsModel(string? ignoreCase, string? allowSlash, string? strictDoubleDash, string? doubleDashSeparator, string? ignoreNameSeparators, string? allowUnknown, string? splitPositional, string? combineOptions, string? colonSeparator, string? equalSeparator, string? blankSeparator)
	{
		IgnoreCase = ignoreCase;
		AllowSlash = allowSlash;
		StrictDoubleDash = strictDoubleDash;
		DoubleDashSeparator = doubleDashSeparator;
		IgnoreNameSeparators = ignoreNameSeparators;
		AllowUnknown = allowUnknown;
		SplitPositional = splitPositional;
		CombineOptions = combineOptions;
		ColonSeparator = colonSeparator;
		EqualSeparator = equalSeparator;
		BlankSeparator = blankSeparator;
	}

	public string? IgnoreCase { get; init; }
	public string? AllowSlash { get; init; }
	public string? StrictDoubleDash { get; init; }
	public string? DoubleDashSeparator { get; init; }
	public string? IgnoreNameSeparators { get; init; }
	public string? AllowUnknown { get; init; }
	public string? SplitPositional { get; init; }
	public string? CombineOptions { get; init; }
	public string? ColonSeparator { get; init; }
	public string? EqualSeparator { get; init; }
	public string? BlankSeparator { get; init; }
}

public class CliCommandModel
{
	public CliCommandModel() { }
	
	public CliCommandModel(string[]? alias, string? description)
	{
		Alias = alias ?? [];
		Description = description;
	}

	public string[] Alias { get; init; } = [];
	public string? Description { get; init; }
}

public class CliParamModel
{
	public CliParamModel() { }
	
	public CliParamModel(string[]? alias, string? valueName, string? description, string? required, string? positional)
	{
		Alias = alias ?? [];
		ValueName = valueName;
		Description = description;
		Required = required;
		Positional = positional;
	}

	public string[] Alias { get; init; } = [];
	public string? ValueName { get; init; }
	public string? Description { get; init; }
	public string? Required { get; init; }
	public string? Positional { get; init; }
}

public class ArgumentClassModel
{
	public ArgumentClassModel(string name, string nameSpace, CliArgumentsModel? attribute = null)
	{
		Name = name;
		NameSpace = nameSpace;
		Attribute = attribute;
	}

	public string Name { get; init; }
	public string NameSpace { get; init; }
	public List<ArgumentPropertyModel> Properties { get; } = [];
	public CliArgumentsModel? Attribute { get; init; }
	public string FullName => $"{NameSpace}+{Name}";
}

public class ArgumentPropertyModel
{
	private bool? _isCommand;

	public ArgumentPropertyModel() { }
	
	public ArgumentPropertyModel(string? name, string? type, CliParamModel? paramAttribute = null, CliCommandModel? commandAttribute = null)
	{
		Name = name;
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
