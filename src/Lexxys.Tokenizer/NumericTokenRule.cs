// Lexxys Infrastructural library.
// file: NumericTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Globalization;
using System.Text;

namespace Lexxys.Tokenizer;

[Flags]
public enum NumericTokenStyles
{
	Ordinal = 0,

	AllowDecimalPoint = 1,
	AllowExponent = 2,
	AllowHexadecimal = 4,
	AllowOctal = 8,
	AllowBinary = 16,
	StartingWithDot = 32,
	EndingWithDot = 64,
	NegativeSign = 128,
	PositiveSign = 256,

	Integer = NegativeSign | AllowBinary | AllowOctal | AllowHexadecimal,
	Decimal = NegativeSign | AllowDecimalPoint,
	Double = Decimal | AllowExponent,
}

/// <summary>
/// A <see cref="LexicalTokenRule"/> for numerical values.
/// </summary>
[Serializable]
public class NumericTokenRule: LexicalTokenRule
{
	private const char DecimalPoint = '.';
	private const char ExponentChar1 = 'E';
	private const char ExponentChar2 = 'e';

	private readonly NumericTokenStyles _style;

	public NumericTokenRule()
	{
		_style = NumericTokenStyles.Double;
		BeginningChars = DigitsWithSign;
		TokenType = LexicalTokenType.NUMERIC;
	}

	public NumericTokenRule(LexicalTokenType numeric)
	{
		_style = NumericTokenStyles.Double;
		BeginningChars = DigitsWithSign;
		TokenType = numeric;
	}

	public NumericTokenRule(NumericTokenStyles style): this(LexicalTokenType.NUMERIC, style)
	{
	}

	public NumericTokenRule(LexicalTokenType numeric, NumericTokenStyles style)
	{
		_style = style;
		var start = Digits;
		if ((_style & NumericTokenStyles.StartingWithDot) != 0)
			start += ".";
		if ((_style & NumericTokenStyles.NegativeSign) != 0)
			start += "-";
		if ((_style & NumericTokenStyles.PositiveSign) != 0)
			start += "+";
		BeginningChars = start;
		TokenType = numeric;
	}

	private const string Digits = "0123456789";
	private const string DigitsWithSign = "0123456789-";

	public override string? BeginningChars { get; }

	public LexicalTokenType TokenType { get; }

	public override bool TestBeginning(char value) => value switch {
		>='0' and <='9' => true,
		'.' => (_style & NumericTokenStyles.StartingWithDot) != 0,
		'-' => (_style & NumericTokenStyles.NegativeSign) != 0,
		'+' => (_style & NumericTokenStyles.PositiveSign) != 0,
		_ => false
	};

	public override LexicalToken TryParse(ref CharStream stream)
	{
		var text = new StringBuilder();
		int i = 0;
		char ch = stream[0];
		bool dec = false;
		bool exp = false;

		if (ch is '-' or '+')
		{
			if (ch == '-' && (_style & NumericTokenStyles.NegativeSign) == 0 || ch == '+' && (_style & NumericTokenStyles.PositiveSign) == 0)
				return LexicalToken.Empty;
			if (ch == '-')
				text.Append(ch);
			ch = stream[1];
			i = 1;
		}

		if (ch is >= '0' and <= '9')
		{
			if (ch == '0' && (i + 1) < stream.Length)
			{
				ch = stream[++i];
				if (ch is 'o' or 'O' or 'x' or 'X' or 'b' or 'B')
					return TryParseBinary(ref stream, i);
				while (ch == '0')
				{
					ch = stream[++i];
				}
			}
			while (ch >= '0' && ch <= '9')
			{
				text.Append(ch);
				ch = stream[++i];
			}
			if (text.Length == 0)
				text.Append('0');
		}

		if (ch == DecimalPoint && (_style & NumericTokenStyles.AllowDecimalPoint) != 0)
		{
			if (i == 0 && (_style & NumericTokenStyles.StartingWithDot) == 0)
				return LexicalToken.Empty;

			ch = stream[++i];
			if (ch is >='0' and <='9')
			{
				if (text.Length == 0)
					text.Append('0');
				text.Append(DecimalPoint);
				do
				{
					text.Append(ch);
					ch = stream[++i];
				} while (ch is >='0' and <='9');
			}
			else if (text.Length == 0 || (_style & NumericTokenStyles.EndingWithDot) == 0)
			{
				return LexicalToken.Empty;
			}
			dec = true;
		}

		if (text.Length == 0)
			return LexicalToken.Empty;

		if ((_style & NumericTokenStyles.AllowExponent) != 0)
		{
			int i0 = i;
			while (ch == ' ')
			{
				ch = stream[++i];
			}
			if (ch == ExponentChar1 || ch == ExponentChar2)
			{
				do
				{
					ch = stream[++i];
				} while (ch == ' ');

				bool minus = ch == '-';
				if (ch is '-' or '+')
					ch = stream[++i];
				if (ch is >='0' and <='9')
				{
					text.Append(ExponentChar1);
					if (minus)
						text.Append('-');
					do
					{
						text.Append(ch);
						ch = stream[++i];
					} while (ch is >='0' and <='9');
					exp = true;
					i0 = i;
				}
			}
			i = i0;
		}

		string s = text.ToString();

		if (!exp && !dec && Int64.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long n))
		{
			if (n > Int32.MaxValue || n < Int32.MinValue)
				return stream.Token(TokenType, i, (_, _) => n);
			int m = (int)n;
			return stream.Token(TokenType, i, (_, _) => m);
		}
		else if (!exp && Decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal d))
		{
			return stream.Token(TokenType, i, (_, _) => d);	
		}
		else if (Double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double f))
		{
			return stream.Token(TokenType, i, (_, _) => f);
		}
		else
		{
			return LexicalToken.Empty;
		}
	}

	private LexicalToken TryParseBinary(ref CharStream stream, int i)
	{
		const ulong HighBit = 0x80ul << ((sizeof(ulong) - 1) * 8);
		const ulong HighOctalBit = 0xE0ul << ((sizeof(ulong) - 1) * 8);
		const ulong HighHexBit = 0xF0ul << ((sizeof(ulong) - 1) * 8);

		if (++i == stream.Length)
			return LexicalToken.Empty;
		ulong x = 0;
		bool negative = stream[0] == '-';
		char ch = stream[i];
		int i0 = i;
		switch (stream[i - 1])
		{
			case 'b':
			case 'B':
				if ((_style & NumericTokenStyles.AllowBinary) == 0)
					return LexicalToken.Empty;
				while (ch == '0' || ch == '1')
				{
					if ((x & HighBit) != 0)
						return LexicalToken.Empty;
					x <<= 1;
					if (ch == '1')
						x |= 1;
					ch = stream[++i];
				}
				break;

			case 'o':
			case 'O':
				if ((_style & NumericTokenStyles.AllowOctal) == 0)
					return LexicalToken.Empty;
				while (ch >= '0' && ch <= '7')
				{
					if ((x & HighOctalBit) != 0)
						return LexicalToken.Empty;
					x <<= 3;
					x |= (uint)(ch - '0');
					ch = stream[++i];
				}
				break;

			case 'x':
			case 'X':
				if ((_style & NumericTokenStyles.AllowHexadecimal) == 0)
					return LexicalToken.Empty;
				while (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f')
				{
					if ((x & HighHexBit) != 0)
						return LexicalToken.Empty;
					x <<= 4;
					if (ch >= '0' && ch <= '9')
						x |= (uint)(ch - '0');
					else if (ch >= 'a' && ch <= 'f')
						x |= (uint)(10 + ch - 'a');
					else if (ch >= 'A' && ch <= 'F')
						x |= (uint)(10 + ch - 'A');
					ch = stream[++i];
				}
				break;

			default:
				return LexicalToken.Empty;
		}

		if (i == i0)
			return LexicalToken.Empty;

		if (negative)
		{
			if ((x & HighBit) != 0)
			{
				return x == HighBit ? stream.Token(TokenType, i, (_, _) => long.MinValue): LexicalToken.Empty;
			}
			else if (x < Int32.MaxValue + 1u)
			{
				int n = (int)-(long)x;
				return stream.Token(TokenType, i, (_, _) => n);
			}
			else
			{
				long n = -(long)x;
				return stream.Token(TokenType, i, (_, _) => n);
			}
		}
		else
		{
			if (x <= Int32.MaxValue)
			{
				int n = (int)x;
				return stream.Token(TokenType, i, (_, _) => n);
			}
			else if (x <= Int64.MaxValue)
			{
				long n = (long)x;
				return stream.Token(TokenType, i, (_, _) => n);
			}
			else
			{
				return stream.Token(TokenType, i, (_, _) => x);
			}
		}
	}
}
