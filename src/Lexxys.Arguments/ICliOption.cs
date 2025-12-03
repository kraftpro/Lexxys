namespace Lexxys;

#if NET

public interface ICliOptionBase<T>
{
	/// <summary>
	/// Adds this option model's parameters and commands to an argument builder.
	/// </summary>
	/// <param name="builder">The builder to extend, or <c>null</c> to create a new builder.</param>
	/// <returns>The configured argument builder.</returns>
	static abstract ArgumentsBuilder CreateBuilder(ArgumentsBuilder? builder = null);
}

/// <summary>
/// Describes a generated command-line option model with static builder and parser members.
/// </summary>
/// <typeparam name="T">The option model type.</typeparam>
public interface ICliOption<T>: ICliOptionBase<T>
{
	/// <summary>
	/// Creates an option model instance from a parsed command.
	/// </summary>
	/// <param name="cmd">The parsed command to bind.</param>
	/// <returns>The populated option model.</returns>
	static abstract T Parse(IArguments cmd);
}

#endif
