namespace Lexxys;

/// <summary>
/// Command line arguments parser with the parsed option value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Parsed option type.</typeparam>
public class Arguments<T>: Arguments where T : class, new()
{
	public Arguments(Arguments arguments, T value, IReadOnlyCollection<string>? error = null): base(arguments, error) => ArgumentValue = value;

	/// <summary>
	/// Gets the parsed option value.
	/// </summary>
	public T ArgumentValue { get; init; }
}

public class ParsedArguments<T>
{
	public ParsedArguments(Arguments arguments, T value, IReadOnlyCollection<string>? error = null)
	{
		Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
		Value = value ?? throw new ArgumentNullException(nameof(value));
		if (error is { Count: >0 })
			Arguments.Errors.AddRange(error);
	}

	public Arguments Arguments { get; }

	public T Value { get; }

	public IList<string>? Errors => Arguments.Errors;

	public bool HasErrors => Arguments.HasErrors;

	public bool HelpRequested => Arguments.HelpRequested;

	public void Usage(string? application = null, bool brief = false, int width = 0, bool alignAbbreviation = false, bool excludePositional = false, TextWriter? writer = null)
		=> Arguments.Usage(application, brief, width, alignAbbreviation, excludePositional, writer);

}
