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

using Lexxys;

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// Parser of the <see cref="CharStream"/> to the list of <see cref="LexicalToken"/>s.
	/// </summary>
	public class TokenScanner: IEnumerable<LexicalToken>
	{
		private const int LowAscii = 0;
		private const int HighAscii = 127;

		private readonly List<LexicalTokenRule>?[] _asciiRules;
		private readonly List<LexicalTokenRule> _extraRules;
		private Func<LexicalToken> _getter;

		/// <summary>
		/// Initializes a new <see cref="TokenScanner"/> with the specified <paramref name="stream"/> and the list of the <see cref="LexicalTokenRule"/>s.
		/// </summary>
		/// <param name="stream">The <see cref="CharStream"/> to parse</param>
		/// <param name="rules">Collection of the <see cref="LexicalTokenRule"/>s.</param>
		public TokenScanner(CharStream stream, params LexicalTokenRule[] rules)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			if (rules is not { Length: > 0 })
				throw new ArgumentNullException(nameof(rules));

			_extraRules = new List<LexicalTokenRule>(rules.Length);
			_asciiRules = new List<LexicalTokenRule>[HighAscii - LowAscii + 1];
			SetRules(rules);
			_getter = GetNextToken;
			Stream = stream;
			Current = LexicalToken.Empty;
		}

		/// <summary>
		/// Creats a copy of the <see cref="TokenScanner"/> with the specified <paramref name="stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="CharStream"/> to parse</param>
		/// <param name="scanner">The <see cref="TokenScanner"/> to copy parameters from</param>
		/// <param name="copyFilters">Indicates to copy existing filters from the original <paramref name="scanner"/>.</param>
		public TokenScanner(CharStream stream, TokenScanner scanner, bool copyFilters)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			if (scanner is null)
				throw new ArgumentNullException(nameof(scanner));

			Stream = stream;
			_asciiRules = scanner._asciiRules;
			_extraRules = scanner._extraRules;
			_getter = copyFilters ? scanner._getter: GetNextToken;
			Current = LexicalToken.Empty;
		}

		/// <summary>
		/// Current <see cref="CharStream"/>.
		/// </summary>
		public CharStream Stream { get; }

		/// <summary>
		/// The latest parsed <see cref="LexicalToken"/>.
		/// </summary>
		public LexicalToken Current { get; protected set; }

		/// <summary>
		/// The <see cref="CultureInfo"/> of the <see cref="Stream"/>
		/// </summary>
		public CultureInfo CultureInfo => Stream.Culture;

		/// <summary>
		/// It indicates that the end of the stream has been encountered.
		/// </summary>
		public bool EOF => Stream.Eof;

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
			Current = LexicalToken.Empty;
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
			var stream = Stream;
			_getter = () => filter.GetNextToken(stream, getter);
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
				} while (!token.IsEmpty && !predicate(token));
				return token;
			};
			return this;
		}

		/// <summary>
		/// Removes all filters from the filter chain.
		/// </summary>
		public void ResetFilter() => _getter = GetNextToken;

		/// <summary>
		/// Parses the next token and sets the value of <see cref="Current"/> to the parsed token.
		/// </summary>
		/// <returns>The parsed token value</returns>
		public LexicalToken Next() => Current = _getter();

		public virtual bool MoveNext() => Next().TokenType != LexicalTokenType.EOF;

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/>
		/// </summary>
		/// <returns></returns>
		public SyntaxException SyntaxException() => SyntaxException(null);

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> with the specified <paramref name="message"/> and the position.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="at"></param>
		/// <returns></returns>
		public SyntaxException SyntaxException(string? message, int at = default)
			=> Stream.SyntaxException(message, null, at == default ? Current.Position: at);

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> with the specified <paramref name="message"/>, position and <paramref name="fileName"/>.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="fileName"></param>
		/// <param name="at"></param>
		/// <returns></returns>
		public SyntaxException SyntaxException(string? message, string? fileName, int at = default)
			=> Stream.SyntaxException(message, fileName, at == default ? Current.Position: at);

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
			} while (!token.IsEmpty && token.Is(LexicalTokenType.IGNORE));
			return token;
		}

		private LexicalToken GetFirstByRule()
		{
			if (Stream.Eof)
				return LexicalToken.Eof(Stream.Position);
			foreach (var rule in GetRules(Stream[0]))
			{
				LexicalToken? token = rule.TryParse(Stream);
				if (token != null)
					return token;
			}
			return Stream.Token(LexicalTokenType.CHAR, 1);
		}

		private List<LexicalTokenRule> GetRules(char startingWith)
			=> IsAscii(startingWith) ? _asciiRules[startingWith] ?? EmptyList: _extraRules;
		private static readonly List<LexicalTokenRule> EmptyList = new List<LexicalTokenRule>();

		private void SetRules(LexicalTokenRule[] rules)
		{
			foreach (var rule in rules)
			{
				bool extra = rule.HasExtraBeginning;

				if (rule.BeginningChars != null)
				{
					foreach (char c in rule.BeginningChars)
					{
						if (!IsAscii(c))
							extra = true;
						else
							(_asciiRules[c] ??= new List<LexicalTokenRule>(4)).Add(rule);
					}
				}
				else
				{
					extra = true;
					for (char c = (char)LowAscii; c <= (char)HighAscii; ++c)
					{
						if (rule.TestBeginning(c))
							(_asciiRules[c] ??= new List<LexicalTokenRule>(4)).Add(rule);
					}
				}
				if (extra)
					_extraRules.Add(rule);
			}
		}

		private static bool IsAscii(char ch) => ch <= HighAscii;

		public override string ToString()
			=> String.Format(CultureInfo.InvariantCulture, "{0}; current: {1}", Stream.Position, Current);

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


