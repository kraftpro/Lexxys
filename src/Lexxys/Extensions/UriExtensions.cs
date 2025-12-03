namespace Lexxys;

public static class UriExtensions
{
	/// <summary>
	/// Splits the query component of the specified URI into an array of key-value pairs representing the query parameters.
	/// </summary>
	/// <remarks>Parameter names and values are decoded using URI decoding. If the query component is empty or does
	/// not contain any parameters, the method returns an empty array.</remarks>
	/// <param name="uri">The URI from which to extract the query parameters. This parameter must not be null.</param>
	/// <returns>An array of key-value pairs containing the names and values of the query parameters. Returns an empty array if the
	/// URI does not contain any query parameters.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the uri parameter is null.</exception>
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
