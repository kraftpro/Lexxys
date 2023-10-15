// Lexxys Infrastructural library.
// file: CommentsTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Tokenizer;

[Serializable]
public class CommentsTokenRule: LexicalTokenRule
{
	private readonly string _beginning;
	private readonly (string Start, string End)[] _startEnd;
	private readonly int _startLength;
	private static readonly (string, string)[] _cppComments = [("//", "\n")];

	public CommentsTokenRule(params (string, string)[]? startEnd)
		: this(LexicalTokenType.COMMENT, startEnd)
	{
	}

	public CommentsTokenRule(LexicalTokenType comment, params (string Start, string End)[]? startEnd)
	{
		TokenType = comment;
		if (startEnd == null || startEnd.Length == 0)
		{
			_startEnd = _cppComments;
		}
		else
		{
			for (int i = 0; i < startEnd.Length; ++i)
			{
				if (String.IsNullOrEmpty(startEnd[i].Start))
					throw new ArgumentOutOfRangeException($"startEnd[{i}].Start", startEnd[i].Start, null);
				if (String.IsNullOrEmpty(startEnd[i].End))
					throw new ArgumentOutOfRangeException($"startEnd[{i}].End", startEnd[i].End, null);
			}
			_startEnd = new (string, string)[startEnd.Length];
			startEnd.CopyTo(_startEnd, 0);
		}
		_startLength = 0;
		char[] beginning = new char[_startEnd.Length];
		int j = 0;
		for (int i = 0; i < _startEnd.Length; ++i)
		{
			if (_startEnd[i].Start.Length > _startLength)
				_startLength = _startEnd[i].Start.Length;
			if (!beginning.Contains(_startEnd[i].Start[0]))
				beginning[j++] = _startEnd[i].Start[0];
		}
		_beginning = new string(beginning, 0, j);
	}

	public override string BeginningChars => _beginning;

	public LexicalTokenType TokenType { get; }

	public override bool TestBeginning(char value) => _beginning.IndexOf(value) >= 0;

	public override LexicalToken TryParse(ref CharStream stream)
	{
		var s = stream.Slice(0, _startLength);
		bool found = false;
		foreach ((string Start, string End) item in _startEnd)
		{
			if (s.StartsWith(item.Start.AsSpan()))
			{
				found = true;
				int position = stream.IndexOf(item.End, item.Start.Length);
				if (position < 0)
				{
					if (item.End != "\n")
						continue;
					position = stream.Length;
				}
				return stream.Token(TokenType, position + (item.End == "\n" ? 0 : item.End.Length));
			}
		}
		if (found)
			throw stream.SyntaxException(SR.EofInComments());
		return LexicalToken.Empty;
	}
}


public class CppCommentsTokenRule: LexicalTokenRule
{
	public CppCommentsTokenRule()
	{
		TokenType = LexicalTokenType.COMMENT;
	}

	public CppCommentsTokenRule(LexicalTokenType comment)
	{
		TokenType = comment;
	}

	public override string BeginningChars => "/";

	public LexicalTokenType TokenType { get; }

	public override bool TestBeginning(char value) => value == '/';

	public override LexicalToken TryParse(ref CharStream stream)
	{
		if (stream[0] == '/')
		{
			if (stream[1] == '/')
			{
				int i = stream.IndexOf('\n', 2);
				if (i < 0)
					i = stream.Length;
				return stream.Token(TokenType, i);
			}
			if (stream[1] == '*')
			{
				int i = stream.IndexOf("*/", 2);
				if (i < 0)
					throw stream.SyntaxException(SR.EofInComments());
				return stream.Token(TokenType, i + 2);
			}
		}
		return LexicalToken.Empty;
	}
}

public class PythonCommentsTokenRule: LexicalTokenRule
{
	public PythonCommentsTokenRule()
	{
		TokenType = LexicalTokenType.COMMENT;
	}

	public PythonCommentsTokenRule(LexicalTokenType comment)
	{
		TokenType = comment;
	}

	public override string BeginningChars => "#";

	public LexicalTokenType TokenType { get; }

	public override bool TestBeginning(char value) => value == '#';

	public override LexicalToken TryParse(ref CharStream stream)
	{
		if (stream[0] != '#')
			return LexicalToken.Empty;
		int i = stream.IndexOf('\n', 1);
		if (i < 0)
			i = stream.Length;
		return stream.Token(TokenType, i);
	}
}
