// Lexxys Infrastructural library.
// file: SixBitsCoder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
		private const string CharLine = "0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
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
			>='0' and <='9' => value - ('0' - 0),
			>='A' and <='Z' => value - ('A' - 11),
			>='a' and <='z' => value - ('a' - 38),
			'-' or '.' => 10,
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
		public static string Encode(IReadOnlyCollection<byte> bits)
		{
			if (bits == null)
				throw new ArgumentNullException(nameof(bits));
			int len = EncodedLength(bits.Count, true);
			char[]? arr = len > Tools.MaxStackAllocSizeChar ? ArrayPool<char>.Shared.Rent(len): null;
			var mem = arr != null ? arr.AsSpan(0, len): (stackalloc char[len]);
			var (length, tail) = Encode(bits, mem);
			if (tail > 0)
				mem[length] = BitsToChar(tail);
			var result = mem.ToString();
			if (arr != null)
				ArrayPool<char>.Shared.Return(arr);
			return result;
		}

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
		public static string Encode(byte[] bits) => Encode((ReadOnlySpan<byte>)bits);

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
		public static string Encode(ReadOnlySpan<byte> bits)
		{
			int len = EncodedLength(bits.Length, true);
			char[]? arr = len > Tools.MaxStackAllocSizeChar ? ArrayPool<char>.Shared.Rent(len): null;
			var mem = arr != null ? arr.AsSpan(0, len): (stackalloc char[len]);
			var (length, tail) = Encode(bits, mem);
			if (tail > 0)
				mem[length] = BitsToChar(tail);
			var result = mem.ToString();
			if (arr != null)
				ArrayPool<char>.Shared.Return(arr);
			return result;
		}

		/// <summary>
		/// Encode byte array <paramref name="bits"/> using 64 characters map.
		/// </summary>
		/// <param name="bits">Bytes to encode</param>
		/// <param name="result">Output stream to append encoded characters</param>
		/// <returns>Last not encoded bits</returns>
		private static (int Length, int Tail) Encode(IReadOnlyCollection<byte> bits, Span<char> result)
		{
			if (bits == null)
				throw new ArgumentNullException(nameof(bits));
			if (bits.Count == 0)
				return (0, 0);
			if (EncodedLength(bits.Count, false) > result.Length)
				throw new ArgumentOutOfRangeException(nameof(result), result.Length, null);
			using var bb = bits.GetEnumerator();
			int i = 0;
			for (;;)
			{
				if (!bb.MoveNext())
					return (i, 0);
				int a = bb.Current;
				if (!bb.MoveNext())
				{
					// yy|xxxxxx
					result[i++] = BitsToChar(a);
					return (i, (2 << 8) | (a >> 6));
				}
				int b = bb.Current;
				if (!bb.MoveNext())
				{
					// [yyyy|xxxx] [xx|xxxxxx]
					a = b << 8 | a;
					result[i++] = BitsToChar(a);
					result[i++] = BitsToChar(a >> 6);
					return (i, (4 << 8) | (a >> 12));
				}
				int c = bb.Current;
				// xxxxxx|xx xxxx|xxxx xx|xxxxxx|
				a = c << 16 | b << 8 | a;
				result[i++] = BitsToChar(a);
				result[i++] = BitsToChar(a >> 6);
				result[i++] = BitsToChar(a >> 12);
				result[i++] = BitsToChar(a >> 18);
			}
		}

		/// <summary>
		/// Encode byte array <paramref name="bits"/> using 64 characters map.
		/// </summary>
		/// <param name="bits">Bytes to encode</param>
		/// <param name="result">Output stream to append encoded characters</param>
		/// <returns>Last not encoded bits</returns>
		private static (int Length, int Tail) Encode(ReadOnlySpan<byte> bits, Span<char> result)
		{
			if (bits.Length == 0)
				return (0, 0);
			if (EncodedLength(bits.Length, false) > result.Length)
				throw new ArgumentOutOfRangeException(nameof(result), result.Length, null);
			int i = 0;
			for (;;)
			{
				if (bits.Length == 0)
					return (i, 0);
				int a = bits[0];
				bits = bits[1..];
				if (bits.Length == 0)
				{
					// yy|xxxxxx
					result[i++] = BitsToChar(a);
					return (i, (2 << 8) | (a >> 6));
				}
				int b = bits[0];
				bits = bits[1..];
				if (bits.Length == 0)
				{
					// [yyyy|xxxx] [xx|xxxxxx]
					a = b << 8 | a;
					result[i++] = BitsToChar(a);
					result[i++] = BitsToChar(a >> 6);
					return (i, (4 << 8) | (a >> 12));
				}
				int c = bits[0];
				bits = bits[1..];
				// xxxxxx|xx xxxx|xxxx xx|xxxxxx|
				a = c << 16 | b << 8 | a;
				result[i++] = BitsToChar(a);
				result[i++] = BitsToChar(a >> 6);
				result[i++] = BitsToChar(a >> 12);
				result[i++] = BitsToChar(a >> 18);
			}
		}

		/// <summary>
		/// Encode byte array <paramref name="bits"/> using 32 characters map.
		/// </summary>
		/// <param name="bits">Bytes to encode</param>
		/// <returns>Encoded string</returns>
		public static string Encode32(IReadOnlyCollection<byte> bits)
		{
			if (bits == null)
				throw new ArgumentNullException(nameof(bits));
			if (bits.Count == 0)
				return String.Empty;

			int len = (bits.Count * 8 + 4) / 5;
			char[]? arr = len > Tools.MaxStackAllocSizeChar ? ArrayPool<char>.Shared.Rent(len): null;
			var mem = arr != null ? arr.AsSpan(0, len): (stackalloc char[len]);
			int n = 0;
			int k = 0;
			int b = 0;
			foreach (byte v in bits)
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

			string result = mem[..n].ToString();
			if (arr != null)
				ArrayPool<char>.Shared.Return(arr);
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
#if NET9_0_OR_GREATER
			var guid = Guid.CreateVersion7();
#else
			var guid = Guid.NewGuid();
#endif
			Span<byte> bits = stackalloc byte[16];
#if NETCOREAPP
			MemoryMarshal.TryWrite(bits, in guid);
#else
			MemoryMarshal.TryWrite(bits, ref guid);
#endif
			Span<char> chars = stackalloc char[24];
			var (_, tail) = Encode(bits, chars);

			int q = ((StringHashCode(chars[..21]) & 0xFFFF) << 2) | (tail & 3);
			chars[21] = BitsToChar(q);
			chars[22] = BitsToChar(q >> 6);
			chars[23] = BitsToChar(q >> 12);
			return chars.ToString();
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
			var s = sessionId.AsSpan();
			int q = StringHashCode(s[..21]) & 0xFFFF;
			int k = (CharToBits(s[21]) | (CharToBits(s[22]) << 6) | (CharToBits(s[23]) << 12));
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
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Index(char c) => c switch
		{
			>='0' and <= '9' => c - '0',
			>='A' and <= 'Z' => c - ('A' - ('9' - '0' + 1)),
			>='a' and <= 'z' => c - ('a' - ('9' - '0' + 1)),
			_ => -1
		};

		public static string Thirty(ReadOnlySpan<ulong> value)
		{
			if (value.Length == 0)
				return "0";
			int len = value.Length * 15;
			
			char[]? array = null;
			Span<char> buffer = len <= Tools.MaxStackAllocSizeChar ? stackalloc char[len]: (array = ArrayPool<char>.Shared.Rent(len));
			int i = buffer.Length;
			for(int j = value.Length - 1; j >= 0; --j)
			{
				ulong val = value[j];
				if (val == 0)
				{
					buffer[--i] = '0';
					continue;
				}
				int k = i;
				while (val > 0)
				{
					buffer[--i] = CharLin3L[(int)(val % 36)];
					val /= 36;
				}
				char c = CharLin3L[k - i];
				buffer[--i] = c;
			}
			string result = buffer.Slice(i).ToString();
			if (array != null)
				ArrayPool<char>.Shared.Return(array);
			return result;
		}

		public static (ulong Value, int Index) ThirtyNext(ReadOnlySpan<char> value)
		{
			if (value.Length == 0)
				return (0, 0);
			int len = value.Length * 15;

			int i = 0;
			int n;
			do
			{
				n = Index(value[i++]);
			}
			while (n < 0 && i < value.Length);
			if (n < 0)
				return (0, 0);

			ulong result = 0;
			while (i < value.Length && n > 0)
			{
				int k = Index(value[i++]);
				if (k < 0)
					continue;
				result = result * 36 + (ulong)k;
				--n;
			}
			return (result, i);
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
			ulong code = (ulong)id * encodeMult ^ encodeMask;
			if (!straight)
				code = Swap64(code);
			return convert(code);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong Swap64(ulong value)
		{
			ulong x = value << 32 | value >> 32;
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

		private static int StringHashCode(ReadOnlySpan<char> value)
		{
			if (value.Length == 0)
				return 0;
			int hash = 0x15051505;
			foreach (var c in value)
			{
				hash = ((hash << 5) + hash) ^ c;
			}
			return hash & int.MaxValue;
		}
	}
}
