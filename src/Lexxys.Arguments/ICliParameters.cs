namespace Lexxys;

#if NET

/// <summary>
/// Describes a generated command-line option model with static builder and parser members.
/// </summary>
/// <typeparam name="T">The option model type.</typeparam>
public interface ICliParameters<T>: ICliOptionBase<T>
{
	/// <summary>
	/// Creates an option model instance from a parsed command.
	/// </summary>
	/// <param name="cmd">The parsed command to bind.</param>
	/// <param name="error">An optional collection that receives binding errors.</param>
	/// <returns>The populated option model.</returns>
	static abstract T Parse(IArguments cmd, ICollection<string>? error = null);
}

#endif
