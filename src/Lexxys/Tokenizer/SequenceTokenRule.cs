// Lexxys Infrastructural library.
// file: SequenceTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Lexxys.Tokenizer
{
	public class SequenceTokenRule: LexicalTokenRule
	{
		private bool _isSorted;
		private string _beginning;
		private int _length;
		private readonly List<Element> _sequence;
		private readonly bool _ignoreCase;
		private readonly Func<char, bool> _testEndChar;

		public SequenceTokenRule(LexicalTokenType tokenType, bool ignoreCase, Func<char, bool> testEndChar, params string[] sequence)
		{
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			TokenType = tokenType;
			_ignoreCase = ignoreCase;
			_testEndChar = testEndChar;
			_sequence = new List<Element>(sequence.Length);
			for (int i = 0; i < sequence.Length; ++i)
			{
				if (sequence[i] == null || sequence[i].Length == 0)
					throw new ArgumentOutOfRangeException($"sequence[{i}]");
				_sequence.Add(new Element((short)(i + 1), ignoreCase ? sequence[i].ToUpperInvariant() : sequence[i]));
			}
		}

		public SequenceTokenRule(LexicalTokenType tokenType, bool ignoreCase, params string[] sequence)
			: this(tokenType, ignoreCase, null, sequence)
		{
		}

		public SequenceTokenRule(LexicalTokenType tokenType, params string[] sequence)
			: this(tokenType, false, null, sequence)
		{
		}

		public SequenceTokenRule(bool ignoreCase, params string[] sequence)
			: this(LexicalTokenType.SEQUENCE, ignoreCase, null, sequence)
		{
		}

		public SequenceTokenRule(params string[] sequence)
			: this(LexicalTokenType.SEQUENCE, false, null, sequence)
		{
		}

		public override string BeginningChars
		{
			get
			{
				Sort();
				return _beginning;
			}
		}

		public LexicalTokenType TokenType { get; }

		public override bool TestBeginning(char value)
		{
			return BeginningChars.IndexOf(value) >= 0;
		}

		public override LexicalToken TryParse(CharStream stream)
		{
			int k = BeginningChars.IndexOf(stream[0]);
			if (k < 0)
				return null;
			string s = stream.Substring(0, _length + 1);
			if (_ignoreCase)
			{
				s = s.ToUpperInvariant();
				if (k >= _sequence.Count)
					k -= _sequence.Count;
			}
			for (int i = k; i < _sequence.Count; ++i)
			{
				if (s.StartsWith(_sequence[i].Text, StringComparison.Ordinal) &&
					(s.Length == _length || _testEndChar == null || _testEndChar(s[s.Length - 1])))
					return stream.Token(new LexicalTokenType(TokenType.Group, _sequence[i].Id, TokenType.Name), _sequence[i].Text.Length, _sequence[i].Text);
				if (s[0] != _sequence[i].Text[0])
					return null;
			}
			return null;
		}

		public SequenceTokenRule Add(int id, string text)
		{
			if (text == null || text.Length == 0)
				throw new ArgumentNullException(nameof(text));
			if (id <= 0 || id > Int16.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(id), id, null);
			_sequence.Add(new Element((short)id, text));
			_isSorted = false;
			return this;
		}

		private void Sort()
		{
			if (!_isSorted)
			{
				_isSorted = true;
				_sequence.Sort((x, y) => String.CompareOrdinal(y.Text, x.Text));
				StringBuilder sb = new StringBuilder(_sequence.Count);
				_length = 0;
				for (int i = 0; i < _sequence.Count; ++i)
				{
					string s = _sequence[i].Text;
					sb.Append(s[0]);
					if (s.Length > _length)
						_length = s.Length;
				}
				if (_ignoreCase)
					sb.Append(sb.ToString().ToUpperInvariant());
				_beginning = sb.ToString();
			}
		}

		private struct Element
		{
			public readonly short Id;
			public readonly string Text;

			public Element(short id, string text)
			{
				Id = id;
				Text = text;
			}
		}
	}
}


