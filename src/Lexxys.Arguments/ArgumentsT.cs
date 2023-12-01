namespace Lexxys;

/// <summary>
/// Command line arguments parser with the parsed option value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Parsed option type.</typeparam>
public class Arguments<T>: Arguments where T : class, new()
{
	internal Arguments(Arguments arguments, T option): base(arguments) => Option = option;

	/// <summary>
	/// Gets the parsed option value.
	/// </summary>
	public T Option { get; init; }
}
