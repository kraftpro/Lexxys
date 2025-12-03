namespace Lexxys;

public class Arguments<T>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Arguments{T}"/> class.
	/// </summary>
	/// <param name="arguments">The parsed argument state.</param>
	/// <param name="value">The typed option model created from the parsed arguments.</param>
	/// <param name="error">Additional binding errors to append to <paramref name="arguments"/>.</param>
	/// <exception cref="ArgumentNullException"><paramref name="arguments"/> or <paramref name="value"/> is <c>null</c>.</exception>
	public Arguments(Arguments arguments, T value, IReadOnlyCollection<string>? error = null)
	{
		Args = arguments ?? throw new ArgumentNullException(nameof(arguments));
		Value = value ?? throw new ArgumentNullException(nameof(value));
		if (error is { Count: > 0 })
			Args.Errors.AddRange(error);
	}

	/// <summary>
	/// Gets the parsed argument state.
	/// </summary>
	public Arguments Args { get; }

	/// <summary>
	/// Gets the typed option model.
	/// </summary>
	public T Value { get; }

	/// <summary>
	/// Gets the parse and binding errors.
	/// </summary>
	public IList<string> Errors => Args.Errors;

	/// <summary>
	/// Gets a value indicating whether parsing or binding produced errors.
	/// </summary>
	public bool HasErrors => Args.HasErrors;

	/// <summary>
	/// Gets a value indicating whether a help option was present.
	/// </summary>
	public bool HelpRequested => Args.HelpRequested;

	/// <summary>
	/// Writes usage text for the parsed argument definition.
	/// </summary>
	/// <param name="application">The application name to show in the usage line.</param>
	/// <param name="brief">If <c>true</c>, omits command details.</param>
	/// <param name="width">The maximum output width, or <c>0</c> to use the console width.</param>
	/// <param name="alignAbbreviation">If <c>true</c>, aligns option names that have aliases with those that do not.</param>
	/// <param name="excludePositional">If <c>true</c>, excludes positional parameters from details.</param>
	/// <param name="writer">The destination writer, or <c>null</c> to use <see cref="Console.Out"/>.</param>
	public void Usage(string? application = null, bool brief = false, int width = 0, bool alignAbbreviation = false, bool excludePositional = false, TextWriter? writer = null)
		=> Args.Usage(application, brief, width, alignAbbreviation, excludePositional, writer);
}
