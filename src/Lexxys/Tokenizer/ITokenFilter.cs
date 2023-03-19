// Lexxys Infrastructural library.
// file: ITokenFilter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Tokenizer
{
	public interface ITokenFilter
	{
		//LexicalToken GetNextToken(TokenScanner.TokenParser parser, in TokenScanner scanner, ref CharStream stream);
		LexicalToken GetNextToken(ITokenParser parser, in TokenScanner scanner, ref CharStream stream);
		void Reset();
	}

	public interface ITokenParser
	{
		LexicalToken GetNextToken(in TokenScanner scanner, ref CharStream stream);
		void Reset();
	}


	public static class TokenFilter
    {
        public static ITokenFilter Create(Func<LexicalToken, bool> predicate) => new Filter(predicate);

        private class Filter: ITokenFilter
        {
            private readonly Func<LexicalToken, bool> _predicate;

            public Filter(Func<LexicalToken, bool> predicate)
            {
                _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            }
            
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

		public LexicalToken GetNextToken(ITokenParser source, in TokenScanner scanner, ref CharStream stream)
			=> _stack.Count > 0 ? _stack.Pop(): source.GetNextToken(in scanner, ref stream);

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
			_value = value;
			_back = false;
		}

		public LexicalToken GetNextToken(ITokenParser source, in TokenScanner scanner, ref CharStream stream)
		{
			if (_back)
			{
				_back = false;
				return Value;
			}
			return _value = source.GetNextToken(in scanner, ref stream);
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

		public LexicalToken GetNextToken(ITokenParser source, in TokenScanner scanner, ref CharStream stream)
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
			LexicalToken token = source.GetNextToken(scanner, ref stream);
			if (token.IsEmpty)
			{
				if (_indent.Count == 0)
					return LexicalToken.Empty;
				_indent.Pop();
				return new LexicalToken(LexicalTokenType.UNDENT, _last.Position, 0);
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

		public LexicalToken GetNextToken(ITokenParser source, in TokenScanner scanner, ref CharStream stream)
		{
			if (_back == 0)
				return Push(source.GetNextToken(scanner, ref stream));

			--_back;
			return Pop();
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
			return _trace[_top == 0 ? _trace.Length - 1 : _top];
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
}
