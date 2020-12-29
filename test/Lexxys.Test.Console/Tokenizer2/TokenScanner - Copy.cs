// Lexxys Infrastructural library.
// file: TokenScanner2.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Lexxys.Tokenizer2
{
	/// <summary>
	/// Parser of the <see cref="CharStream{T}"/> to the list of <see cref="LexicalToken"/>s.
	/// </summary>
	public ref struct TokenScanner<T> where T : IEquatable<T>
	{
		private const int LowAscii = 0;
		private const int HighAscii = 127;

		public delegate LexicalTokenT TokenParser(in TokenScanner<T> scanner);

		private readonly List<LexicalTokenRule<T>> _rules;
		private readonly Dictionary<T, List<LexicalTokenRule<T>>> _rulesTable;
		private readonly List<LexicalTokenRule<T>> _extraRules;
		private ITokenFilter<T> _getter;
		private LexicalTokenT _current;
		private LexicalTokenT _previous;
		private bool _back;

		/// <summary>
		/// Initializes a new <see cref="TokenScanner"/> with the specified <paramref name="stream"/> and the list of the <see cref="LexicalTokenRule"/>s.
		/// </summary>
		/// <param name="stream">The <see cref="CharStream"/> to parse</param>
		/// <param name="rules">Collection of the <see cref="LexicalTokenRule"/>s.</param>
		public TokenScanner(in CharStream<T> stream, params LexicalTokenRule<T>[] rules)
		{
			_rules = new List<LexicalTokenRule<T>>();
			_extraRules = new List<LexicalTokenRule<T>>();
			_rulesTable = new Dictionary<T, List<LexicalTokenRule<T>>>();
			_getter = Getter.Instance;
			Stream = stream;
			_current = default;
			_previous = default;
			_back = false;
			if (rules != null)
			{
				foreach (var rule in rules)
				{
					if (rule != null)
						AddRule(rule);
				}
			}
		}

		/// <summary>
		/// Creats a copy of the <see cref="TokenScanner"/> with the specified <paramref name="stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="CharStream"/> to parse</param>
		/// <param name="scanner">The <see cref="TokenScanner"/> to copy parameters from</param>
		/// <param name="copyFilters">Indicates to copy existing filters from the original <paramref name="scanner"/>.</param>
		public TokenScanner(in CharStream<T> stream, TokenScanner<T> scanner, bool copyFilters)
		{
			_rules = scanner._rules;
			_rulesTable = new Dictionary<T, List<LexicalTokenRule<T>>>();
			_extraRules = scanner._extraRules;
			_getter = copyFilters ? scanner._getter : Getter.Instance;
			Stream = stream;
			_current = default;
			_previous = default;
			_back = false;
		}

		/// <summary>
		/// Current <see cref="CharStream"/>.
		/// </summary>
		public CharStream<T> Stream { get; }

		/// <summary>
		/// The latest parsed <see cref="LexicalToken"/>.
		/// </summary>
		public LexicalTokenT Current => _current;

		/// <summary>
		/// It indicates that the end of the stream has been encountered.
		/// </summary>
		public bool Eof => Stream.Eof;

		/// <summary>
		/// Gets the <see cref="CharPosition"/> of the current token.
		/// </summary>
		public int At => Current.Position;

		/// <summary>
		/// Rewinds the <see cref="Stream"/> and reset current token.
		/// </summary>
		public void Reset()
		{
			Stream.Rewind();
			_current = default;
		}

		/// <summary>
		/// Appends the token filter to the filters chain.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		public TokenScanner<T> SetFilter(ITokenFilter<T> filter)
		{
			if (filter != null)
				_getter = new Join(filter, _getter);
			return this;
		}

		class Getter: ITokenFilter<T>
		{
			public static readonly Getter Instance = new Getter();

			public LexicalTokenT GetNextToken(TokenParser source, in TokenScanner<T> scanner)
			{
				throw new NotImplementedException();
			}

			public bool TryGetToken(in TokenScanner<T> scanner, out LexicalTokenT token)
			{
				if (!scanner.Eof)
				{
					foreach (var rule in scanner.GetRules(scanner.Stream[0]))
					{
						if (rule.TryParse(scanner.Stream, out token))
							return true;
					}
				}
				token = default;
				return false;
			}
		}

		class Join: ITokenFilter<T>
		{
			private readonly ITokenFilter<T> _first;
			private readonly ITokenFilter<T> _next;

			public Join(ITokenFilter<T> first, ITokenFilter<T> next)
			{
				_first = first;
				_next = next;
			}

			public LexicalTokenT GetNextToken(TokenParser source, in TokenScanner<T> scanner)
			{
				throw new NotImplementedException();
			}

			public bool TryGetToken(in TokenScanner<T> scanner, out LexicalTokenT token)
			{
				return _first.TryGetToken(in scanner, out token) || _next.TryGetToken(in scanner, out token);
			}
		}

		/// <summary>
		/// Removes all filters from the filter chain.
		/// </summary>
		public void ResetFilter()
		{
			_getter = Getter.Instance;
		}

		/// <summary>
		/// Parses the next token and sets the value of <see cref="Current"/> to the parsed token.
		/// </summary>
		/// <returns>The parsed token value</returns>
		public LexicalTokenT Next()
		{
			MoveNext();
			return _current;
		}

		/// <summary>
		/// Parses the next token and sets the value of <see cref="_current"/> to the parsed token.
		/// </summary>
		/// <returns>Returns false if end of the <see cref="Stream"/> found.</returns>
		public bool MoveNext()
		{
			if (_back)
			{
				_back = false;
				return true;
			}
			if (_getter.TryGetToken(in this, out var token))
			{
				_current = token;
				return true;
			}
			if (!_current.Is(LexicalTokenType.EOF))
				_current = new LexicalTokenT(LexicalTokenType.EOF, Stream.Position, 0, Stream.Culture);
			return false;
		}

		public void Back() => _back = true;

		private List<LexicalTokenRule<T>> GetRules(T startingWith)
		{
			return _rulesTable.TryGetValue(startingWith, out var rules) ? rules : _extraRules;
		}

		private void AddRule(LexicalTokenRule<T> rule)
		{
			if (_rules.Contains(rule))
				return;

			_rules.Add(rule);
			bool extra = rule.HasExtraBeginning;

			if (rule.BeginningChars != null)
			{
				foreach (var ch in rule.BeginningChars)
				{
					if (!_rulesTable.TryGetValue(ch, out var rules))
					{
						_rulesTable[ch] = rules = new List<LexicalTokenRule<T>>();
					}
					rules.Add(rule);
				}
			}
			if (rule.BeginningChars == null || rule.HasExtraBeginning)
				_extraRules.Add(rule);
		}

		public override string ToString()
		{
			return String.Format(CultureInfo.InvariantCulture, "{0}; current: {1}", Stream.Position, _current);
		}
	}
}
