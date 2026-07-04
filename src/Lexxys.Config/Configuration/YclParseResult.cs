namespace Lexxys.Configuration;

public sealed class YclParseResult
{
	public YclParseResult(ConfigNodeCollection? value, IReadOnlyList<YclParseMessage> messages)
	{
		Value = value;
		Messages = messages;
	}

	public ConfigNodeCollection? Value { get; }
	public IReadOnlyList<YclParseMessage> Messages { get; }
	public bool IsSuccess => !Messages.Any(o => o.Severity == YclParseMessageSeverity.Error);
	public bool IsFailure => !IsSuccess;
	public IReadOnlyList<YclParseMessage> Warnings => Messages.Where(o => o.Severity == YclParseMessageSeverity.Warning).ToArray();
	public IReadOnlyList<YclParseMessage> Errors => Messages.Where(o => o.Severity == YclParseMessageSeverity.Error).ToArray();
}

public sealed record YclParseMessage(YclParseMessageSeverity Severity, string Message);

public enum YclParseMessageSeverity
{
	Warning,
	Error
}
