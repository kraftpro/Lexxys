using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Lexxys;
using Lexxys.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con.Js
{
	public class Utf8Test
	{
		public static BenchmarkDotNet.Reports.Summary Go()
		{
			return BenchmarkRunner.Run<Utf8Test>();
		}

		static string _randStr = "";


		public static void SetStringValue(int len, bool ansi)
		{
			int n1 = Rand.Int(len);
			int n2 = len - n1;
			_randStr = ansi ? R.Ascii0(len) : R.Concat(R.Ascii0(n1), R.Str(R.Chr('А', 'я'), n2));
		}

		public static void SetStringValue(string value)
		{
			_randStr = value;
		}

		public static int GoRun(int count)
		{
			int sum = 0;
			for (int i = 0; i < count; i++)
			{
				var bytes = new byte[Utf8.BytesCount(_randStr.AsSpan())];
				Utf8.Append(bytes, _randStr.AsSpan());
				sum += bytes.Length;
			}
			return sum;
		}

		//[Params(1000, 10000)]
		[Params(1000)]
		public int N;

		[Params(true, false)]
		//[Params(false)]
		public bool Ansi;


		[GlobalSetup]
		public void Setup()
		{
			SetStringValue(N, Ansi);
			_string = _randStr;
		}

		private string _string;


		[Benchmark]
		public byte[] UseEncoding()
		{
			return Encoding.UTF8.GetBytes(_string);
		}

		[Benchmark]
		public byte[] UseUtf8()
		{
			var bytes = new byte[Utf8.BytesCount(_string.AsSpan())];
			Utf8.Append(bytes, _string.AsSpan());
			return bytes;
		}

	}

	public static class Utf8
	{
		[SecurityCritical]
		public static unsafe (int ByteCount, int CharCount) Append(Span<byte> buffer, ReadOnlySpan<char> value)
		{
			fixed (char* vp0 = value)
			fixed (byte* bp0 = buffer)
			{
				char* vp = vp0;
				byte* bp = bp0;
				int blen = buffer.Length;
				int vlen = value.Length;
				if (vlen > blen)
					vlen = blen;
				char* ve = vp0 + vlen;
				byte* be = bp0 + blen;

				//int safe = blen / 3;
				//char* vsafe = vp0 + (vlen < safe ? vlen : safe);
				//while (vp < vsafe)
				//{
				//	ushort v = *vp++;
				//	if (v <= 0x7F)
				//	{
				//		*bp++ = (byte)v;
				//	}
				//	else if (v <= 0x7FF)
				//	{
				//		*bp++ = (byte)(0b1100_0000 | (v >> 6));
				//		*bp++ = (byte)(0b1000_0000 | (v & 0b0011_1111));
				//	}
				//	else
				//	{
				//		*bp++ = (byte)(0b1110_0000 | (v >> 12));
				//		*bp++ = (byte)(0b1000_0000 | ((v >> 6) & 0b0011_1111));
				//		*bp++ = (byte)(0b1000_0000 | (v & 0b0011_1111));
				//	}
				//}

				char* ve2 = vlen < 2 ? vp : vp + (vlen - 1);
				byte* be2 = blen < 2 ? bp : bp + (blen - 3);
				while (vp < ve2 && bp < be2)
				{
					ulong v2 = *(uint*)vp;
					if ((v2 & 0xFF80FF80) == 0 && bp + 2 < be)
					{
						*(uint*)bp = ((uint)v2 & 0xFF) | (((uint)v2 >> 16) & 0xFF);
						vp += 2;
						bp += 2;
						continue;
					}

					ushort v = *vp++;
					if (v <= 0x7F)
					{
						*bp++ = (byte)v;
					}
					else if (v <= 0x7FF)
					{
						*bp++ = (byte)(0b1100_0000 | (v >> 6));
						*bp++ = (byte)(0b1000_0000 | (v & 0b0011_1111));
					}
					else
					{
						if (bp + 1 >= be2)
							break;
						*bp++ = (byte)(0b1110_0000 | (v >> 12));
						*bp++ = (byte)(0b1000_0000 | ((v >> 6) & 0b0011_1111));
						*bp++ = (byte)(0b1000_0000 | (v & 0b0011_1111));
					}

					if (vp >= ve2 || bp >= be2)
						break;

					v = *vp++;
					if (v <= 0x7F)
					{
						*bp++ = (byte)v;
					}
					else if (v <= 0x7FF)
					{
						*bp++ = (byte)(0b1100_0000 | (v >> 6));
						*bp++ = (byte)(0b1000_0000 | (v & 0b0011_1111));
					}
					else
					{
						if (bp + 1 >= be2)
							break;
						*bp++ = (byte)(0b1110_0000 | (v >> 12));
						*bp++ = (byte)(0b1000_0000 | ((v >> 6) & 0b0011_1111));
						*bp++ = (byte)(0b1000_0000 | (v & 0b0011_1111));
					}
				}

				while (vp < ve)
				{
					ushort v = *vp++;
					if (v <= 0x7F)
					{
						if (bp == be)
							break;
						*bp++ = (byte)v;
					}
					else if (v <= 0x7FF)
					{
						if (bp + 1 >= be)
							break;
						*bp++ = (byte)(0b1100_0000 | (v >> 6));
						*bp++ = (byte)(0b1000_0000 | (v & 0b0011_1111));
					}
					else
					{
						if (bp + 2 >= be)
							break;
						*bp++ = (byte)(0b1110_0000 | (v >> 12));
						*bp++ = (byte)(0b1000_0000 | ((v >> 6) & 0b0011_1111));
						*bp++ = (byte)(0b1000_0000 | (v & 0b0011_1111));
					}
				}
				return ((int)(bp - bp0), (int)(vp - vp0));
			}

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Append(Span<byte> buffer, char value) => Append(buffer, (ushort)value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Append(Span<byte> buffer, int value) => Append(buffer, (uint)value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BytesCount(char value) => value <= 0x7F ? 1 : value <= 0x7FF ? 2 : 3;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BytesCount(ushort value) => value <= 0x7F ? 1 : value <= 0x7FF ? 2 : 3;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BytesCount(uint value) => value <= 0x7F ? 1 : value <= 0x7FF ? 2 : value <= 0xFFFF ? 3 : value <= 0x1FFFFF ? 4 : 5;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSurogate(ushort value) => value > 0xD7FF && value <= 0xDFFF;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSurogate(uint value) => value > 0xD7FF && (value <= 0xDFFF || value > 0x10FFFF);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SkipBrokenBytes(ReadOnlySpan<byte> bits)
		{
			int i = 0;
			while (i < bits.Length && SeqLength(bits[i]) == 0)
				++i;
			return i;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BackToBeginOfSquence(ReadOnlySpan<byte> bits, int index)
		{
			if (bits.Length == 0)
				return 0;
			if (index >= bits.Length)
				index = bits.Length - 1;
			int i = index;
			while (i >= 0 && SeqLength(bits[i]) == 0)
				--i;
			return i < 0 ? -1: index - i;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Append(Span<byte> buffer, ushort value)
		{
			if (value <= 0x7F)
			{
				if (buffer.Length < 1)
					return 0;
				buffer[0] = (byte)value;
				return 1;
			}
			if (value <= 0x7FF)
			{
				if (buffer.Length < 2)
					return 0;
				buffer[0] = (byte)(0b1100_0000 | (value >> 6));
				buffer[1] = (byte)(0b1000_0000 | (value & 0b0011_1111));
				return 2;
			}
			if (buffer.Length < 3)
				return 0;
			buffer[0] = (byte)(0b1110_0000 | (value >> 12));
			buffer[1] = (byte)(0b1000_0000 | ((value >> 6) & 0b0011_1111));
			buffer[2] = (byte)(0b1000_0000 | (value & 0b0011_1111));
			return 3;
		}

		public static int Append(Span<byte> buffer, uint value)
		{
			if (value <= 0x7F)
			{
				buffer[0] = (byte)value;
				return 1;
			}
			if (value <= 0x7FF)
			{
				buffer[0] = (byte)(0b1100_0000 | (value >> 6));
				buffer[1] = (byte)(0b1000_0000 | (value & 0b0011_1111));
				return 2;
			}
			if (value <= 0xFFFF)
			{
				buffer[0] = (byte)(0b1110_0000 | (value >> 12));
				buffer[1] = (byte)(0b1000_0000 | ((value >> 6) & 0b0011_1111));
				buffer[2] = (byte)(0b1000_0000 | (value & 0b0011_1111));
				return 3;
			}
			if (value <= 0x1FFFFF)
			{
				buffer[0] = (byte)(0b1111_0000 | (value >> 18));
				buffer[1] = (byte)(0b1000_0000 | ((value >> 12) & 0b0011_1111));
				buffer[2] = (byte)(0b1000_0000 | ((value >> 6) & 0b0011_1111));
				buffer[3] = (byte)(0b1000_0000 | (value & 0b0011_1111));
				return 4;
			}
			value &= 0x3FFFFFF;
			buffer[0] = (byte)(0b1111_1000 | (value >> 24));
			buffer[1] = (byte)(0b1000_0000 | ((value >> 18) & 0b0011_1111));
			buffer[2] = (byte)(0b1000_0000 | ((value >> 12) & 0b0011_1111));
			buffer[3] = (byte)(0b1000_0000 | ((value >> 6) & 0b0011_1111));
			buffer[4] = (byte)(0b1000_0000 | (value & 0b0011_1111));
			return 5;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Read(ReadOnlySpan<byte> bits, out char value)
		{
			var i = Read(bits, out ushort u);
			value = (char)u;
			return i;
		}

		public static int Read(ReadOnlySpan<byte> bits, out ushort value)
		{
			if (bits.Length == 0)
			{
				value = 0;
				return 0;
			}
			byte b = bits[0];
			if (b <= 0b0111_1111)
			{
				value = b;
				return 1;
			}
			if (bits.Length < 2)
			{
				value = 0;
				return 0;
			}
			byte b2 = bits[1];
			if ((b & 0b1110_0000) == 0b1100_0000)
			{
				value = 0;
				return 0;
			}
			if ((b & 0b1111_0000) != 0b1110_0000)
			{
				value = 0;
				return 0;
			}
			if (bits.Length < 3)
			{
				value = 0;
				return 0;
			}
			byte b3 = bits[2];
			if ((b2 & 0b1100_0000) != 0b1000_0000 || (b3 & 0b1100_0000) != 0b1000_0000)
			{
				value = 0;
				return 0;
			}
			value = (ushort)(((b & 0b0000_1111) << 12) | ((b2 & 0b0011_1111) << 6) | (b3 & 0b0011_1111));
			return 3;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BytesCount(ReadOnlySpan<char> value)
		{
			int n = 0;
			for (int i = 0; i < value.Length; i++)
			{
				n += BytesCount(value[i]);
			}
			return n;
		}

		public static int SeqLength(byte first) => _cnU8[first >> 4];
		private static readonly int[] _cnU8 = new[] {
			1, 1, 1, 1, 1, 1, 1, 1, // 0???
			0, 0, 0, 0, // 8, 9, A, B  10??
			2, 2,		// C, D        110?
			3,			// E           1110
			0,			// F
		};

		public static (int ByteCount, int CharCount) AppendCsString(Span<byte> bits, ReadOnlySpan<char> value, char marker = '"')
		{
			int k;
			int m = 0;
			if (marker != '\0')
			{
				m = 1;
				k = Append(bits, marker);
				if (k == 0)
					return default;
				bits = bits.Slice(k);
			}
			int n = 0;
			int i;
			for (i = 0; i < value.Length; ++i)
			{
				if (bits.Length < 1)
					break;
				ushort c = value[i];
				if (c <= 127)
				{
					if (c >= ' ' && c < 127 && c != '\\' && c != marker)
					{
						if (bits.Length == 0)
							break;
						bits[0] = (byte)c;
						bits = bits.Slice(1);
						++n;
						continue;
					}
					if (bits.Length < 2)
						return (n, i + m);
					bits[0] = (byte)'\\';
					switch (c)
					{
						case '\n':
							bits[1] = (byte)'n';
							break;
						case '\r':
							bits[1] = (byte)'r';
							break;
						case '\t':
							bits[1] = (byte)'t';
							break;
						case '\f':
							bits[1] = (byte)'f';
							break;
						case '\v':
							bits[1] = (byte)'v';
							break;
						case '\a':
							bits[1] = (byte)'b';
							break;
						case '\b':
							bits[1] = (byte)'b';
							break;
						case '\\':
							bits[1] = (byte)'\\';
							break;
						case '\0':
							bits[1] = (byte)'0';
							break;
						default:
							if (c == marker)
							{
								bits[1] = (byte)marker;
							}
							else if (value.Length > i + 1 && IsHex(value[i + 1]))
							{
								if (bits.Length < 6)
									return (n, i + m);
								bits[1] = (byte)'u';
								bits[2] = (byte)'0';
								bits[3] = (byte)'0';
								bits[4] = __hex[(c & 0xF0) >> 4];
								bits[5] = __hex[c & 0xF];
								bits = bits.Slice(4);
								n += 4;
							}
							else
							{
								if (bits.Length < 4)
									return (n, i + m);
								bits[1] = (byte)'x';
								bits[2] = __hex[(c & 0xF0) >> 4];
								bits[3] = __hex[c & 0xF];
								bits = bits.Slice(2);
								n += 2;
							}
							break;
					}
					bits = bits.Slice(2);
					n += 2;
					continue;
				}
				if (c >= '\xd800')
				{
					if (bits.Length < 6)
						break;
					bits[0] = (byte)'\\';
					bits[1] = (byte)'u';
					bits[2] = __hex[(c & 0xF000) >> 12];
					bits[3] = __hex[(c & 0xF00) >> 8];
					bits[4] = __hex[(c & 0xF0) >> 4];
					bits[5] = __hex[c & 0xF];
					bits = bits.Slice(6);
					n += 6;
				}
				else if (c == marker)
				{
					bits[0] = (byte)'\\';
					k = Append(bits.Slice(1), marker);
					if (k == 0)
						break;
					bits = bits.Slice(k + 1);
					n += k + 1;
				}
				else
				{
					k = Append(bits, c);
					if (k == 0)
						break;
					bits = bits.Slice(k);
					n += k;
				}
			}

			if (marker == '\0' || i < value.Length)
				return (n, i + m);

			k = Append(bits, marker);
			return (n + k, i + (k == 0 ? 1: 2));

			static bool IsHex(char c) => c >= '0' && (c <= '9' || (c >= 'A' && (c <= 'F' || c >= 'a' && c <= 'f')));
		}
		private static readonly byte[] __hex = { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f' };

		public static int ReadChar3(ReadOnlySpan<byte> bits, out ushort value)
		{
			if (bits.Length == 0)
			{
				value = 0;
				return 0;
			}
			byte b = bits[0];
			int n = SeqLength(b);
			if (n == 0 || bits.Length < n)
			{
				value = 0;
				return 0;
			}
			switch (n)
			{
				case 1:
					value = b;
					return 1;
				case 2:
					if ((bits[1] & 0b1100_0000) != 0b1000_0000)
					{
						value = 0;
						return 0;
					}
					value = (ushort)(((b & 0b0001_1111) << 6) | (bits[1] & 0b0011_1111));
					return 2;
				default:
				case 3:
					if ((bits[1] & 0b1100_0000) != 0b1000_0000 || (bits[2] & 0b1100_0000) != 0b1000_0000)
					{
						value = 0;
						return 0;
					}
					value = (ushort)(((b & 0b0000_1111) << 12) | ((bits[1] & 0b0011_1111) << 6) | (bits[2] & 0b0011_1111));
					return 3;
			}
		}

		private static bool InRange(int ch, int start, int end)
		{
			return (uint)(ch - start) <= (uint)(end - start);
		}

		private unsafe static int PtrDiff(char* a, char* b)
		{
			return (int)((uint)((byte*)a - (byte*)b) >> 1);
		}

		private unsafe static int PtrDiff(byte* a, byte* b)
		{
			return (int)(a - b);
		}

		internal unsafe static int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
		{
			char* pchar = chars;
			byte* pbyte = bytes;
			char* echar = pchar + charCount;
			byte* ebyte = pbyte + byteCount;
			int ch = 0;
			while (true)
			{
				if (pchar >= echar)
				{
					if (ch <= 0)
						break;
					goto Loop;
				}

				if (ch > 0)
				{
					int ch2 = *pchar;
					if (InRange(ch2, 0xDC00, 0xDFFF))
					{
						ch = ch2 + (ch << 10) + -56613888;
						pchar++;
					}
					goto Loop;
				}

				ch = *pchar;
				pchar++;

				if (InRange(ch, 0xD800, 0xDBFF))
				{
					continue;
				}

			Loop:
				if (InRange(ch, 0xD800, 0xDFFF))
				{
					ch = 0;
					continue;
				}
				int width = 1;
				if (ch > 127)
				{
					if (ch > 0x07FF)
					{
						if (ch > 0xFFFF)
						{
							width++;
						}
						width++;
					}
					width++;
				}
				if (pbyte > ebyte - width)
				{
					pchar--;
					if (ch > 0xFFFF)
					{
						pchar--;
					}
					ch = 0;
					break;
				}
				if (ch <= 127)
				{
					*pbyte = (byte)ch;
				}
				else
				{
					int ch2;
					if (ch <= 0x07FF)
					{
						ch2 = (byte)(-64 | (ch >> 6));
					}
					else
					{
						if (ch <= 0xFFFF)
						{
							ch2 = (byte)(-32 | (ch >> 12));
						}
						else
						{
							*pbyte = (byte)(-16 | (ch >> 18));
							pbyte++;
							ch2 = (-128 | ((ch >> 12) & 0x3F));
						}
						*pbyte = (byte)ch2;
						pbyte++;
						ch2 = (-128 | ((ch >> 6) & 0x3F));
					}
					*pbyte = (byte)ch2;
					pbyte++;
					*pbyte = (byte)(-128 | (ch & 0x3F));
				}
				pbyte++;

				int cleft = PtrDiff(echar, pchar);
				int bleft = PtrDiff(ebyte, pbyte);
				if (cleft <= 13)
				{
					if (bleft < cleft)
					{
						ch = 0;
						continue;
					}
					while (pchar < echar)
					{
						ch = *pchar;
						pchar++;
						if (ch <= 127)
							*pbyte++ = (byte)ch;
						else if (!InRange(ch, 0xD800, 0xDBFF))
							goto Loop;
					}
					ch = 0;
					break;
				}
				if (bleft < cleft)
					cleft = bleft;

				char* ptr5 = pchar + cleft - 5;
				while (pchar < ptr5)
				{
					ch = *pchar;
					pchar++;
					if (ch <= 127)
					{
						*pbyte = (byte)ch;
						pbyte++;
						if (((int)pchar & 2) != 0)
						{
							ch = *pchar;
							pchar++;
							if (ch > 127)
							{
								goto IL_03dd;
							}
							*pbyte = (byte)ch;
							pbyte++;
						}
						while (pchar < ptr5)
						{
							ch = *(int*)pchar;
							int num8 = *(int*)(pchar + 2);
							if (((ch | num8) & 0xFF80FF80) == 0)
							{
								*pbyte = (byte)ch;
								pbyte[1] = (byte)(ch >> 16);
								pchar += 4;
								pbyte[2] = (byte)num8;
								pbyte[3] = (byte)(num8 >> 16);
								pbyte += 4;
								continue;
							}
							ch = (ushort)ch;
							pchar++;
							if (ch > 127)
								goto IL_03dd;

							*pbyte = (byte)ch;
							pbyte++;
						}
						continue;
					}

				IL_03dd:
					int num9;
					if (ch <= 0x07FF)
					{
						num9 = (-64 | (ch >> 6));
					}
					else
					{
						if (!InRange(ch, 0xD800, 0xDFFF))
						{
							num9 = (-32 | (ch >> 12));
						}
						else
						{
							if (ch > 0xDBFF)
							{
								pchar--;
								break;
							}
							num9 = *pchar;
							pchar++;
							if (!InRange(num9, 0xDC00, 0xDFFF))
							{
								pchar -= 2;
								break;
							}
							ch = num9 + (ch << 10) + -56613888;
							*pbyte = (byte)(-16 | (ch >> 18));
							pbyte++;
							num9 = (-128 | ((ch >> 12) & 0x3F));
						}
						*pbyte = (byte)num9;
						ptr5--;
						pbyte++;
						num9 = (-128 | ((ch >> 6) & 0x3F));
					}
					*pbyte = (byte)num9;
					ptr5--;
					pbyte++;
					*pbyte = (byte)(-128 | (ch & 0x3F));
					pbyte++;
				}
				ch = 0;
			}
			return (int)(pbyte - bytes);
		}

	}
}
