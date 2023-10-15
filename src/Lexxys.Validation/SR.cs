using System.Text;

namespace Lexxys.Validation;

internal class SR
{
	public static string CheckInvariantFailed(ValidationResults results = default, string? source = default)
	{
		string message = String.IsNullOrEmpty(source) ?
			(results.Success ? "Invariant check failed." : "Invariant check failed with message: \"{1}\".") :
			(results.Success ? "Invariant check failed in {0}." : "Check invariant failed in {0} with message: \"{1}\".");
		return String.Format(Lexxys.SR.Culture, message, source, results);
	}

	public static string ValidationFailed(ValidationResults validation = default)
	{
		if (validation.Success)
			return "Validation Failed.";
		var text = new StringBuilder();
		text.Append("Validation Failed. {");
		string prefix = "";
		foreach (var item in validation.Items)
		{
			if (item.Message == null)
				text.Append(prefix).Append(item.Field);
			else
				text.Append(prefix).Append(item.Field).Append(": \"").Append(item.Message).Append('"');
			prefix = "; ";
		}
		text.Append('}');
		return text.ToString();
	}
}
