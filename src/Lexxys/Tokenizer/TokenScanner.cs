// Lexxys Infrastructural library.
// file: TokenScanner.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// Parser of the <see cref="CharStream"/> to the list of <see cref="LexicalToken"/>s.
	/// </summary>
	public class TokenScanner: IEnumerable<LexicalToken>
	{
		private const int LowAscii = 0;
		private const int HighAscii = 127;

		private readonly List<LexicalTokenRule> _rules;
		private readonly List<LexicalTokenRule>[] _asciiRules;
		private readonly List<LexicalTokenRule> _extraRules;
		private Func<LexicalToken> _getter;

		private TokenScanner()
		{
			_rules = new List<LexicalTokenRule>(32);
			_extraRules = new List<LexicalTokenRule>(32);
			_asciiRules = new List<LexicalTokenRule>[HighAscii - LowAscii + 1];
			_getter = GetNextToken;
		}

		/// <summary>
		/// Initializes a new <see cref="TokenScanner"/> with the specified <paramref name="stream"/> and the list of the <see cref="LexicalTokenRule"/>s.
		/// </summary>
		/// <param name="stream">The <see cref="CharStream"/> to parse</param>
		/// <param name="rules">Collection of the <see cref="LexicalTokenRule"/>s.</param>
		public TokenScanner(CharStream stream, params LexicalTokenRule[] rules)
			: this()
		{
			Stream = stream ?? throw new ArgumentNullException(nameof(stream));
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
		public TokenScanner(CharStream stream, TokenScanner scanner, bool copyFilters)
		{
			Stream = stream ?? throw new ArgumentNullException(nameof(stream));
			_rules = scanner._rules;
			_asciiRules = scanner._asciiRules;
			_extraRules = scanner._extraRules;
			_getter = copyFilters ? scanner._getter: GetNextToken;
		}

		/// <summary>
		/// Current <see cref="CharStream"/>.
		/// </summary>
		public CharStream Stream { get; }

		/// <summary>
		/// The latest parsed <see cref="LexicalToken"/>.
		/// </summary>
		public virtual LexicalToken Current { get; protected set; }

		/// <summary>
		/// The <see cref="CultureInfo"/> of the <see cref="Stream"/>
		/// </summary>
		public CultureInfo CultureInfo => Stream.CultureInfo;

		/// <summary>
		/// It indicates that the end of the stream has been encountered.
		/// </summary>
		public bool EOF => Stream.Eof;

		/// <summary>
		/// Gets the <see cref="CharPosition"/> of the current token.
		/// </summary>
		public int At => Current?.Position ?? default;

		/// <summary>
		/// Rewinds the <see cref="Stream"/> and reset current token.
		/// </summary>
		public void Reset()
		{
			Stream.Rewind();
			Current = null;
		}

		/// <summary>
		/// Appends the token filter to the filters chain.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		public TokenScanner SetFilter(ITokenFilter filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));
			var getter = _getter;
			_getter = () => filter.GetNextToken(Stream, getter);
			return this;
		}

		/// <summary>
		/// Appends the token filter based on the specified <paramref name="predicate"/> to the filter chain.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TokenScanner SetFilter(Func<LexicalToken, bool> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			var getter = _getter;
			_getter = () =>
			{
				LexicalToken token;
				do
				{
					token = getter();
				} while (token != null && !predicate(token));
				return token;
			};
			return this;
		}

		/// <summary>
		/// Removes all filters from the filter chain.
		/// </summary>
		public void ResetFilter()
		{
			_getter = GetNextToken;
		}

		/// <summary>
		/// Parses the next token and sets the value of <see cref="Current"/> to the parsed token.
		/// </summary>
		/// <returns>The parsed token value</returns>
		public LexicalToken Next()
		{
			LexicalToken token = _getter();
			if (token != null)
			{
				Current = token;
			}
			else if (Current == null || !Current.Is(LexicalTokenType.EOF))
			{
				int at = Stream.Position;
				Current = new LexicalToken(LexicalTokenType.EOF, "", at, Stream.CultureInfo);
			}
			return Current;
		}

		/// <summary>
		/// Parses the next token and sets the value of <see cref="Current"/> to the parsed token.
		/// </summary>
		/// <returns>Returns false if end of the <see cref="Stream"/> found.</returns>
		public virtual bool MoveNext()
		{
			return !Next().Is(LexicalTokenType.EOF);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/>
		/// </summary>
		/// <returns></returns>
		public SyntaxException SyntaxException()
		{
			return SyntaxException(null);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> with the specified <paramref name="message"/> and the position.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="at"></param>
		/// <returns></returns>
		public SyntaxException SyntaxException(string message, int at = default)
		{
			if (at == default && Current != null)
				at = Current.Position;
			return Stream.SyntaxException(message, null, at);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> with the specified <paramref name="message"/>, position and <paramref name="fileName"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="fileName"></param>
		/// <param name="at"></param>
		/// <returns></returns>
		public SyntaxException SyntaxException(string message, string fileName, int at = default)
		{
			if (at == default && Current != null)
				at = Current.Position;
			return Stream.SyntaxException(message, fileName, at);
		}

		/// <summary>
		/// Parses the next token without affecting <see cref="Current"/> property.
		/// </summary>
		/// <returns></returns>
		private LexicalToken GetNextToken()
		{
			LexicalToken token;
			do
			{
				token = GetFirstByRule();
			} while (token != null && token.Is(LexicalTokenType.IGNORE));
			return token;
		}

		private LexicalToken GetFirstByRule()
		{
			if (Stream.Eof)
				return null;
			foreach (var rule in GetRules(Stream[0]))
			{
				LexicalToken token = rule.TryParse(Stream);
				if (token != null)
					return token;
			}
			return Stream.Token(LexicalTokenType.CHAR, 1);
		}

		private List<LexicalTokenRule> GetRules(char startingWith)
		{
			if (!IsAscii(startingWith))
				return _extraRules;

			List<LexicalTokenRule> rr = _asciiRules[startingWith];
			return rr ?? EmptyList;
		}
		private static readonly List<LexicalTokenRule> EmptyList = new List<LexicalTokenRule>();

		private void AddRule(LexicalTokenRule rule)
		{
			if (_rules.Contains(rule))
				return;

			_rules.Add(rule);
			bool extra = rule.HasExtraBeginning;

			if (rule.BeginningChars != null)
			{
				foreach (char ch in rule.BeginningChars)
				{
					if (!IsAscii(ch))
					{
						extra = true;
					}
					else
					{
						List<LexicalTokenRule> r = _asciiRules[ch];
						if (r == null)
						{
							r = new List<LexicalTokenRule>();
							_asciiRules[ch] = r;
						}
						if (!r.Contains(rule))
							r.Add(rule);
					}
				}
			}
			else
			{
				extra = true;
				for (int i = LowAscii; i <= HighAscii; ++i)
				{
					if (rule.TestBeginning((char)i))
					{
						List<LexicalTokenRule> r = _asciiRules[i];
						if (r == null)
						{
							r = new List<LexicalTokenRule>();
							_asciiRules[i] = r;
						}
						if (!r.Contains(rule))
							r.Add(rule);
					}
				}
			}
			if (extra)
				_extraRules.Add(rule);
		}

		private static bool IsAscii(char ch)
		{
			return ch <= '\x007F';
		}

		public override string ToString()
		{
			return String.Format(CultureInfo.InvariantCulture, "{0}; current: {1}", Stream.Position, Current);
		}

		#region IEnumerable Members

		public IEnumerator<LexicalToken> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}

		private class Enumerator: IEnumerator<LexicalToken>
		{
			readonly TokenScanner _scanner;

			public Enumerator(TokenScanner scanner)
			{
				_scanner = scanner;
			}


			#region IEnumerator<LexicalToken> Members

			public LexicalToken Current => _scanner.Current;

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
			}

			#endregion

			#region IEnumerator Members

			object IEnumerator.Current => _scanner.Current;

			public bool MoveNext()
			{
				return _scanner.MoveNext();
			}

			public void Reset()
			{
				_scanner.Reset();
			}

			#endregion
		}
		#endregion
	}
}


