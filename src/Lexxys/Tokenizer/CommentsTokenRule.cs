// Lexxys Infrastructural library.
// file: CommentsTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.Tokenizer
{
	public class CommentsTokenRule: LexicalTokenRule
	{
		private readonly string _beginning;
		private readonly string[] _startEnd;
		private readonly int _startLength;
		private static readonly string[] _cppComments = { "//", "\n" };

		public CommentsTokenRule(params string[] startEnd)
			: this(LexicalTokenType.COMMENT, startEnd)
		{
		}

		public CommentsTokenRule(LexicalTokenType comment, params string[] startEnd)
		{
			TokenType = comment;
			if (startEnd == null || startEnd.Length == 0)
			{
				_startEnd = _cppComments;
			}
			else
			{
				_startEnd = new string[(startEnd.Length + 1) & ~1];
				_startEnd[_startEnd.Length - 1] = "\n";
				startEnd.CopyTo(_startEnd, 0);
				for (int i = 0; i < startEnd.Length; ++i)
				{
					if (startEnd[i] == null || startEnd[i].Length == 0)
						throw new ArgumentOutOfRangeException($"startEnd[{i}]", startEnd[i], null);
				}
			}
			_startLength = 0;
			string beginning = "";
			for (int i = 0; i < _startEnd.Length; i += 2)
			{
				if (_startEnd[i].Length > _startLength)
					_startLength = _startEnd[i].Length;
				if (beginning.IndexOf(_startEnd[i][0]) < 0)
					beginning += _startEnd[i][0].ToString();
			}
			_beginning = beginning;
		}
		protected CommentsTokenRule(LexicalTokenType tokenType)
		{
			TokenType = tokenType;
		}
		
		public override string BeginningChars => _beginning;

		public LexicalTokenType TokenType { get; }

		public override bool TestBeginning(char value)
		{
			return _beginning.Contains(value);
		}

		public override LexicalToken TryParse(CharStream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			string commentStart = null;
			string commentEnd = null;
			string s = stream.Substring(0, _startLength);
			for (int i = 0; i < _startEnd.Length; i += 2)
			{
				if (s.StartsWith(_startEnd[i], StringComparison.Ordinal))
				{
					commentStart = _startEnd[i];
					commentEnd = _startEnd[i + 1];
					break;
				}
			}
			if (commentStart == null)
				return null;

			int position = stream.IndexOf(commentEnd, commentStart.Length);
			if (position < 0)
			{
				if (commentEnd == "\n")
					position = stream.Length;
				else
					throw stream.SyntaxException(SR.EofInComments());
			}
			string comment = stream.Substring(commentStart.Length, position - commentStart.Length);
			return stream.Token(TokenType, position + (commentEnd == "\n" ? 0: commentEnd.Length), comment);
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

		public override bool TestBeginning(char value)
		{
			return value == '/';
		}

		public override LexicalToken TryParse(CharStream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			if (stream[0] == '/')
			{
				if (stream[1] == '/')
				{
					int i = stream.IndexOf('\n', 2);
					if (i < 0)
						i = stream.Length;
					return stream.Token(TokenType, i, stream.Substring(2, i - 2));
				}
				if (stream[1] == '*')
				{
					int i = stream.IndexOf("*/", 2);
					if (i < 0)
						throw stream.SyntaxException(SR.EofInComments());
					return stream.Token(TokenType, i + 2, stream.Substring(2, i - 2));
				}
			}
			return null;
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

		public override bool TestBeginning(char value)
		{
			return value == '#';
		}

		public override LexicalToken TryParse(CharStream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			if (stream[0] == '#')
			{
				int i = stream.IndexOf('\n', 2);
				if (i < 0)
					i = stream.Length;
				return stream.Token(TokenType, i, stream.Substring(2, i - 2));
			}
			return null;
		}
	}
}
