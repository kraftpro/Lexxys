// Lexxys Infrastructural library.
// file: TokenScanner.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// Parser of the <see cref="CharStream"/> to the list of <see cref="LexicalToken"/>s.
	/// </summary>
	public readonly ref struct TokenScanner
	{
		public delegate LexicalToken TokenParser(in TokenScanner scanner, ref CharStream stream);
		private const int HighAscii = 127;

		private readonly LexicalTokenRule?[] _asciiRules;
		private readonly LexicalTokenRule?[] _extraRules;
		private readonly ITokenParser _parser;

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
		public TokenScanner(ITokenFilter[]? filters, params LexicalTokenRule[] rules)
		{
			if (rules is null)
				throw new ArgumentNullException(nameof(rules));
			(_asciiRules, _extraRules) = ScanRules(rules);
			_parser = JoinFilters(filters);
		}

		private static ITokenParser JoinFilters(ITokenFilter[]? tokenFilters)
		{
			ITokenParser result = RootParser.Instance;
			if (tokenFilters is null)
				return result;
			foreach (var filter in tokenFilters)
			{
				if (filter == null)
					throw new ArgumentNullException(nameof(filter));
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

		/// <summary>
		/// Parses the next token.
		/// </summary>
		/// <returns>The parsed token value</returns>
		public readonly LexicalToken Next(ref CharStream stream)
		{
			LexicalToken token = _parser.GetNextToken(this, ref stream);
			return token.IsEmpty ? new LexicalToken(LexicalTokenType.EOF, stream.Position, 0) : token;
		}

		public void Reset() => _parser.Reset();

		//public void Reset()
		//{
		//	if (_filters == null) return;
		//	foreach (var filter in _filters)
		//	{
		//		filter.Reset();
		//	}
		//}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private readonly LexicalToken GetFirstByRule(ref CharStream stream)
		{
			if (stream.Eof)
				return LexicalToken.Empty;

			var c = stream[0];

			ReadOnlySpan<LexicalTokenRule?> rules = c <= HighAscii ? _asciiRules.AsSpan().Slice(c * _extraRules.Length, _extraRules.Length) : _extraRules.AsSpan();
			foreach (var rule in rules)
			{
				if (rule is null)
					continue;
				var token = rule.TryParse(ref stream);
				if (!token.IsEmpty)
					return token;
			}
			throw stream.SyntaxException($"Cannot find a rule for character ({(int)c})'{c}'");
		}

		private static (LexicalTokenRule?[] AsciiRules, LexicalTokenRule?[] ExtraRules) ScanRules(LexicalTokenRule[] tokenRules)
		{
			int n = tokenRules.Length;
			var asciiTable = new LexicalTokenRule?[(HighAscii + 1) * n];
			var extraTable = new LexicalTokenRule?[n];

			int j = 0;
			for (int i = 0; i < tokenRules.Length; ++i)
			{
				var rule = tokenRules[i];
				bool extra = rule.HasExtraBeginning;

				if (rule.BeginningChars != null)
				{
					foreach (char ch in rule.BeginningChars.AsSpan())
					{
						if (ch > HighAscii)
							extra = true;
						else
							asciiTable[ch * n + i] = rule;
					}
				}
				else
				{
					extra = true;
					for (int ch = 0; ch <= HighAscii; ++ch)
					{
						if (rule.TestBeginning((char)ch))
							asciiTable[ch * n + i] = rule;
					}
				}
				if (extra)
					extraTable[j++] = rule;
			}
			return (asciiTable, extraTable);
		}
	}
}


