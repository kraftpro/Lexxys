// Lexxys Infrastructural library.
// file: ITokenFilter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Tokenizer;

/// <summary>
/// Defines a filter for processing tokens during lexical analysis.
/// </summary>
public interface ITokenFilter
{
	/// <summary>
	/// Gets the next lexical token from the character stream using the specified parser and scanner.
	/// </summary>
	/// <param name="parser">The token parser to use for parsing.</param>
	/// <param name="scanner">The token scanner to use for scanning.</param>
	/// <param name="stream">The character stream to read from. This parameter is passed by reference and may be modified.</param>
	/// <returns>The next lexical token parsed from the stream.</returns>
	LexicalToken GetNextToken(ITokenParser parser, in TokenScanner scanner, ref CharStream stream);

	/// <summary>
	/// Resets the instance to its initial state.
	/// </summary>
	void Reset();
}

/// <summary>
/// Defines a contract for parsing tokens from a character stream.
/// </summary>
public interface ITokenParser
{
	/// <summary>
	/// Reads the next token from the character stream.
	/// </summary>
	/// <param name="scanner">The scanner used to identify and parse tokens.</param>
	/// <param name="stream">The character stream to read from. Updated to reflect the current position after reading the token.</param>
	/// <returns>The next lexical token parsed from the stream.</returns>
	LexicalToken GetNextToken(in TokenScanner scanner, ref CharStream stream);

	/// <summary>
	/// Resets the object to its initial state.
	/// </summary>
	void Reset();
}


public static class TokenFilter
{
	public static ITokenFilter Create(Func<LexicalToken, bool> predicate) => new Filter(predicate);

	private class Filter(Func<LexicalToken, bool> predicate): ITokenFilter
	{
		private readonly Func<LexicalToken, bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

		public LexicalToken GetNextToken(ITokenParser parser, in TokenScanner scanner, ref CharStream stream)
		{
			LexicalToken token;
			do
			{
				token = parser.GetNextToken(in scanner, ref stream);
			} while (!token.IsEmpty && !_predicate(token));
			return token;
		}

		public void Reset()
		{
		}
	}
}

public class PushFilter: ITokenFilter
{
	private readonly Stack<LexicalToken> _stack = new Stack<LexicalToken>();

	public void Push(LexicalToken value) => _stack.Push(value);

	public LexicalToken GetNextToken(ITokenParser parser, in TokenScanner scanner, ref CharStream stream)
		=> _stack.Count > 0 ? _stack.Pop():
			parser is null ? throw new ArgumentNullException(nameof(parser)):
			parser.GetNextToken(in scanner, ref stream);

	public void Reset() => _stack.Clear();
}

public class OneBackFilter: ITokenFilter
{
	private bool _back;
	private LexicalToken _value;

	public LexicalToken Value => _value;

	public void Back()
	{
		if (_back)
			throw new InvalidOperationException();
		_back = true;
	}

	public void Back(LexicalToken value)
	{
		if (_back)
			throw new InvalidOperationException();
		_value = value;
		_back = true;
	}

	public LexicalToken GetNextToken(ITokenParser parser, in TokenScanner scanner, ref CharStream stream)
	{
		if (_back)
		{
			_back = false;
			return Value;
		}

		if (parser is null)
			throw new ArgumentNullException(nameof(parser));
		return _value = parser.GetNextToken(in scanner, ref stream);
	}

	public void Reset()
	{
		_value = default;	
		_back = false;
	}
}

public class IndentFilter: ITokenFilter
{
	private readonly Stack<int> _indent = new Stack<int>();
	private CharPosition _last;
	private LexicalToken _current;
	private int _currentColumn;

	public LexicalToken GetNextToken(ITokenParser parser, in TokenScanner scanner, ref CharStream stream)
	{
		if (!_current.IsEmpty)
		{
			if (_indent.Count > 0)
			{
				if (_currentColumn < _indent.Peek())
				{
					_indent.Pop();
					return new LexicalToken(LexicalTokenType.UNDENT, _current.Position, 0);
				}
				if (_currentColumn > _indent.Peek())
				{
					_indent.Push(_currentColumn);
					return new LexicalToken(LexicalTokenType.INDENT, _current.Position, 0);
				}
			}
			LexicalToken tmp = _current;
			_current = LexicalToken.Empty;
			return tmp;
		}

		if (parser is null) throw new ArgumentNullException(nameof(parser));

		int position = stream.Position;
		LexicalToken token = parser.GetNextToken(scanner, ref stream);
		if (token.IsEmpty)
		{
			if (_indent.Count == 0)
				return LexicalToken.Empty;
			_indent.Pop();
			return new LexicalToken(LexicalTokenType.UNDENT, _last.Position, 0);
		}
		CharPosition at = stream.GetCharPosition(position, _last);
		if (_indent.Count == 0)
			_indent.Push(at.Column);
		var line = _last.Line;
		_last = at;
		if (line >= at.Line || token.Is(LexicalTokenType.IGNORE) || token.Is(LexicalTokenType.COMMENT) || token.Is(LexicalTokenType.WHITESPACE) || token.Is(LexicalTokenType.NEWLINE))
			return token;

		if (at.Column == _indent.Peek())
			return token;

		_current = token;
		_currentColumn = at.Column;
		if (at.Column > _indent.Peek())
		{
			_indent.Push(at.Column);
			return new LexicalToken(LexicalTokenType.INDENT, token.Position, 0);
		}
		_indent.Pop();
		return new LexicalToken(LexicalTokenType.UNDENT, token.Position, 0);
	}

	public void Reset()
	{
		_indent.Clear();
		_last = default;
		_current = default;
		_currentColumn = default;
	}
}

public class NthBackFilter: ITokenFilter
{
	private readonly LexicalToken[] _trace;
	private int _top;
	private int _count;
	private int _back;

	public NthBackFilter(int count)
	{
		if (count <= 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, null);

		_trace = new LexicalToken[count];
	}

	public LexicalToken Value => Peek();

	public void Back()
	{
		if (_count <= _back)
			throw new InvalidOperationException();
		++_back;
	}

	public void Back(LexicalToken value)
	{
		if (_count <= _back)
			throw new InvalidOperationException("The back queue is full");
		++_back;
		Push(value);
	}

	public LexicalToken GetNextToken(ITokenParser parser, in TokenScanner scanner, ref CharStream stream)
	{
		if (_back != 0)
		{
			--_back;
			return Pop();
		}

		if (parser is null)
			throw new ArgumentNullException(nameof(parser));
		return Push(parser.GetNextToken(scanner, ref stream));
	}

	private LexicalToken Push(LexicalToken token)
	{
		if (_count < _trace.Length)
			++_count;
		_trace[_top] = token;
		if (++_top == _trace.Length)
			_top = 0;
		return token;
	}

	private LexicalToken Peek()
	{
		if (_count == 0)
			return LexicalToken.Empty;
		return _trace[_top == 0 ? _trace.Length - 1: _top];
	}

	private LexicalToken Pop()
	{
		if (--_count < 0)
			throw new InvalidOperationException("The back queue is empty");
		if (--_top < 0)
			_top = _trace.Length - 1;
		return _trace[_top];
	}

	public void Reset()
	{
		_top = 0;
		_count = 0;
		_back = 0;
	}
}
