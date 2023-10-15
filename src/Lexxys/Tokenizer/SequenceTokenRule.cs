// Lexxys Infrastructural library.
// file: SequenceTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Tokenizer;

[Serializable]
public class SequenceTokenRule: LexicalTokenRule
{
	private bool _isSorted;
	private string? _beginning;
	private int _length;
	private readonly List<Element> _sequence;
	private readonly bool _ignoreCase;

	public SequenceTokenRule(LexicalTokenType tokenType, bool ignoreCase, params string[] sequence)
	{
		if (sequence == null)
			throw new ArgumentNullException(nameof(sequence));

		TokenType = tokenType;
		_ignoreCase = ignoreCase;
		_sequence = new List<Element>(sequence.Length);
		for (int i = 0; i < sequence.Length; ++i)
		{
			if (sequence[i] == null || sequence[i].Length == 0)
				throw new ArgumentOutOfRangeException($"sequence[{i}]");
			_sequence.Add(new Element((short)(i + 1), ignoreCase ? sequence[i].ToUpperInvariant() : sequence[i]));
		}
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

	public override string? BeginningChars
	{
		get
		{
			Sort();
			return _beginning;
		}
	}

	public LexicalTokenType TokenType { get; }

	public override bool TestBeginning(char value) => BeginningChars?.Contains(value) ?? false;

	public override LexicalToken TryParse(ref CharStream stream)
	{
		var c = stream[0];
		var beginning = BeginningChars;
#if NET6_0_OR_GREATER
		int k = beginning == null ? -1: _ignoreCase ? beginning.IndexOf(c, StringComparison.OrdinalIgnoreCase): beginning.IndexOf(c);
#else
		int k = beginning == null ? -1: _ignoreCase ? beginning.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase): beginning.IndexOf(c);
#endif
		if (k < 0)
			return LexicalToken.Empty;

		var s = stream.Slice(0, _length).ToString();
		if (_ignoreCase)
		{
			for (int i = k % _sequence.Count; i < _sequence.Count; ++i)
			{
				if (s.StartsWith(_sequence[i].Text, StringComparison.OrdinalIgnoreCase))
					return stream.Token(LexicalTokenType.Create(TokenType.Group, _sequence[i].Id, TokenType.Name), _sequence[i].Text.Length);
				if (Char.ToUpperInvariant(s[0]) != Char.ToUpperInvariant(_sequence[i].Text[0]))
					return LexicalToken.Empty;
			}
		}
		else
		{
			for (int i = k; i < _sequence.Count; ++i)
			{
				if (s.StartsWith(_sequence[i].Text, StringComparison.Ordinal))
					return stream.Token(LexicalTokenType.Create(TokenType.Group, _sequence[i].Id, TokenType.Name), _sequence[i].Text.Length);
				if (s[0] != _sequence[i].Text[0])
					return LexicalToken.Empty;
			}
		}
		return LexicalToken.Empty;
	}

	public SequenceTokenRule Add(int id, string text)
	{
		if (text is not { Length: >0 })
			throw new ArgumentNullException(nameof(text));
		if (id is <=0 or >Int16.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(id), id, null);
		_sequence.Add(new Element((short)id, text));
		_isSorted = false;
		return this;
	}

	private void Sort()
	{
		if (_isSorted) return;
		_isSorted = true;
		_sequence.Sort((x, y) => String.CompareOrdinal(y.Text, x.Text));
		int n = _sequence.Count;
		char[] line = new char[n * 2];
		_length = 0;
		for (int i = 0; i < n; ++i)
		{
			string s = _sequence[i].Text;
			if (_ignoreCase)
			{
				line[i] = Char.ToUpperInvariant(s[0]);
				line[n + i] = Char.ToLowerInvariant(s[0]);
			}
			else
			{
				line[i] = s[0];
			}
			if (s.Length > _length)
				_length = s.Length;
		}
		_beginning = _ignoreCase ? new string(line): new string(line, 0, n);
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
