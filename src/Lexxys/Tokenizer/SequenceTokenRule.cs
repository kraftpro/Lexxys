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

#nullable enable

namespace Lexxys.Tokenizer
{
	public class SequenceTokenRule: LexicalTokenRule
	{
		private string? _beginning;
		private readonly bool _ignoreCase;
		private readonly List<Element> _sequence;
		private readonly List<Element> _sequence0;

		public SequenceTokenRule(LexicalTokenType tokenType, bool ignoreCase, params string[] sequence)
		{
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			TokenType = tokenType;
			_ignoreCase = ignoreCase;
			_sequence = new List<Element>(Math.Max(sequence.Length + 2, 8));
			for (int i = 0; i < sequence.Length; ++i)
			{
				if (sequence[i] == null || sequence[i].Length == 0)
					throw new ArgumentOutOfRangeException($"sequence[{i}]");
				_sequence.Add(new Element((short)(i + 1), ignoreCase ? sequence[i].ToUpperInvariant() : sequence[i]));
			}
			_sequence0 = new List<Element>();
		}

		public SequenceTokenRule(LexicalTokenType tokenType, params string[] sequence)
			: this(tokenType, false, sequence)
		{
		}

		public SequenceTokenRule(bool ignoreCase, params string[] sequence)
			: this(LexicalTokenType.SEQUENCE, ignoreCase, sequence)
		{
		}

		public SequenceTokenRule(params string[] sequence)
			: this(LexicalTokenType.SEQUENCE, false, sequence)
		{
		}

		public override string? BeginningChars => _beginning ??= GetBeginning(_sequence, _ignoreCase);

		public override bool HasExtraBeginning => _sequence0.Count > 0;

		private static string GetBeginning(List<Element> sequence, bool ignoreCase)
		{
			//Comparison<Element> startComparison = ignoreCase ?
			//	(x, y) => Char.ToUpperInvariant(x.Start).CompareTo(Char.ToUpperInvariant(y.Start)):
			//	(x, y) => x.Start.CompareTo(y.Start);
			//sequence.Sort(startComparison);

			Comparison<Element> lengthComparison = (x, y) => String.CompareOrdinal(y.Text, x.Text);
			sequence.Sort(lengthComparison);

			char[] chars = new char[sequence.Count];
			for (int i = 0; i < sequence.Count; ++i)
			{
				chars[i] = ignoreCase ? Char.ToUpperInvariant(sequence[i].Start): sequence[i].Start;
			}

			return new string(chars);
		}

		public LexicalTokenType TokenType { get; }

		public override bool TestBeginning(char value) => BeginningChars?.Contains(value) ?? false;

		public override LexicalToken? TryParse(CharStream stream)
		{
			var c = stream[0];
			var beginning = BeginningChars;
#if NET6_0_OR_GREATER
			int k = beginning == null ? -1: _ignoreCase ? beginning.IndexOf(c, StringComparison.OrdinalIgnoreCase): beginning.IndexOf(c);
#else
			int k = beginning == null ? -1: _ignoreCase ? beginning.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase): beginning.IndexOf(c);
#endif
			if (k >= 0)
			{
				for (int i = k; i < _sequence.Count; ++i)
				{
					int n = _sequence[i].Match(stream, _ignoreCase);
					if (n > 0)
						return stream.Token(new LexicalTokenType(TokenType.Group, _sequence[i].Id, TokenType.Name), n, stream.Substring(0, n));
					if (c != _sequence[i].Start)
						break;
				}
			}
			for (int i = 0; i < _sequence0.Count; ++i)
			{
				int n = _sequence[i].Match(stream, _ignoreCase);
				if (n > 0)
					return stream.Token(new LexicalTokenType(TokenType.Group, _sequence[i].Id, TokenType.Name), n, stream.Substring(0, n));
			}
			return null;
		}

		public SequenceTokenRule Add(int id, string text)
		{
			if (id <= 0 || id > Int16.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(id), id, null);
			if (text == null || text.Length == 0)
				throw new ArgumentNullException(nameof(text));

			_sequence.Add(new Element((short)id, text));
			_beginning = null;
			return this;
		}

		public SequenceTokenRule Add(short id, string text, char start, Func<CharStream, bool, int> match)
		{
			if (id <= 0 || id > Int16.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(id), id, null);
			if (text == null || text.Length == 0)
				throw new ArgumentNullException(nameof(text));
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (start == '\0')
				_sequence0.Add(new Element(id, text, start, match));
			else
				_sequence.Add(new Element(id, text, start, match));
			_beginning = null;
			return this;
		}

		private readonly struct Element
		{
			public short Id { get; }
			public string Text { get; }
			public char Start { get; }
			//public int Length { get; }
			public Func<CharStream, bool, int> Match { get; }

			public Element(short id, string text)
			{
				if (id <= 0 || id > Int16.MaxValue)
					throw new ArgumentOutOfRangeException(nameof(id), id, null);
				if (text == null || text.Length <= 0)
					throw new ArgumentNullException(nameof(text));

				Id = id;
				Text = text;
				Start = text[0];
				Match = (o, b) => o.StartsWith(text, 0, b ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal) ? text.Length: 0;
			}

			public Element(short id, string text, char start, Func<CharStream, bool, int> match)
			{
				if (id <= 0 || id > Int16.MaxValue)
					throw new ArgumentOutOfRangeException(nameof(id), id, null);
				if (text == null || text.Length <= 0)
					throw new ArgumentNullException(nameof(text));
				if (match == null)
					throw new ArgumentNullException(nameof(match));

				Id = id;
				Text = text;
				Start = start;
				Match = match;
			}
		}
	}
}
