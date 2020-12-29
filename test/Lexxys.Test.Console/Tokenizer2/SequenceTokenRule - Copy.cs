// Lexxys Infrastructural library.
// file: SequenceTokenRule.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Lexxys.Tokenizer2
{
	public class SequenceTokenRule<T>: LexicalTokenRule<T> where T: IEquatable<T>
	{
		private bool _isSorted;
		private IReadOnlyCollection<T> _beginning;
		private int _length;
		private readonly List<Element> _sequence;
		private readonly Func<T, bool> _testEndChar;

		public SequenceTokenRule(LexicalTokenType tokenType, Func<T, bool> testEndChar, params IReadOnlyList<T>[] sequence)
		{
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			TokenType = tokenType;
			_testEndChar = testEndChar;
			_sequence = new List<Element>(sequence.Length);
			for (int i = 0; i < sequence.Length; ++i)
			{
				if (sequence[i] == null || sequence[i].Count == 0)
					throw new ArgumentOutOfRangeException($"sequence[{i}]");
				_sequence.Add(new Element((short)(i + 1), sequence[i]));
			}
		}

		public SequenceTokenRule(LexicalTokenType tokenType, params IReadOnlyList<T>[] sequence)
			: this(tokenType, null, sequence)
		{
		}

		public SequenceTokenRule(params IReadOnlyList<T>[] sequence)
			: this(LexicalTokenType.SEQUENCE, null, sequence)
		{
		}

		public override IReadOnlyCollection<T> BeginningChars
		{
			get
			{
				Sort();
				return _beginning;
			}
		}

		public LexicalTokenType TokenType { get; }

		public override bool TryParse(in CharStream<T> stream, out LexicalTokenT token)
		{
			Sort();

			int k = BeginningChars.IndexOf(stream[0]);
			if (k < 0)
				return false;
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
			return default;
		}

		public SequenceTokenRule<T> Add(int id, string text)
		{
			if (text == null || text.Length == 0)
				throw new ArgumentNullException(nameof(text));
			if (id <= 0 || id > Int16.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(id), id, null);
			_sequence.Add(new Element((short)id, text));
			_isSorted = false;
			return default;
		}

		private void Sort()
		{
			if (!_isSorted)
			{
				_isSorted = true;
				_sequence.Sort((x, y) => String.CompareOrdinal(y.Text, x.Text));
				var items = new List<T>(_sequence.Count);
				_length = 0;
				for (int i = 0; i < _sequence.Count; ++i)
				{
					var item = _sequence[i].Text;
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
			public readonly IReadOnlyList<T> Text;

			public Element(short id, IReadOnlyList<T> text)
			{
				Id = id;
				Text = text;
			}
		}
	}
}
