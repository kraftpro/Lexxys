using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys
{
	public static class UriExtensions
	{
		public static KeyValuePair<string, string?>[] SplitQuery(this Uri uri)
		{
			var query = uri.Query;
			if (query.Length <= 1)
				return Array.Empty<KeyValuePair<string, string?>>();
			var parts = query.Split(_querySeparator, StringSplitOptions.RemoveEmptyEntries);
			parts[0] = parts[0].Substring(1);
			var result = new KeyValuePair<string, string?>[parts.Length];
			for (int i = 0; i < parts.Length; ++i)
			{
				var s = parts[i];
				var j = s.IndexOf('=');
				if (j < 0)
					result[i] = new KeyValuePair<string, string?>(Uri.UnescapeDataString(s), null);
				else
					result[i] = new KeyValuePair<string, string?>(Uri.UnescapeDataString(s.Substring(0, j)), Uri.UnescapeDataString(s.Substring(j + 1)));
			}
			return result;
		}
		private static readonly char[] _querySeparator = new char[] { '&' };
	}
}
