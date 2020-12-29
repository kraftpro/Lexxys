// Lexxys Infrastructural library.
// file: ITokenFilter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Tokenizer
{
	public interface ITokenFilter
	{
		LexicalToken GetNextToken(CharStream stream, Func<LexicalToken> source);
	}

	public class PushFilter: ITokenFilter
	{
		private readonly Stack<LexicalToken> _stack = new Stack<LexicalToken>();

		public void Push(LexicalToken value)
		{
			_stack.Push(value);
		}

		public LexicalToken GetNextToken(CharStream stream, Func<LexicalToken> source)
		{
			return _stack.Count > 0 ? _stack.Pop() : source();
		}
	}

	public class OneBackFilter: ITokenFilter
	{
		private LexicalToken _last;
		private bool _back;

		public void Back()
		{
			if (_back)
				throw new InvalidOperationException();
			_back = true;
		}

		public LexicalToken GetNextToken(CharStream stream, Func<LexicalToken> source)
		{
			if (_back)
			{
				_back = false;
				return _last;
			}
			return _last = source();
		}
	}

	public class IndentFilter: ITokenFilter
	{
		private readonly Stack<int> _indent;
		private CharPosition _last;
		private LexicalToken _current;
		private int _currentColumn;

		public IndentFilter()
		{
			_indent = new Stack<int>();
		}

		public LexicalToken GetNextToken(CharStream stream, Func<LexicalToken> source)
		{
			if (_current != null)
			{
				if (_indent.Count > 0)
				{
					if (_currentColumn < _indent.Peek())
					{
						_indent.Pop();
						return new LexicalToken(LexicalTokenType.UNDENT, "", _current.Position);
					}
					if (_currentColumn > _indent.Peek())
					{
						_indent.Push(_currentColumn);
						return new LexicalToken(LexicalTokenType.INDENT, "", _current.Position);
					}
				}
				LexicalToken tmp = _current;
				_current = null;
				return tmp;
			}
			LexicalToken token = source();
			if (token == null)
			{
				if (_indent.Count == 0)
					return null;
				_indent.Pop();
				return new LexicalToken(LexicalTokenType.UNDENT, "", _last.Position);
			}
			CharPosition at = stream.GetPosition(token.Position, _last);
			if (_indent.Count == 0)
				_indent.Push(at.Position);
			var line = _last.Line;
			_last = at;
			if (line >= at.Line || token.Is(LexicalTokenType.COMMENT) || token.Is(LexicalTokenType.WHITESPACE) || token.Is(LexicalTokenType.NEWLINE))
				return token;

			if (at.Column == _indent.Peek())
				return token;

			_current = token;
			_currentColumn = at.Column;
			if (at.Column > _indent.Peek())
			{
				_indent.Push(at.Column);
				return new LexicalToken(LexicalTokenType.INDENT, "", token.Position);
			}
			_indent.Pop();
			return new LexicalToken(LexicalTokenType.UNDENT, "", token.Position);
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

		public void Back()
		{
			if (_count <= _back)
				throw new InvalidOperationException();
			++_back;
		}

		public LexicalToken GetNextToken(CharStream stream, Func<LexicalToken> source)
		{
			LexicalToken token;
			if (_back > 0)
			{
				--_back;
				--_count;
				if (--_top < 0)
					_top = _trace.Length - 1;
				token = _trace[_top];
			}
			else
			{
				if (_count < _trace.Length)
					++_count;
				token = source();
				_trace[_top] = token;
				if (++_top == _trace.Length)
					_top = 0;
			}
			return token;
		}
	}
}


