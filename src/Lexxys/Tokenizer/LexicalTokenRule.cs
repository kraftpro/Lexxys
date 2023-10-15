// Lexxys Infrastructural library.
// file: LexicalTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Tokenizer;

/// <summary>
/// The rule used by <see cref="TokenScanner"/> to extract tokens.
/// </summary>
[Serializable]
public abstract class LexicalTokenRule
{
	/// <summary>
	/// List of the possible starting characters for the parsing token.
	/// </summary>
	public virtual string? BeginningChars => null;

	/// <summary>
	/// Specifies that the parsing token could contains extra characters not included in the <see cref="BeginningChars"/>.
	/// </summary>
	public virtual bool HasExtraBeginning => false;

	/// <summary>
	/// Tests that the specified <paramref name="value"/> could be start of a new token.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public virtual bool TestBeginning(char value)
	{
		string? s = BeginningChars;
		return s == null || s.IndexOf(value) >= 0;
	}

	/// <summary>
	/// Tries to extract a token from the <paramref name="stream"/>.
	/// </summary>
	/// <param name="stream"></param>
	/// <returns>Extracted token or null.</returns>
	public abstract LexicalToken TryParse(ref CharStream stream);
}
