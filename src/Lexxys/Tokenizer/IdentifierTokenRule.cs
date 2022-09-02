// Lexxys Infrastructural library.
// file: IdentifierTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.Tokenizer
{
	public class IdentifierTokenRule: LexicalTokenRule
	{
		private readonly Dictionary<string, LexicalTokenType> _keywords;
		private readonly bool _ignoreCase;

		public IdentifierTokenRule(LexicalTokenType identifierType, LexicalTokenType keywordType, bool ignoreCase, params string[] keywords)
		{
			if (keywords is null)
				throw new ArgumentNullException(nameof(keywords));

			KeywordTokenType = keywordType;
			_keywords = new Dictionary<string, LexicalTokenType>();
			_ignoreCase = ignoreCase;
			IdentifierTokenType = identifierType;

			for (int i = 0; i < keywords.Length; ++i)
			{
				Add(i + 1, keywords[i]);
			}
		}

		public IdentifierTokenRule(bool ignoreCase, params string[] keywords)
			: this(LexicalTokenType.IDENTIFIER, LexicalTokenType.KEYWORD, ignoreCase, keywords)
		{
		}

		public IdentifierTokenRule(params string[] keywords)
			: this(LexicalTokenType.IDENTIFIER, LexicalTokenType.KEYWORD, false, keywords)
		{
		}

		public IdentifierTokenRule Add(int id, string keyword)
		{
			return Add(KeywordTokenType.WithItem(id), keyword);
		}

		public IdentifierTokenRule Add(LexicalTokenType type, string keyword)
		{
			if (keyword is null)
				throw new ArgumentNullException(nameof(keyword));
			if (_ignoreCase)
				keyword = keyword.ToUpperInvariant();
			if (!_keywords.TryGetValue(keyword, out LexicalTokenType tp))
				_keywords.Add(keyword, type);
			else if (tp != type)
				throw new ArgumentOutOfRangeException(nameof(keyword), keyword, null);
			return this;
		}

		public override string? BeginningChars => "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";

		public override bool HasExtraBeginning => true;

		public LexicalTokenType IdentifierTokenType { get; }
		public LexicalTokenType KeywordTokenType { get; }

		public override bool TestBeginning(char value)
		{
			return (Char.IsLetter(value) || value == '_');
		}

		public override LexicalToken? TryParse(CharStream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			char ch = stream[0];
			if (!(Char.IsLetter(ch) || ch == '_'))
				return null;

			int i = 0;
			do
			{
				ch = stream[++i];
			} while (Char.IsDigit(ch) || Char.IsLetter(ch) || ch == '_');
			string text = stream.Substring(0, i);
			if (_ignoreCase)
				text = text.ToUpperInvariant();
			if (!_keywords.TryGetValue(text, out LexicalTokenType tp))
				tp = IdentifierTokenType;
			return stream.Token(tp, i, text);
		}
	}
}


