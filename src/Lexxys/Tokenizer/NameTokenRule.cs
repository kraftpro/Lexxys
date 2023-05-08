// Lexxys Infrastructural library.
// file: NameTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Tokenizer;

public class NameTokenRule: LexicalTokenRule
{
	public const string DefaultBeginning = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
	private string _beginning = DefaultBeginning;
	private bool _extra = true;
	private Func<char, bool> _isNameStartCharacter = IsNameStartCharacter;
	private Func<char, bool> _isNamePartCharacter = IsNamePartCharacter;
	private Func<char, bool> _isNameEndCharacter = _ => true;

	public NameTokenRule(LexicalTokenType nameTokenType)
	{
		TokenType = nameTokenType;
	}

	public NameTokenRule(): this(LexicalTokenType.IDENTIFIER)
	{
	}

	public override string BeginningChars => _beginning;

	public override bool HasExtraBeginning => _extra;

	public override bool TestBeginning(char value) => _isNameStartCharacter(value);

	public NameTokenRule WithNameRecognition(Func<char, bool>? nameStart = null, Func<char, bool>? namePart = null, Func<char, bool>? nameEnd = null, string beginning = "", bool? extra = null)
	{
		if (nameStart != null)
			NameStartCharacter = nameStart;
		if (namePart != null)
			NamePartCharacter = namePart;
		if (nameEnd != null)
			NameEndCharacter = nameEnd;
		if (!String.IsNullOrEmpty(beginning))
			_beginning = beginning;
		if (extra != null)
			_extra = extra.GetValueOrDefault();
		return this;
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

	public Func<char, bool> NameEndCharacter
	{
		get => _isNameEndCharacter;
		set => _isNameEndCharacter = value ?? throw new ArgumentNullException(nameof(value));
	}

	public LexicalTokenType TokenType { get; }

	public override LexicalToken TryParse(ref CharStream stream)
	{
		char ch = stream[0];
		if (!_isNameStartCharacter(ch))
			return LexicalToken.Empty;

		int i = 0;
		do
		{
			ch = stream[++i];
		} while (_isNamePartCharacter(ch));

		while (i > 0 && !_isNameEndCharacter(stream[i - 1]))
		{
			--i;
		}
		if (i == 0)
			return LexicalToken.Empty;

		return stream.Token(TokenType, i);
	}

	private static bool IsNameStartCharacter(char value) => Char.IsLetter(value) || value == '_';

	private static bool IsNamePartCharacter(char value) => Char.IsLetterOrDigit(value) || value == '_';
}
