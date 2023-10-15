namespace Lexxys;

public static class UriExtensions
{
	public static KeyValuePair<string, string>[] SplitQuery(this Uri uri)
	{
		if (uri is null)
			throw new ArgumentNullException(nameof(uri));
		var query = uri.Query;
		if (query.Length <= 1)
			return [];
		var parts = query.Split(_querySeparator, StringSplitOptions.RemoveEmptyEntries);
		parts[0] = parts[0].Substring(1);
		var result = new KeyValuePair<string, string>[parts.Length];
		for (int i = 0; i < parts.Length; ++i)
		{
			var s = parts[i];
			var j = s.IndexOf('=');
			if (j < 0)
				result[i] = new KeyValuePair<string, string>(Uri.UnescapeDataString(s), String.Empty);
			else
				result[i] = new KeyValuePair<string, string>(Uri.UnescapeDataString(s.Substring(0, j)), Uri.UnescapeDataString(s.Substring(j + 1)));
		}
		return result;
	}
	private static readonly char[] _querySeparator = ['&'];
}
