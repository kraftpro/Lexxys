using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lexxys.Test.Con
{
	/// <summary>
	/// an fast Base64 function by QQ:20437023 liaisonme@hotmail.com
	/// MIT License Copyright (c) 2018 zhangxx2015
	/// </summary>
	static class BinaryTools
	{
		private readonly static int[] H6 = /* i >> 2        */ { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9, 10, 10, 10, 10, 11, 11, 11, 11, 12, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 14, 15, 15, 15, 15, 16, 16, 16, 16, 17, 17, 17, 17, 18, 18, 18, 18, 19, 19, 19, 19, 20, 20, 20, 20, 21, 21, 21, 21, 22, 22, 22, 22, 23, 23, 23, 23, 24, 24, 24, 24, 25, 25, 25, 25, 26, 26, 26, 26, 27, 27, 27, 27, 28, 28, 28, 28, 29, 29, 29, 29, 30, 30, 30, 30, 31, 31, 31, 31, 32, 32, 32, 32, 33, 33, 33, 33, 34, 34, 34, 34, 35, 35, 35, 35, 36, 36, 36, 36, 37, 37, 37, 37, 38, 38, 38, 38, 39, 39, 39, 39, 40, 40, 40, 40, 41, 41, 41, 41, 42, 42, 42, 42, 43, 43, 43, 43, 44, 44, 44, 44, 45, 45, 45, 45, 46, 46, 46, 46, 47, 47, 47, 47, 48, 48, 48, 48, 49, 49, 49, 49, 50, 50, 50, 50, 51, 51, 51, 51, 52, 52, 52, 52, 53, 53, 53, 53, 54, 54, 54, 54, 55, 55, 55, 55, 56, 56, 56, 56, 57, 57, 57, 57, 58, 58, 58, 58, 59, 59, 59, 59, 60, 60, 60, 60, 61, 61, 61, 61, 62, 62, 62, 62, 63, 63, 63, 63 };
		private readonly static int[] L2 = /* (i >> 4) & 63 */ { 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48, 0, 16, 32, 48 };
		private readonly static int[] H4 = /* i >> 4        */ { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 };
		private readonly static int[] L4 = /* (i >> 2) & 63 */ { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60 };
		private readonly static int[] H2 = /* i >> 6        */ { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 };
		private readonly static int[] L6 = /* i & 63        */ { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63 };
		private readonly static char[] CharsMap = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/', '=' };
		private static readonly byte[] BytesMap = new byte[] { 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 43, 47, 61 };

		public static char[] ToBase64CharArray(ReadOnlySpan<byte> bits)
		{
			var bytesCount = bits.Length;
			var bodysCount = bytesCount;
			var bytesIndex = 0;
			var b64Chars = new char[4 * (bytesCount / 3 + (bytesCount % 3 > 0 ? 1 : 0))];
			var outIndex = 0;
			while (bodysCount >= 3)
			{
				b64Chars[outIndex + 0] = CharsMap[H6[bits[bytesIndex + 0]]];
				b64Chars[outIndex + 1] = CharsMap[L2[bits[bytesIndex + 0]] | H4[bits[bytesIndex + 1]]];
				b64Chars[outIndex + 2] = CharsMap[L4[bits[bytesIndex + 1]] | H2[bits[bytesIndex + 2]]];
				b64Chars[outIndex + 3] = CharsMap[L6[bits[bytesIndex + 2]]];
				outIndex += 4;
				bytesIndex += 3;
				bodysCount -= 3;
			}

			if (bytesIndex >= bytesCount)
				return b64Chars;

			b64Chars[outIndex++] = CharsMap[H6[bits[bytesIndex]]];
			var idx = L2[bits[bytesIndex++]];
			if (bytesIndex < bytesCount)
				idx |= H4[bits[bytesIndex]];
			b64Chars[outIndex++] = CharsMap[idx];

			if (bytesIndex < bytesCount)
			{
				idx = L4[bits[bytesIndex++]];
				if (bytesIndex < bytesCount)
					idx |= H2[bits[bytesIndex]];
				b64Chars[outIndex++] = CharsMap[idx];

				if (bytesIndex < bytesCount)
					b64Chars[outIndex] = CharsMap[L6[bits[bytesIndex]]];
				else
					b64Chars[outIndex] = '=';
			}
			else
			{
				b64Chars[outIndex + 0] = '=';
				b64Chars[outIndex + 1] = '=';
			}

			return b64Chars;
		}

		public static byte[] ToBase64ByteArray(ReadOnlySpan<byte> bits)
		{

			var bytesCount = bits.Length;
			var bodysCount = bytesCount;
			var bytesIndex = 0;
			var b64Bytes = new byte[4 * (bytesCount / 3 + (bytesCount % 3 > 0 ? 1 : 0))];
			var outIndex = 0;
			while (bodysCount >= 3)
			{
				b64Bytes[outIndex + 0] = BytesMap[H6[bits[bytesIndex + 0]]];
				b64Bytes[outIndex + 1] = BytesMap[L2[bits[bytesIndex + 0]] | H4[bits[bytesIndex + 1]]];
				b64Bytes[outIndex + 2] = BytesMap[L4[bits[bytesIndex + 1]] | H2[bits[bytesIndex + 2]]];
				b64Bytes[outIndex + 3] = BytesMap[L6[bits[bytesIndex + 2]]];
				outIndex += 4;
				bytesIndex += 3;
				bodysCount -= 3;
			}

			if (bytesIndex >= bytesCount)
				return b64Bytes;

			b64Bytes[outIndex++] = BytesMap[H6[bits[bytesIndex]]];
			var idx = L2[bits[bytesIndex++]];
			if (bytesIndex < bytesCount)
				idx |= H4[bits[bytesIndex]];
			b64Bytes[outIndex++] = BytesMap[idx];

			if (bytesIndex < bytesCount)
			{
				idx = L4[bits[bytesIndex++]];
				if (bytesIndex < bytesCount)
					idx |= H2[bits[bytesIndex]];
				b64Bytes[outIndex++] = BytesMap[idx];

				if (bytesIndex < bytesCount)
					b64Bytes[outIndex] = BytesMap[L6[bits[bytesIndex]]];
				else
					b64Bytes[outIndex] = (byte)'=';
			}
			else
			{
				b64Bytes[outIndex + 0] = (byte)'=';
				b64Bytes[outIndex + 1] = (byte)'=';
			}

			return b64Bytes;
		}

		public static unsafe void ToBase64ByteStream(ReadOnlySpan<byte> bits, Stream stream)
		{
			const int BufferSize = 128 * 4;
			var bytesCount = bits.Length;
			var bodysCount = bytesCount;
			var bytesIndex = 0;
#if NETSTANDARD
			var buffer = stackalloc byte[BufferSize];
#else
			var buffer = new byte[BufferSize];
#endif
			var i = 0;
			while (bodysCount >= 3)
			{
				buffer[i + 0] = BytesMap[H6[bits[bytesIndex + 0]]];
				buffer[i + 1] = BytesMap[L2[bits[bytesIndex + 0]] | H4[bits[bytesIndex + 1]]];
				buffer[i + 2] = BytesMap[L4[bits[bytesIndex + 1]] | H2[bits[bytesIndex + 2]]];
				buffer[i + 3] = BytesMap[L6[bits[bytesIndex + 2]]];
				bytesIndex += 3;
				bodysCount -= 3;
				i += 4;
				if (i >= BufferSize)
				{
					Debug.Assert(i == BufferSize);
#if NETSTANDARD
					stream.Write(new ReadOnlySpan<byte>(buffer, BufferSize));
#else
					stream.Write(buffer, 0, BufferSize);
#endif
					i = 0;
				}
			}

			if (bytesIndex >= bytesCount)
				return;

			buffer[i++] = BytesMap[H6[bits[bytesIndex]]];
			var idx = L2[bits[bytesIndex++]];
			if (bytesIndex < bytesCount)
				idx |= H4[bits[bytesIndex]];
			buffer[i++] = BytesMap[idx];

			if (bytesIndex < bytesCount)
			{
				idx = L4[bits[bytesIndex++]];
				if (bytesIndex < bytesCount)
					idx |= H2[bits[bytesIndex]];
				buffer[i++] = BytesMap[idx];

				if (bytesIndex < bytesCount)
					buffer[i++] = BytesMap[L6[bits[bytesIndex]]];
				else
					buffer[i++] = (byte)'=';
			}
			else
			{
				buffer[i++] = (byte)'=';
				buffer[i++] = (byte)'=';
			}
#if NETSTANDARD
			stream.Write(new ReadOnlySpan<byte>(buffer, i));
#else
			stream.Write(buffer, 0, i);
#endif
		}
	}
}
