#if NET7_0_OR_GREATER

namespace Lexxys;

public class Arguments<T>: Arguments where T: ICliOption<T>
{
	public Arguments(IEnumerable<string> args): base(args, T.Build()) => Option = T.Parse(Container);

	public Arguments(IEnumerable<string> args, ArgumentsBuilder builder): base(args, builder) => Option = T.Parse(Container);

	public T Option { get; }
}

public partial class Arguments
{
	public static Arguments<T> Create<T>(IEnumerable<string> args) where T: ICliOption<T> => new Arguments<T>(args);
}

public interface ICliOption<out T>
{
	static abstract T Parse(ArgumentCommand command);
	static abstract ArgumentsBuilder Build(ArgumentsBuilder? builder = null);
}

public static class CliArgumentsExtensions
{
	public static ArgumentsBuilder AddCommand<T>(this ArgumentsBuilder builder, string name, string? description = null) where T: ICliOption<T>
	{
		builder.BeginCommand(name, description);
		return T.Build(builder).EndCommand();
	}
}

#endif
