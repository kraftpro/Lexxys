// Lexxys Infrastructural library.
// file: Six.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con
{
	internal class Six
	{
		private static string _charLine = "abcdefghijklmnopqrstuvwxyz.ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

		public static char SixToChar(int index)
		{
			return _charLine[index & 63];
		}

		public static int CharToSix(char value)
		{
			if ((int)value >= 97)
			{
				if ((int)value <= 122)
					return (int)value - 97;
				else
					return -1;
			}
			else if ((int)value >= 65)
			{
				if ((int)value <= 90)
					return (int)value - 65 + 27;
				return (int)value == 95 ? 63: -1;
			}
			else if ((int)value >= 48)
			{
				if ((int)value <= 57)
					return (int)value - 48 + 53;
				else
					return -1;
			}
			else
				return (int)value == 46 ? 26: -1;
		}

		public static string Encode(byte[] bits)
		{
			StringBuilder result = new StringBuilder((bits.Length*8 + 5)/6);
			int index = Encode(bits, result);
			if (index > 0)
				result.Append(SixToChar(index));
			return ((object)result).ToString();
		}

		public static int Encode(byte[] bits, StringBuilder result)
		{
			int index1 = 0;
			int num = 0;
			for (int index2 = 0; index2 < bits.Length; ++index2)
			{
				index1 |= (int)bits[index2] << num;
				num += 8;
				while (num > 6)
				{
					result.Append(SixToChar(index1));
					index1 >>= 6;
					num -= 6;
				}
			}
			return num << 8 | index1;
		}

		public static int DecodedLength(int encodedLength, bool keepRest)
		{
			if (encodedLength < 0 || encodedLength > 357913934)
				throw new ArgumentOutOfRangeException(nameof(encodedLength), encodedLength, null);
			if (!keepRest)
				return encodedLength*6/8;
			else
				return (encodedLength*6 + 7)/8;
		}

		public static int DecodedRest(int encodedLength)
		{
			if (encodedLength < 0 || encodedLength > 357913941)
				throw Lexxys.EX.ArgumentOutOfRange("encodedLength", (object)encodedLength);
			else
				return encodedLength*6%8;
		}

		public static int EncodedLength(int decodedLength, bool keepRest)
		{
			if (decodedLength < 0 || decodedLength > 268435450)
				throw Lexxys.EX.ArgumentOutOfRange("decodedLength", (object)decodedLength);
			if (!keepRest)
				return decodedLength*8/6;
			else
				return (decodedLength*8 + 5)/6;
		}

		public static int EncodedRest(int decodedLength)
		{
			if (decodedLength < 0 || decodedLength > 268435455)
				throw new ArgumentOutOfRangeException(nameof(decodedLength), decodedLength, null);
			else
				return decodedLength*8%6;
		}

		public static byte[] Decode(string value)
		{
			int rest;
			return Decode(value, out rest);
		}

		public static byte[] Decode(string value, out int rest)
		{
			rest = 0;
			byte[] numArray = new byte[value.Length*3/4];
			int num1 = 0;
			int num2 = 0;
			int num3 = 0;
			for (int index = 0; index < value.Length; ++index)
			{
				int num4 = CharToSix(value[index]);
				if (num4 == -1)
					return (byte[])null;
				num1 |= num4 << num2;
				num2 += 6;
				if (num2 >= 8)
				{
					numArray[num3++] = (byte)(num1 & (int)byte.MaxValue);
					num1 >>= 8;
					num2 -= 8;
				}
			}
			rest = num2 << 8 | num1;
			return numArray;
		}

		public static int Decode(string value, byte[] bits)
		{
			if (value == null)
				throw Lexxys.EX.ArgumentNull("value");
			if (bits == null)
				throw Lexxys.EX.ArgumentNull("bits");
			int num1 = 0;
			int num2 = 0;
			int index1 = 0;
			for (int index2 = 0; index2 < value.Length; ++index2)
			{
				int num3 = CharToSix(value[index2]);
				if (num3 == -1)
					throw Lexxys.EX.ArgumentOutOfRange("value[i]", (object)value[index2]);
				num1 |= num3 << num2;
				num2 += 6;
				if (num2 >= 8)
				{
					if (index1 == bits.Length)
						return (value.Length - index2 - 1)*6 + num2;
					bits[index1++] = (byte)(num1 & (int)byte.MaxValue);
					num1 >>= 8;
					num2 -= 8;
				}
			}
			if (num2 > 0 && index1 < bits.Length)
			{
				bits[index1] = (byte)(num1 & (int)byte.MaxValue);
				num2 = 0;
			}
			return num2;
		}

		public static string GenerateSessionId()
		{
			byte[] bits = Guid.NewGuid().ToByteArray();
			StringBuilder result = new StringBuilder(24);
			int num = Encode(bits, result);
			int index = (((object)result).ToString().GetHashCode() & (int)ushort.MaxValue) << 2 | num & 3;
			result.Append(SixToChar(index));
			result.Append(SixToChar(index >> 6));
			result.Append(SixToChar(index >> 12));
			return ((object)result).ToString();
		}

		public static bool IsWellFormedSessionId(string sessionId)
		{
			if (sessionId == null || sessionId.Length != 24)
				return false;
			int num = sessionId.Substring(0, 21).GetHashCode() & (int)ushort.MaxValue;
			return (CharToSix(sessionId[21]) | CharToSix(sessionId[22]) << 6 | CharToSix(sessionId[23]) << 12) >> 2 == num;
		}
	}
}

