// Lexxys Infrastructural library.
// file: NumericTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

namespace Lexxys.Tokenizer
{
	[Flags]
	public enum NumericTokenStyles
	{
		None = 0,

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

		public NumericTokenRule(NumericTokenStyles style)
			: this(LexicalTokenType.NUMERIC, style)
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
			>= '0' and <= '9' => true,
			'.' => (_style & NumericTokenStyles.StartingWithDot) != 0,
			'-' => (_style & NumericTokenStyles.NegativeSign) != 0,
			'+' => (_style & NumericTokenStyles.PositiveSign) != 0,
			_ => false
		};

		public override LexicalToken? TryParse(CharStream stream)
		{
			var text = new StringBuilder();
			int i = 0;
			char ch = stream[0];
			bool dec = false;
			bool exp = false;

			if (ch == '-' || ch == '+')
			{
				if (ch == '-')
					text.Append(ch);
				ch = stream[1];
				++i;
			}

			if (ch >= '0' && ch <= '9')
			{
				if (ch == '0' && "oOxXbB".Contains(stream[i + 1]))
					return TryParseBinary(stream);
				while (ch == '0')
				{
					ch = stream[++i];
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
					return null;

				ch = stream[++i];
				if (ch >= '0' && ch <= '9')
				{
					if (text.Length == 0)
						text.Append('0');
					text.Append(DecimalPoint);
					do
					{
						text.Append(ch);
						ch = stream[++i];
					} while (ch >= '0' && ch <= '9');
				}
				else if (text.Length == 0 || (_style & NumericTokenStyles.EndingWithDot) == 0)
				{
					return null;
				}
				dec = true;
			}

			if (text.Length == 0)
				return null;

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
					if (ch == '-' || ch == '+')
						ch = stream[++i];
					if (ch >= '0' && ch <= '9')
					{
						text.Append(ExponentChar1);
						if (minus)
							text.Append('-');
						do
						{
							text.Append(ch);
							ch = stream[++i];
						} while (ch >= '0' && ch <= '9');
						exp = true;
						i0 = i;
					}
				}
				i = i0;
			}

			string s = text.ToString();

			if (!exp && !dec && Int64.TryParse(s, out long n))
				return n <= Int32.MaxValue && n >= Int32.MinValue ? stream.Token(TokenType, i, s, (int)n): stream.Token(TokenType, i, s, n);
			if (!exp && Decimal.TryParse(s, out decimal d))
				return stream.Token(TokenType, i, s, d);
			if (Double.TryParse(s, out double f))
				return stream.Token(TokenType, i, s, f);

			return null;
		}

		private LexicalToken? TryParseBinary(CharStream stream)
		{
			ulong x = 0;
			int i = 2;
			char ch = stream[2];
			switch (stream[1])
			{
				case 'b':
				case 'B':
					if ((_style & NumericTokenStyles.AllowBinary) == 0)
						return null;
					while (ch == '0' || ch == '1')
					{
						if ((x & 0x800000000000u) != 0)
							return null;
						x <<= 1;
						if (ch == '1')
							x |= 1;
						ch = stream[++i];
					}
					break;

				case 'o':
				case 'O':
					if ((_style & NumericTokenStyles.AllowOctal) == 0)
						return null;
					while (ch >= '0' && ch <= '7')
					{
						if ((x & 0xE00000000000u) != 0)
							return null;
						x <<= 3;
						x |= (uint)(ch - '0');
						ch = stream[++i];
					}
					break;

				case 'x':
				case 'X':
					if ((_style & NumericTokenStyles.AllowHexadecimal) == 0)
						return null;
					while (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f')
					{
						if ((x & 0xF00000000000u) != 0)
							return null;
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
					return null;
			}

			if (i == 2)
				return null;

			return x > Int32.MaxValue ? stream.Token(TokenType, i, (long)x): stream.Token(TokenType, i, (int)x);
		}
	}
}
