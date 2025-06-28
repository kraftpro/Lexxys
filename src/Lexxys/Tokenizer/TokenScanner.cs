// Lexxys Infrastructural library.
// file: TokenScanner.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Tokenizer;

/// <summary>
/// Parser of the <see cref="CharStream"/> to the list of <see cref="LexicalToken"/>s.
/// </summary>
[Serializable]
public ref struct TokenScanner
{
	public delegate LexicalToken TokenParser(in TokenScanner scanner, ref CharStream stream);
	private const int HighAscii = 127;

	private readonly ITokenParser _parser;
	private readonly List<(bool Enabled, LexicalTokenRule Rule)> _rules;
	private int[] _asciiRules;
	private int[] _extraRules;

	/// <summary>
	/// Initializes a new <see cref="TokenScanner"/> with the list of the <see cref="LexicalTokenRule"/>s.
	/// </summary>
	/// <param name="rules">Collection of the <see cref="LexicalTokenRule"/>s.</param>
	public TokenScanner(params LexicalTokenRule[] rules): this(null, rules)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="TokenScanner"/> with the list of the <see cref="LexicalTokenRule"/>s.
	/// </summary>
	/// <param name="filters"></param>
	/// <param name="rules">Collection of the <see cref="LexicalTokenRule"/>s.</param>
	public TokenScanner(IEnumerable<ITokenFilter?>? filters, params LexicalTokenRule[] rules)
	{
		if (rules is null) throw new ArgumentNullException(nameof(rules));

		_parser = JoinFilters(filters);
		_rules = new List<(bool Enabled, LexicalTokenRule Rule)>(rules.Length);
		_rules.AddRange(rules.Select(o => (true, o)));
		_extraRules = new int [_rules.Count];
		_asciiRules = new int[(HighAscii + 1) * _rules.Count];
		ApplyRules();
	}

	public readonly List<(bool Enabled, LexicalTokenRule Rule)> Rules => _rules;

	public readonly bool EnableRule(Func<LexicalTokenRule, bool> predicate, bool enable)
	{
		bool result = false;
		for (int i = 0; i < _rules.Count; i++)
		{
			var (enabled, rule) = _rules[i];
			if (predicate(rule))
			{
				_rules[i] = (enable, _rules[i].Rule);
				result |= enabled != enable;
			}
		}
		return result;
	}

	public readonly void ApplyRules()
	{
		_asciiRules.AsSpan().Fill(-1);
		_extraRules.AsSpan().Fill(-1);
		int n = _rules.Count;
		int j = 0;
		for (int i = 0; i < n; ++i)
		{
			var rule = _rules[i].Rule;
			bool extra = rule.HasExtraBeginning;

			if (rule.BeginningChars != null)
			{
				foreach (char ch in rule.BeginningChars.AsSpan())
				{
					if (ch > HighAscii)
						extra = true;
					else
						_asciiRules[ch * n + i] = i;
				}
			}
			else
			{
				extra = true;
				for (int ch = 0; ch <= HighAscii; ++ch)
				{
					if (rule.TestBeginning((char)ch))
						_asciiRules[ch * n + i] = i;
				}
			}
			if (extra)
				_extraRules[j++] = i;
		}
	}

	public readonly void ResetParser() => _parser.Reset();

	/// <summary>
	/// Parses the next token.
	/// </summary>
	/// <returns>The parsed token value</returns>
	public readonly LexicalToken Next(ref CharStream stream)
	{
		LexicalToken token = _parser.GetNextToken(this, ref stream);
		return token.IsEmpty ? new LexicalToken(LexicalTokenType.EOF, stream.Position, 0): token;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private readonly LexicalToken GetFirstByRule(ref CharStream stream)
	{
		if (stream.Eof) return LexicalToken.Empty;

		var c = stream[0];

		ReadOnlySpan<int> items = c <= HighAscii ? _asciiRules.AsSpan().Slice(c * _extraRules.Length, _extraRules.Length): _extraRules.AsSpan();
		foreach (var item in items)
		{
			if (item < 0)
				continue;
			var rule = _rules[item];
			if (!rule.Enabled)
				continue;
			var token = rule.Rule.TryParse(ref stream);
			if (!token.IsEmpty)
				return token;
		}
		throw stream.SyntaxException($"Cannot find a rule for character ({(int)c})'{c}'");
	}

	private static ITokenParser JoinFilters(IEnumerable<ITokenFilter?>? tokenFilters)
	{
		ITokenParser result = RootParser.Instance;
		if (tokenFilters is null)
			return result;
		foreach (var filter in tokenFilters)
		{
			if (filter != null)
				result = new FilterChain(result, filter);
		}
		return result;
	}

	sealed class RootParser: ITokenParser
	{
		public static readonly ITokenParser Instance = new RootParser();

		private RootParser()
		{
		}

		LexicalToken ITokenParser.GetNextToken(in TokenScanner scanner, ref CharStream stream)
		{
			LexicalToken token;
			do
			{
				token = scanner.GetFirstByRule(ref stream);
			} while (token.Is(LexicalTokenType.IGNORE));
			return token;
		}

		void ITokenParser.Reset()
		{
		}
	}

	sealed class FilterChain: ITokenParser
	{
		private readonly ITokenParser _parser;
		private readonly ITokenFilter _filter;

		public FilterChain(ITokenParser parser, ITokenFilter filter)
		{
			_parser = parser;
			_filter = filter;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		LexicalToken ITokenParser.GetNextToken(in TokenScanner scanner, ref CharStream stream) => _filter.GetNextToken(_parser, in scanner, ref stream);

		void ITokenParser.Reset()
		{
			_filter.Reset();
			_parser.Reset();
		}
	}
}


