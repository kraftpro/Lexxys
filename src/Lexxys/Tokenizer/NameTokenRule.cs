// Lexxys Infrastructural library.
// file: NameTokenRule.cs
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
	public class NameTokenRule: LexicalTokenRule
	{
		public const string DefaultBeginning = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
		private string _beginning = DefaultBeginning;
		private bool _extra = true;
		private Func<char, bool> _isNameStartCharacter = o => IsNameStartCharacter(o);
		private Func<char, bool> _isNamePartCharacter = o => IsNamePartCharacter(o);
		private Func<string, string> _cleanupResult;

		public NameTokenRule(LexicalTokenType nameTokenType, bool ignoreCase)
		{
			TokenType = nameTokenType;
			if (ignoreCase)
				_cleanupResult = ToUppercase;
		}

		public NameTokenRule(bool ignoreCase)
			: this(LexicalTokenType.IDENTIFIER, ignoreCase)
		{
		}

		public NameTokenRule()
			: this(LexicalTokenType.IDENTIFIER, false)
		{
		}

		public override string BeginningChars => _beginning;

		public override bool HasExtraBeginning => _extra;

		public override bool TestBeginning(char value)
		{
			return _isNameStartCharacter(value);
		}

		public NameTokenRule WithNameRecognition(Func<char, bool> nameStart, Func<char, bool> namePart, Func<string, string> cleanup)
		{
			if (nameStart != null)
				NameStartCharacter = nameStart;
			if (namePart != null)
				NamePartCharacter = namePart;
			if (cleanup != null)
				CleanupResult = cleanup;
			return this;
		}

		public NameTokenRule WithNameRecognition(Func<char, bool> nameStart, Func<char, bool> namePart, Func<string, string> cleanup, string beginning, bool extra)
		{
			if (nameStart != null)
				NameStartCharacter = nameStart;
			if (namePart != null)
				NamePartCharacter = namePart;
			if (cleanup != null)
				CleanupResult = cleanup;
			SetBeginning(beginning, extra);
			return this;
		}

		public void SetBeginning(string beginning, bool extra)
		{
			_beginning = beginning;
			_extra = extra;
		}

		public Func<char, bool> NameStartCharacter
		{
			get => _isNameStartCharacter;
			set => _isNameStartCharacter = value ?? throw new ArgumentNullException(nameof(value));
		}

		public Func<char, bool> NamePartCharacter
		{
			get => _isNamePartCharacter;
			set => _isNamePartCharacter = value ?? throw new ArgumentNullException(nameof(value));
		}

		public Func<string, string> CleanupResult
		{
			get => _cleanupResult;
			set => _cleanupResult = value ?? throw new ArgumentNullException(nameof(value));
		}

		public LexicalTokenType TokenType { get; }

		public override LexicalToken TryParse(CharStream stream)
		{
			char ch = stream[0];
			if (!_isNameStartCharacter(ch))
				return null;

			int i = 0;
			do
			{
				ch = stream[++i];
			} while (_isNamePartCharacter(ch));

			string text = stream.Substring(0, i);
			if (_cleanupResult != null)
			{
				text = _cleanupResult(text);
				if (text == null || text.Length == 0)
					return null;
			}
			return stream.Token(TokenType, text.Length, text);
		}

		public static bool IsNameStartCharacter(char value)
		{
			return (Char.IsLetter(value) || value == '_');
		}

		public static bool IsNamePartCharacter(char value)
		{
			return (Char.IsLetterOrDigit(value) || value == '_');
		}

		public static string ToUppercase(string name)
		{
			return name.ToUpperInvariant();
		}
	}
}


