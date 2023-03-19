// Lexxys Infrastructural library.
// file: SixBitsCoder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lexxys
{
	/// <summary>
	/// Custom base64 encoder/decoder
	/// </summary>
	public static class SixBitsCoder
	{
		//								 0123456789012345678901234567890123456789012345678901234567890123
		//								 0.........1.........2.........3.........4.........5.........6...
		//								 0...............1...............2...............3...............
		//								 0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF
		private const string CharLine = "-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
		private const string CharLin2 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
		private const string CharLin3L= "0123456789abcdefghijklmnopqrstuvwxyz";

		/// <summary>
		/// Convert low 6 bits of integer value to character
		/// </summary>
		/// <param name="index">Value to convert</param>
		/// <returns>Resulting character</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char BitsToChar(int index) => CharLine[index & 0x3F];

		/// <summary>
		/// Convert character to 6 bits value
		/// </summary>
		/// <param name="value">Character to convert</param>
		/// <returns>Integer value in the rage 0..63 or -1 if <paramref name="value"/> has invalid character</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int CharToBits(char value) => value switch
		{
			>='0' and <='9' => value - ('0' - 1),
			>='A' and <='Z' => value - ('A' - 11),
			>='a' and <='z' => value - ('a' - 38),
			'-' or '.' => 0, 
			'_' => 37, 
			_ => -1
		};

		/// <summary>
		/// Encode byte array <paramref name="bits"/> using 64 characters map.
		/// </summary>
		/// <param name="bits">Bytes to encode</param>
		/// <returns>Encoded string</returns>
		/// <remarks>
		/// 1 byte	-> 2 characters	( last char for 01.00xx )
		/// 2 bytes	-> 3 characters ( last char for 11.xxxx )
		/// 3 bytes	-> 4 characters
		/// </remarks>
		public static string Encode(byte[] bits)
		{
			if (bits == null)
				throw new ArgumentNullException(nameof(bits));
			var result = new StringBuilder((bits.Length * 8 + 5)/ 6);
			int rest = Encode(bits, result);
			if (rest > 0)
				result.Append(BitsToChar(rest));
			return result.ToString();
		}

		/// <summary>
		/// Encode byte array <paramref name="bits"/> using 64 characters map.
		/// </summary>
		/// <param name="bits">Bytes to encode</param>
		/// <param name="result">Output stream to append encoded characters</param>
		/// <returns>Last not encoded bits</returns>
		public static int Encode(byte[] bits, StringBuilder result)
		{
			if (bits == null)
				throw new ArgumentNullException(nameof(bits));
			if (result == null)
				throw new ArgumentNullException(nameof(result));
			int k;
			int i;
			for (i = 2; i < bits.Length; i += 3)
			{
				// xxxxxx|xx xxxx|xxxx xx|xxxxxx|
				k = bits[i] << 16 | bits[i - 1] << 8 | bits[i - 2];
				result
					.Append(BitsToChar(k))
					.Append(BitsToChar(k >> 6))
					.Append(BitsToChar(k >> 12))
					.Append(BitsToChar(k >> 18));
			}
			int n = bits.Length - (i - 2);

			if (n == 0)
				return 0;

			if (n == 1)
			{
				// yy|xxxxxx
				k = bits[i - 2];
				result.Append(BitsToChar(k));
				return (2 << 8) | (k >> 6);
			}

			// [yyyy|xxxx] [xx|xxxxxx]
			k = bits[i - 1] << 8 | bits[i - 2];
			result
				.Append(BitsToChar(k))
				.Append(BitsToChar(k >> 6));
			return (4 << 8) | (k >> 12);
		}

		/// <summary>
		/// Encode byte array <paramref name="bits"/> using 32 characters map.
		/// </summary>
		/// <param name="bits">Bytes to encode</param>
		/// <returns>Encoded string</returns>
		public static string Encode5(byte[] bits)
		{
			if (bits is not { Length: >0})
				return String.Empty;

			var mem = ArrayPool<char>.Shared.Rent((bits.Length * 8 + 4) / 5);
			int n = 0;
			int k = 0;
			int b = 0;
			foreach (var v in bits)
			{
				b |= v << k;
				k += 8;
				do
				{
					mem[n++] = ToChar(b & 0x1F);
					b >>= 5;
					k -= 5;
				} while (k >= 5);
			}
			if (k > 0)
				mem[n++] = ToChar(b & 0x1F);

			var result = new string(mem, 0, n);
			ArrayPool<char>.Shared.Return(mem);
			return result;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static char ToChar(int x) => (char)(x + (x < 10 ? '0': 'a' - 10));
		}

		/// <summary>
		/// Calculate decoded byte array length
		/// </summary>
		/// <param name="encodedLength">Length of encoded string</param>
		/// <returns>Length of decoded byte array</returns>
		public static int DecodedLength(int encodedLength)
		{
			if (encodedLength is <0 or >Int32.MaxValue/8)
				throw new ArgumentOutOfRangeException(nameof(encodedLength), encodedLength, null);
			return (encodedLength * 3) / 4;
		}

		/// <summary>
		/// Calculate length of encoded string buffer
		/// </summary>
		/// <param name="decodedLength">Number of decoded bytes</param>
		/// <param name="keepRest">Preserve extra space for rest of bits</param>
		/// <returns>Length of encoded buffer</returns>
		public static int EncodedLength(int decodedLength, bool keepRest = true)
		{
			if (decodedLength is <0 or >Int32.MaxValue/8)
				throw new ArgumentOutOfRangeException(nameof(decodedLength), decodedLength, null);
			return keepRest ? (decodedLength * 4 + 2) / 3: (decodedLength * 4) / 3;
		}

		/// <summary>
		/// Calculate number of bits to be coded in the last character
		/// </summary>
		/// <param name="decodedLength">Number of decoded bytes</param>
		/// <returns>Number of bits to be coded in the last character</returns>
		public static int EncodedRest(int decodedLength)
		{
			if (decodedLength is <0 or >Int32.MaxValue/8)
				throw new ArgumentOutOfRangeException(nameof(decodedLength), decodedLength, null);
			return (decodedLength * 8) % 6;
		}

		/// <summary>
		/// Decode string into bytes array
		/// </summary>
		/// <param name="value">String to decode</param>
		/// <returns>Decoded bytes</returns>
		public static byte[] Decode(string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			byte[] bits = new byte[(value.Length * 3) / 4];
			int i, j = 0;
			for (i = 3; i < value.Length; i += 4)
			{
				int k =
					CharToBits(value[i - 0]) << 18 |
					CharToBits(value[i - 1]) << 12 |
					CharToBits(value[i - 2]) << 6 |
					CharToBits(value[i - 3]);
				if (k < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
				bits[j++] = (byte)(k);
				bits[j++] = (byte)(k >> 8);
				bits[j++] = (byte)(k >> 16);
			}

			int n = value.Length - (i - 3);
			if (n == 2)
			{
				// xxxxxx][0000 00yy]
				int k = (CharToBits(value[i - 3]) | (CharToBits(value[i - 2]) & 3) << 6);
				bits[j] = (byte)(k);
			}
			else if (n == 3)
			{
				// xx][xxxx| xx.xx][xx|00 xxxx]
				int k = CharToBits(value[i - 3]) |
					(CharToBits(value[i - 2]) << 6) |
					(CharToBits(value[i - 1]) << 12);
				bits[j] = (byte)(k);
				bits[j+1] = (byte)(k >> 8);
			}
			else if (n == 1)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			}

			return bits;
		}

		/// <summary>
		/// Generate random 24 characters length string with injected 16 bits check sum
		/// </summary>
		/// <returns>Generated string</returns>
		public static string GenerateSessionId()
		{
			var bits = Guid.NewGuid().ToByteArray();
			var result = new StringBuilder(24);
			int rest = Encode(bits, result);

			int q = ((HashCode(result.ToString(), 21) & 0xFFFF) << 2) | (rest & 3);
			result.Append(BitsToChar(q));
			result.Append(BitsToChar(q >> 6));
			result.Append(BitsToChar(q >> 12));
			return result.ToString();
		}

		/// <summary>
		/// Test that the string was generated using <see cref="IsWellFormedSessionId"/> function
		/// </summary>
		/// <param name="sessionId">String to test</param>
		/// <returns>true if the <paramref name="sessionId"/> was generated by <see cref="IsWellFormedSessionId"/> function</returns>
		public static bool IsWellFormedSessionId(string? sessionId)
		{
			if (sessionId is not { Length: 24 })
				return false;
			int q = HashCode(sessionId, 21) & 0xFFFF;
			int k = (CharToBits(sessionId[21]) | (CharToBits(sessionId[22]) << 6) | (CharToBits(sessionId[23]) << 12));
			return ((k >> 2) == q);
		}

		/// <summary>
		/// Convert an unsigned <paramref name="value"/> to a base 62 string.
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <returns>The string representation of the specified <paramref name="value"/></returns>
		public static unsafe string Sixty(ulong value)
		{
			if (value == 0)
				return "0";
			char* buffer = stackalloc char[15];
			char* p = buffer + 14;
			*p = '\0';
			while (value > 0)
			{
				*--p = CharLin2[(int)(value % 62)];
				value /= 62;
			}
			return new String(p);
		}

		/// <summary>
		/// Convert a base 62 string <paramref name="value"/> to an unsigned number.
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <returns>The numeric representation of the specified <paramref name="value"/></returns>
		public static ulong Sixty(string? value)
		{
			if (value == null)
				return 0;
			ulong result = 0;
			foreach (char c in value)
			{
				int j = Index(c);
				if (j >= 0)
					result = result * 62 + (ulong)j;
			}
			return result;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static int Index(char c) => c switch
			{
				>='0' and <= '9' => c - '0',
				>='A' and <= 'Z' => c - ('A' - ('9' - '0' + 1)),
				>='a' and <= 'z' => c - ('a' - (('9' - '0' + 1) + ('Z' - 'A' + 1))),
				_ => -1
			};
		}

		/// <summary>
		/// Convert an unsigned <paramref name="value"/> to a base 36 string.
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <returns>The string representation of the specified <paramref name="value"/></returns>
		public static unsafe string Thirty(ulong value)
		{
			if (value == 0)
				return "0";
			char* buffer = stackalloc char[15];
			char* p = buffer + 14;
			*p = '\0';
			while (value > 0)
			{
				*--p = CharLin3L[(int)(value % 36)];
				value /= 36;
			}
			return new String(p);
		}

		/// <summary>
		/// Convert a base 36 string <paramref name="value"/> to an unsigned number.
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <returns>The numeric representation of the specified <paramref name="value"/></returns>
		public static ulong Thirty(string? value)
		{
			if (value == null)
				return 0;
			ulong result = 0;
			foreach (char c in value)
			{
				int j = Index(c);
				if (j >= 0)
					result = result * 36 + (ulong)j;
			}
			return result;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static int Index(char c) => c switch
			{
				>='0' and <= '9' => c - '0',
				>='A' and <= 'Z' => c - ('A' - ('9' - '0' + 1)),
				>='a' and <= 'z' => c - ('a' - ('9' - '0' + 1)),
				_ => -1
			};
		}

		/// <summary>
		/// Hashes the specified <paramref name="id"/> value and converts it to a string. 
		/// </summary>
		/// <param name="id">The value to encode</param>
		/// <param name="encodeMult">Hash value multiplier</param>
		/// <param name="encodeMask">Hash value XOR mask</param>
		/// <param name="convert">Function to convert a numeric value to a string</param>
		/// <param name="straight">Indicates not to swap bytes of the converting value</param>
		/// <returns>An encoded value</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> value is less than 0.</exception>
		/// <exception cref="ArgumentNullException"></exception>
		public static string EncodeId(int id, uint encodeMult, ulong encodeMask, Func<ulong, string> convert, bool straight = false)
		{
			if (id < 0)
				throw new ArgumentOutOfRangeException(nameof(id), id, null);
			if (convert == null)
				throw new ArgumentNullException(nameof(convert));
			var code = (ulong)id * encodeMult ^ encodeMask;
			if (!straight)
				code = Swap64(code);
			return convert(code);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong Swap64(ulong value)
		{
			var x = value << 32 | value >> 32;
			x = (x & 0xFFFF0000_FFFF0000) >> 16 | (x & 0x0000FFFF_0000FFFF) << 16;
			x = (x & 0xFF00FF00_FF00FF00) >> 8  | (x & 0x00FF00FF_00FF00FF) << 8;
			return x;
		}

		/// <summary>
		/// Restores an ID value from the string representation.
		/// </summary>
		/// <param name="value">String value to be decoded to the ID.</param>
		/// <param name="encodeMult">Hash value multiplier</param>
		/// <param name="encodeMask">Hash value XOR mask</param>
		/// <param name="convert">Function to convert a string value to a number</param>
		/// <returns>A decoded value</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static int DecodeId(string? value, uint encodeMult, ulong encodeMask, Func<string, ulong> convert)
		{
			if (convert == null)
				throw new ArgumentNullException(nameof(convert));
			if (value == null)
				return 0;

			ulong source = convert(value);
			if (source == 0)
				return 0;

			ulong code = source ^ encodeMask;
			long r0 = Math.DivRem((long)code, encodeMult, out long r1);
			if (r1 == 0 && (ulong)r0 < int.MaxValue)
				return (int)r0;

			code = Swap64(source) ^ encodeMask;
			r0 = Math.DivRem((long)code, encodeMult, out r1);
			if (r1 == 0 && (ulong)r0 < int.MaxValue)
				return (int)r0;

			return 0;
		}

		/// <summary>
		/// Hashes the specified <paramref name="id"/> value and converts it to a string. 
		/// </summary>
		/// <param name="id">The value to encode</param>
		/// <param name="encodeMult">Hash value multiplier</param>
		/// <param name="encodeMask">Hash value XOR mask</param>
		/// <param name="straight">Indicates not to swap bytes of the converting value</param>
		/// <returns>An encoded value</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> value is less than 0.</exception>
		/// <exception cref="ArgumentNullException"></exception>
		public static string EncodeId(int id, uint encodeMult, ulong encodeMask, bool straight = false)
			=> EncodeId(id, encodeMult, encodeMask, Sixty, straight);

		/// <summary>
		/// Restores an ID value from the string representation.
		/// </summary>
		/// <param name="value">String value to be decoded to the ID.</param>
		/// <param name="encodeMult">Hash value multiplier</param>
		/// <param name="encodeMask">Hash value XOR mask</param>
		/// <returns>A decoded value</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static int DecodeId(string? value, uint encodeMult, ulong encodeMask)
			=> DecodeId(value, encodeMult, encodeMask, Sixty);

		private static unsafe int HashCode(string? value, int length)
		{
			if (value == null)
				return 0;
			if (value.Length < length)
				length = value.Length;
			int code = 1234567891;
			fixed (char* str = value)
			{
				char* p = str;
				while (--length >= 0)
				{
					code += (code << 3) + *p++;
				}
			}
			return code & Int32.MaxValue;
		}
	}
}
