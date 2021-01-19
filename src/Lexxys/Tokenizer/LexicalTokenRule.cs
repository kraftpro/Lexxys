// Lexxys Infrastructural library.
// file: LexicalTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Lexxys;

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// The rule used by <see cref="TokenScanner"/> to extract tokens.
	/// </summary>
	public abstract class LexicalTokenRule
	{
		/// <summary>
		/// List of the possible starting characters for the parsing token.
		/// </summary>
		public virtual string BeginningChars => null;

		/// <summary>
		/// Specifies that the parsing token could contains extra characters not included in the <see cref="BeginningChars"/>.
		/// </summary>
		public virtual bool HasExtraBeginning => false;

		/// <summary>
		/// Tests that the specified <paramref name="value"/> could be start of a new token.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool TestBeginning(char value)
		{
			string s = BeginningChars;
			return s == null || s.IndexOf(value) >= 0;
		}

		/// <summary>
		/// Tryes to extract a token from the <paramref name="stream"/>.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns>Extracted token or null.</returns>
		public abstract LexicalToken TryParse(CharStream stream);
	}

	/// <summary>
	/// Generic token rule
	/// </summary>
	public class TokenRule: LexicalTokenRule
	{
		public delegate LexicalToken Parser(CharStream stream);
		private readonly string _start;
		private readonly Parser _parser;
		private readonly Func<char, bool> _test;

		/// <summary>
		/// Creates a new <see cref="LexicalTokenRule"/> with the specified fuctions for testing and parsing.
		/// </summary>
		/// <param name="parser">Function that will be used to extract a token from a stream.</param>
		/// <param name="start">List of the possible starting characters for the parsing token.</param>
		/// <param name="test">Function that will be used to test that a character could be start of a new token.</param>
		public TokenRule(Parser parser, string start = null, Func<char, bool> test = null)
		{
			_parser = parser ?? throw new ArgumentNullException(nameof(parser));
			_start = start;
			_test = test;
		}

		/// <ingeritdoc />
		public override string BeginningChars => _start;

		/// <ingeritdoc />
		public override bool TestBeginning(char value)
		{
			return _test?.Invoke(value) ?? _start == null || _start.IndexOf(value) >= 0;
		}

		/// <ingeritdoc />
		public override LexicalToken TryParse(CharStream stream)
		{
			return _parser(stream);
		}
	}
}
