namespace Lexxys;

public interface IBlobNameFormatter
{
	string CreateName(string? id, string? type = null, IDictionary<string, object?>? metadata = null);
}
