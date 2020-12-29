using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Tokenizer2
{
	public interface ITokenFilter
	{
		LexicalToken GetNextToken(TokenScanner.TokenParser source, in TokenScanner scanner);
	}

	public class PushFilter: ITokenFilter
	{
		private readonly Stack<LexicalToken> _stack = new Stack<LexicalToken>();

		public void Push(LexicalToken value)
		{
			_stack.Push(value);
		}

		public LexicalToken GetNextToken(TokenScanner.TokenParser source, in TokenScanner scanner)
		{
			return _stack.Count > 0 ? _stack.Pop() : source(scanner);
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

		public LexicalToken GetNextToken(TokenScanner.TokenParser source, in TokenScanner scanner)
		{
			if (_back)
			{
				_back = false;
				return _last;
			}
			return _last = source(scanner);
		}
	}

	public class IndentFilter: ITokenFilter
	{
		private readonly Stack<int> _indent;
		private CharPosition _last;
		private LexicalToken _current;
		private int _currentColumn;
		private bool _start;

		public IndentFilter()
		{
			_indent = new Stack<int>();
			_start = true;
		}

		public LexicalToken GetNextToken(TokenScanner.TokenParser source, in TokenScanner scanner)
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
			LexicalToken token = source(scanner);
			if (token == null)
			{
				if (_indent.Count <= 1)
					return null;
				_indent.Pop();
				return new LexicalToken(LexicalTokenType.UNDENT, "", _last.Position);
			}
			CharPosition at = scanner.Stream.GetPosition(token.Position, _last);
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

		public LexicalToken GetNextToken(TokenScanner.TokenParser source, in TokenScanner scanner)
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
				token = source(scanner);
				_trace[_top] = token;
				if (++_top == _trace.Length)
					_top = 0;
			}
			return token;
		}
	}
}
