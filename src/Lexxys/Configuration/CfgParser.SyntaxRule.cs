namespace Lexxys.Configuration;

public ref partial struct CfgParser
{
	private class SyntaxRuleCollection
	{
		private readonly List<SyntaxRule> _rule = [];

		public void Add(List<string> path, string pattern, List<Node> parameters, StringComparer comparer)
		{
			if (pattern == "..")
			{
				if (_rule.Count == 0)
					throw new ArgumentOutOfRangeException(nameof(pattern), pattern, null);
				_rule[^1].Append(parameters);
				return;
			}

			path = [.. path];
			foreach (string item in pattern.Split(['\\', '/']))
			{
				var s = item.Trim();
				if (s.Length > 0 && s != ".")
					path.Add(s);
			}

			var rule = new SyntaxRule([.. path], [.. parameters], true);
			_rule.Add(rule);
		}

		/// <summary>
		/// Find the first rule which can be used with specified root path and node name.
		/// </summary>
		/// <param name="root">Root path to find rule</param>
		/// <param name="dash"></param>
		/// <param name="comparer"></param>
		/// <returns>The first applicable rule</returns>
		public SyntaxRule? Find(List<string> root, bool dash, StringComparer comparer)
		{
			for (int i = _rule.Count - 1; i >= 0; --i)
			{
				if (_rule[i].TryMatch(root, dash, comparer))
					return _rule[i];
			}
			return null;
		}

		public void Clear() => _rule.Clear();
	}

	private class SyntaxRule
	{
		string[] _path;
		Node[] _attrib;
		readonly bool _permanent;

		public SyntaxRule(string[] path, Node[] attrib, bool permanent)
		{
			_path = path;
			_attrib = attrib;
			_permanent = permanent;
		}

		//private static SyntaxRule? Create(string? path, string? node, string[]? attrib, bool ignoreCase, bool extractName)
		//{
		//	if (node == null || (node = node.Trim(TrimmerChars)).Length == 0)
		//		return null;
		//	attrib ??= [];
		//	path ??= "";

		//	string? name = null;
		//	if (extractName && (name = NameRex.Match(node).Value).Length > 1)
		//	{
		//		node = node.Substring(name.Length).Trim();
		//		if (node.Length == 0)
		//			node = ".";

		//		return new SyntaxRule(name.Substring(0, name.Length - 1), node, attrib, ignoreCase);
		//	}

		//	if ((path = path.Trim(TrimmerChars)).Length == 0)
		//		path = node;
		//	else
		//		path += "/" + node;

		//	int i = path.LastIndexOfAny(StarOrSlash);
		//	if (i < 0)
		//	{
		//		name = path;
		//		path = "";
		//	}
		//	else if (i < path.Length - 1 && path[i] == '/')
		//	{
		//		name = path.Substring(i + 1).TrimStart(TrimmerChars);
		//		path = path.Substring(0, i + 1).TrimEnd(TrimmerChars);
		//	}

		//	string start;
		//	Regex? pattern;
		//	bool permanent = false;
		//	if (path.IndexOfAny(Star) < 0)
		//	{
		//		pattern = null;
		//		start = path;
		//	}
		//	else
		//	{
		//		i = -1;
		//		// (\*)+|\((\*+)\)
		//		string patternString = PrepareRex.Replace(Regex.Escape(path), m =>
		//		{
		//			if (i == -1)
		//				i = m.Index;
		//			string s;
		//			if (m.Value[1] == '(')
		//			{
		//				s = m.Value.Length == 6 ? "[^/]*": ".*";
		//				permanent = true;
		//			}
		//			else
		//			{
		//				s = m.Value.Length == 2 ? "([^/]*)": "(.*)";
		//			}
		//			return s;
		//		});
		//		pattern = new Regex(@"\A" + patternString + @"\z", ignoreCase ? RegexOptions.IgnoreCase: RegexOptions.None);
		//		start = path.Substring(0, i).TrimEnd(TrimmerChars);
		//	}

		//	return new SyntaxRule(name, attrib, start, pattern, ignoreCase, permanent);
		//}
		//private static readonly char[] Star = ['*'];
		//private static readonly char[] StarOrSlash = ['*', '/'];
		//private static readonly char[] TrimmerChars = ['/', ' ', '\t'];
		//private static readonly Regex NameRex = new Regex(@"^[a-zA-Z0-9~!@$&+=_-]+:");
		//private static readonly Regex PrepareRex = new Regex(@"(\\\*)+|\\\((\\\*)+\\\)");

		public SyntaxRule? CreateFromTemplate(string[] path)
		{
			return new SyntaxRule(path, _attrib, _permanent);
		}

		public Node[] Attrib => _attrib;

		// public string ItemName { get; }

		public void Append(Node value)
		{
			Node[] attrib = new Node[_attrib.Length + 1];
			Array.Copy(_attrib, attrib, _attrib.Length);
			attrib[attrib.Length - 1] = value;
			_attrib = attrib;
		}

		public bool PathEquals(IReadOnlyList<string> path, StringComparer comparer)
		{
			if (_path.Length != path.Count)
				return false;

			for (int i = 0; i < _path.Length; ++i)
			{
				if (!comparer.Equals(_path[i], path[i]))
					return false;
			}
			return true;
		}

		public void Append(List<Node> value)
		{
			if (value is not { Count: >0 })
				return;
			Node[] attrib = new Node[_attrib.Length + value.Count];
			Array.Copy(_attrib, attrib, _attrib.Length);
			value.CopyTo(attrib, _attrib.Length);
			_attrib = attrib;
		}

		//public bool IsApplicable(string path)
		//{
		//	if (path == null)
		//		throw new ArgumentNullException(nameof(path));
		//	if (_start == null)
		//		return false;
		//	path = path.Trim(TrimmerChars);
		//	if (_pattern == null)
		//		return String.Equals(path, _start, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal);

		//	return _start.Length < path.Length ?
		//		path.StartsWith(_start, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal):
		//		_start.StartsWith(path, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal);
		//}

		public bool TryMatch(List<string> path, bool dash, StringComparer comparer) => TryMatch(new ReadOnlyListSegment<string>(path), _path, dash, comparer);

		// public bool TryMatch(List<string> path, bool dash) => TryMatch(path, 0, _path, dash);

		//private bool TryMatch(List<string> value, int index, ReadOnlySpan<string> pattern, bool dash, StringComparison comparison)
		//{
		//	while (!pattern.IsEmpty)
		//	{
		//		var item = pattern[0];
		//		if (dash && pattern.Length == 1)
		//			return item is not "*" or "**";
		//		if (item == "**")
		//			return TryMatch(value, index, pattern.Slice(1), dash, comparison) ||
		//				index < value.Count && TryMatch(value, index + 1, pattern, dash, comparison);
		//		if (index >= value.Count || !(value[index] == "*" || String.Equals(value[index], pattern[0], comparison)))
		//			return false;
		//		++index;
		//		pattern = pattern.Slice(1);
		//	}
		//	return !dash && value.Count == index;
		//}

		private bool TryMatch(ReadOnlyListSegment<string> value, ReadOnlySpan<string> pattern, bool dash, StringComparer comparer)
		{
			while (!pattern.IsEmpty)
			{
				var item = pattern[0];
				if (dash && pattern.Length == 1)
					return value.Count == 1 && item is not "*" or "**";
				if (item == "**")
					return TryMatch(value, pattern.Slice(1), dash, comparer) ||
						!value.IsEmpty && TryMatch(value.Slice(1), pattern, dash, comparer);
				if (value.IsEmpty || !(item == "*" || comparer.Equals(value[0], item)))
					return false;
				value = value.Slice(1);
				pattern = pattern.Slice(1);
			}
			return !dash && value.IsEmpty;
		}
	}

	//internal readonly object GetSyntaxRuleCollectionForTest() => _syntaxRules;
}
