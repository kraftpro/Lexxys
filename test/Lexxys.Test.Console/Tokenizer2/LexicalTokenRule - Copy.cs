// Lexxys Infrastructural library.
// file: LexicalTokenRule.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Lexxys;
using System.Linq;

namespace Lexxys.Tokenizer2
{
	/// <summary>
	/// The rule used by <see cref="TokenScanner"/> to extract tokens.
	/// </summary>
	public abstract class LexicalTokenRule<T> where T: IEquatable<T>
	{
		/// <summary>
		/// List of the possible starting characters for the parsing token.
		/// </summary>
		public virtual IReadOnlyCollection<T> BeginningChars => default;

		/// <summary>
		/// Specifies that the parsing token could contains extra characters not included in the <see cref="BeginningChars"/>.
		/// </summary>
		public virtual bool HasExtraBeginning => false;

		/// <summary>
		/// Tests that the specified <paramref name="value"/> could be start of a new token.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool TestBeginning(T value)
			=> BeginningChars?.Contains(value) ?? false;

		/// <summary>
		/// Tryes to extract a token from the <paramref name="stream"/>.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns>Extracted token or null.</returns>
		public abstract bool TryParse(in CharStream<T> stream, out LexicalTokenT token);
	}

	/// <summary>
	/// Generic token rule
	/// </summary>
	public class TokenRule<T>: LexicalTokenRule<T> where T: IEquatable<T>
	{
		public delegate bool Parser(in CharStream<T> stream, out LexicalTokenT token);
		private readonly IReadOnlyCollection<T> _start;
		private readonly Parser _parser;
		private readonly Func<T, bool> _test;

		/// <summary>
		/// Creates a new <see cref="LexicalTokenRule"/> with the specified fuctions for testing and parsing.
		/// </summary>
		/// <param name="parser">Function that will be used to extract a token from a stream.</param>
		/// <param name="start">List of the possible starting characters for the parsing token.</param>
		/// <param name="test">Function that will be used to test that a character could be start of a new token.</param>
		public TokenRule(Parser parser)
		{
			_parser = parser ?? throw new ArgumentNullException(nameof(_parser));
			_test = o => true;
		}

		/// <summary>
		/// Creates a new <see cref="LexicalTokenRule"/> with the specified fuctions for testing and parsing.
		/// </summary>
		/// <param name="parser">Function that will be used to extract a token from a stream.</param>
		/// <param name="start">List of the possible starting characters for the parsing token.</param>
		/// <param name="test">Function that will be used to test that a character could be start of a new token.</param>
		public TokenRule(Parser parser, Func<T, bool> test)
		{
			_parser = parser ?? throw new ArgumentNullException(nameof(_parser));
			_test = test ?? (o => true);
		}

		/// <summary>
		/// Creates a new <see cref="LexicalTokenRule"/> with the specified fuctions for testing and parsing.
		/// </summary>
		/// <param name="parser">Function that will be used to extract a token from a stream.</param>
		/// <param name="start">List of the possible starting characters for the parsing token.</param>
		/// <param name="test">Function that will be used to test that a character could be start of a new token.</param>
		public TokenRule(Parser parser, IReadOnlyCollection<T> start)
		{
			_parser = parser ?? throw new ArgumentNullException(nameof(_parser));
			_start = start;
			_test = start == null ? (Func<T, bool>)(o => true): (o => false);
		}

		/// <summary>
		/// Creates a new <see cref="LexicalTokenRule"/> with the specified fuctions for testing and parsing.
		/// </summary>
		/// <param name="parser">Function that will be used to extract a token from a stream.</param>
		/// <param name="start">List of the possible starting characters for the parsing token.</param>
		/// <param name="test">Function that will be used to test that a character could be start of a new token.</param>
		public TokenRule(Parser parser, IReadOnlyCollection<T> start, Func<T, bool> test)
		{
			_parser = parser ?? throw new ArgumentNullException(nameof(_parser));
			_start = start;
			_test = test ?? (start == null ? (Func<T, bool>)(o => true) : (o => false));
		}

		/// <summary>
		/// Creates a new <see cref="LexicalTokenRule"/> with the specified fuctions for testing and parsing.
		/// </summary>
		/// <param name="parser">Function that will be used to extract a token from a stream.</param>
		/// <param name="start">List of the possible starting characters for the parsing token.</param>
		/// <param name="test">Function that will be used to test that a character could be start of a new token.</param>
		public TokenRule(Func<T, bool> predicate, LexicalTokenType tokenType, IReadOnlyCollection<T> start = null, Func<T, bool> test = null)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			_parser = Parse;
			_start = start ?? Array.Empty<T>();
			_test = test ?? (o => false);

			bool Parse(in CharStream<T> stream, out LexicalTokenT token)
			{
				int n = stream.Length;
				for (int i = 0; i < n; i++)
				{
					if (!predicate(stream[i]))
					{
						token = new LexicalTokenT(tokenType, stream.Position, i, stream.Culture);
						return true;
					}
				}
				token = default;
				return false;
			}
		}

		/// <ingeritdoc />
		public override IReadOnlyCollection<T> BeginningChars => _start;

		/// <ingeritdoc />
		public override bool TestBeginning(T value)
			=> _start.Contains(value) || _test.Invoke(value);

		public override bool TryParse(in CharStream<T> stream, out LexicalTokenT token)
			=> _parser(in stream, out token);
	}
}
