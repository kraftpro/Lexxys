using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Tokenizer2
{
	public interface ITokenFilter<T> where T: IEquatable<T>
	{
		LexicalTokenT GetNextToken(TokenScanner<T>.TokenParser source, in TokenScanner<T> scanner);
		//bool TryGetToken(in TokenScanner<T> scanner, out LexicalTokenT token);
	}

	public class PushFilter<T> : ITokenFilter<T> where T : IEquatable<T>
	{
		private readonly Stack<LexicalTokenT> _stack = new Stack<LexicalTokenT>();

		public void Push(LexicalTokenT value)
		{
			_stack.Push(value);
		}

		public LexicalTokenT GetNextToken(TokenScanner<T>.TokenParser source, in TokenScanner<T> scanner)
		{
			return _stack.Count > 0 ? _stack.Pop() : source(scanner);
		}
	}

	public class OneBackFilter<T> : ITokenFilter<T> where T : IEquatable<T>
	{
		private LexicalTokenT _last;
		private bool _back;

		public void Back()
		{
			if (_back)
				throw new InvalidOperationException();
			_back = true;
		}

		public LexicalTokenT GetNextToken(TokenScanner<T>.TokenParser source, in TokenScanner<T> scanner)
		{
			if (_back)
			{
				_back = false;
				return _last;
			}
			return _last = source(scanner);
		}
	}

	public class IndentFilter<T> : ITokenFilter<T> where T : IEquatable<T>
	{
		private readonly Stack<int> _indent;
		private CharPosition _last;
		private LexicalTokenT _current;
		private int _currentColumn;
		private bool _start;

		public IndentFilter()
		{
			_indent = new Stack<int>();
			_start = true;
		}

		public LexicalTokenT GetNextToken(TokenScanner<T>.TokenParser source, in TokenScanner<T> scanner)
		{
			if (_current != default)
			{
				if (_indent.Count > 0)
				{
					if (_currentColumn < _indent.Peek())
					{
						_indent.Pop();
						return new LexicalTokenT(LexicalTokenType.UNDENT, _current.Position, 0);
					}
					if (_currentColumn > _indent.Peek())
					{
						_indent.Push(_currentColumn);
						return new LexicalTokenT(LexicalTokenType.INDENT, _current.Position, 0);
					}
				}
				LexicalTokenT tmp = _current;
				_current = default;
				return tmp;
			}
			LexicalTokenT token = source(scanner);
			if (token == default)
			{
				if (_indent.Count <= 1)
					return default;
				_indent.Pop();
				return new LexicalTokenT(LexicalTokenType.UNDENT, _last.Position, 0);
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
				return new LexicalTokenT(LexicalTokenType.INDENT, token.Position, 0);
			}
			_indent.Pop();
			return new LexicalTokenT(LexicalTokenType.UNDENT, token.Position, 0);
		}
	}

	public class NthBackFilter<T> : ITokenFilter<T> where T : IEquatable<T>
	{
		private readonly LexicalTokenT[] _trace;
		private int _top;
		private int _count;
		private int _back;

		public NthBackFilter(int count)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, null);

			_trace = new LexicalTokenT[count];
		}

		public void Back()
		{
			if (_count <= _back)
				throw new InvalidOperationException();
			++_back;
		}

		public LexicalTokenT GetNextToken(TokenScanner<T>.TokenParser source, in TokenScanner<T> scanner)
		{
			LexicalTokenT token;
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
