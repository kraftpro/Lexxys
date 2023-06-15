namespace Lexxys.Validation.Errors;

internal class ErrorDefinition
{
	public int Id { get; }
	public string Code { get; }
	public string? MessageTemplate { get; }
	public IReadOnlyDictionary<string, object?>? Parameters { get; }

	public ErrorDefinition(int id, string code, string? messageTemplate, IReadOnlyDictionary<string, object?>? parameters)
	{
		Id = id;
		Code = code;
		MessageTemplate = messageTemplate;
		Parameters = parameters;
	}

	public string GetMessage() => MessageTemplate == null ? String.Empty: MessageTemplate.Format(Parameters);
}
