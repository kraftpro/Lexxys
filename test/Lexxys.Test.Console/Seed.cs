// Lexxys Infrastructural library.
// file: Seed.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

namespace Lexxys.Test.Con
{
	public class Seed
	{
		private const int BitsPerByte = 8;
		private const int BitsPerInt = 32;


		private const ulong EncodeMask = 982451653;
		private const ulong EncodeMult = 15485863;

		public static string Encode(int value)
		{
			if (value < 0)
				throw EX.ArgumentOutOfRange("value", value);
			return "a" + SixBitsCoder.Sixty((ulong)value * EncodeMult ^ EncodeMask);
		}

		public static int Decode(string value)
		{
			ulong code;
			if (value == null || !value.StartsWith("a") || (code = SixBitsCoder.Sixty(value.Substring(1))) == 0)
				return 0;
			code = code ^ EncodeMask;
			if (code % EncodeMult != 0)
				return 0;
			code /= EncodeMult;
			return code > (ulong)int.MaxValue ? 0 : (int)code;
		}

		public static string Mx(int value)
		{
			ulong a = (uint)value * EncodeMult;
			ulong a1 = a & uint.MaxValue;
			ulong a2 = a >> 32;
			ulong a11 = a1 & ushort.MaxValue;
			ulong a12 = a1 >> 16;
			ulong a21 = a2 & ushort.MaxValue;
			ulong a22 = a2 >> 16;
			ulong b = a12 << 48 | a21 << 32 | a11 << 16 | a22;
			return SixBitsCoder.Sixty(b);
		}

		public static int Mx(string value)
		{
			ulong a = SixBitsCoder.Sixty(value);
			ulong a1 = a & uint.MaxValue;
			ulong a2 = a >> 32;
			ulong a11 = a1 & ushort.MaxValue;
			ulong a12 = a1 >> 16;
			ulong a21 = a2 & ushort.MaxValue;
			ulong a22 = a2 >> 16;
			ulong b = a11 << 48 | a21 << 32 | a22 << 16 | a12;
			return (int)(b / EncodeMult);
		}

		public static ulong L(byte[] bits)
		{
			return (ulong)new RowVersion(bits).Value;
		}

		public static byte[] B(ulong value)
		{
			return new RowVersion((long)value).GetBits();
		}

		public static string X(int value)
		{
			return SixBitsCoder.Sixty(L(Mix(value, sizeof(ulong) - 2)));
		}

		public static int X(string value)
		{
			return Restore(B(SixBitsCoder.Sixty(value)));
		}

		public static byte[] Mix(int value, int width)
		{
			if (width < 2 + sizeof(int) || width > 2 + 1024)
				throw EX.ArgumentOutOfRange("width", width);

			byte[] bits = new byte[width + 2];
			int seed = (int)WatchTimer.Query(0) & 0xFFFF;
			bits[0] = (byte)seed;
			bits[1] = (byte)(seed >> BitsPerByte);
			Mix(value, seed, bits, 2, width);
			return bits;
		}

		public static void Mix(int value, int seed, byte[] bits, int offset, int length)
		{
			if (bits == null)
				throw EX.ArgumentNull("bits");
			if (length < sizeof(int))
				throw EX.ArgumentOutOfRange("length", length);
			if (offset < 0)
				throw EX.ArgumentOutOfRange("offset", offset);
			if (offset + length > bits.Length)
				throw EX.ArgumentOutOfRange("offset + length", offset + length, bits.Length);


			Rand r = new Rand(seed);
			r.Fill(bits, offset, length);
			IList<int> b = MixPositions(BitsPerInt, seed, length * BitsPerByte);
			for (int i = 0; i < BitsPerInt; ++i)
			{
				int ix = b[i] / BitsPerByte;
				int mask = 1 << (b[i] % BitsPerByte);
				if ((value & 1) != 0)
					bits[offset + ix] ^= (byte)mask;
				value >>= 1;
			}
		}

		public static int Restore(byte[] bits)
		{
			if (bits == null)
				throw EX.ArgumentNull("bits");
			if (bits.Length < 2 + sizeof(int))
				throw EX.ArgumentOutOfRange("bits.Length", bits.Length);

			int seed = bits[0] | (bits[1] << BitsPerByte);
			return Restore(seed, bits, 2, bits.Length - 2);
		}

		public static int Restore(int seed, byte[] bits, int offset, int length)
		{
			if (bits == null)
				throw EX.ArgumentNull("bits");
			if (length < sizeof(int))
				throw EX.ArgumentOutOfRange("bits.Length", bits.Length);
			if (offset < 0)
				throw EX.ArgumentOutOfRange("offset", offset);
			if (offset + length > bits.Length)
				throw EX.ArgumentOutOfRange("offset + length", offset + length, bits.Length);

			Rand r = new Rand(seed);
			byte[] noise = new byte[length];
			r.Fill(noise, 0, length);
			IList<int> b = MixPositions(BitsPerInt, seed, length * BitsPerByte);
			int value = 0;
			uint m = 1;
			for (int i = 0; i < BitsPerInt; ++i)
			{
				int ix = b[i] / BitsPerByte;
				int mask = 1 << (b[i] % BitsPerByte);
				if ((bits[offset + ix] & (byte)mask) != (noise[ix] & (byte)mask))
					value |= (int)m;
				m <<= 1;
			}
			return value;
		}

		public static string Pack(int value)
		{
			char[] buffer = new char[6];
			int i = buffer.Length;
			while (value > 0)
			{
				int k = value % Line.Length;
				value = value / Line.Length;
				buffer[--i] = Line[k];
			}
			return new string(buffer, i, buffer.Length - i);
		}

		public static int Unpack(string value)
		{
			if (value == null)
				throw EX.ArgumentNull("value");
			int result = 0;
			foreach (var c in value)
			{
				int k;
				if (c >= '0' && c <= '9')
					k = c - '0';
				else if (c >= 'A' && c <= 'Z')
					k = 10 + (c - 'A');
				else if (c >= 'a' && c <= 'z')
					k = 10 + 26 + (c - 'a');
				else
					continue;
				result = result * Line.Length + k;
			}
			return result;
		}

		static readonly char[] Line = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
			'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
			'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'};

		static OrderedSet<int> MixPositions(int count, int seed, int width)
		{
			OrderedSet<int> mask = new OrderedSet<int>();
			Rand r = new Rand(seed);
			do
			{
				mask.Add(r.Next() % width);
			} while (mask.Count < count);
			return mask;
		}


		class Rand
		{
			private const int Bound = Int32.MaxValue;			// 2^31 - 1

			private int _x0;

			public Rand()
				: this((int)Lexxys.WatchTimer.Query(0))
			{
			}

			public Rand(int seed)
			{
				_x0 = Math.Max(Math.Abs(seed), 1); // % Bound;
			}

			public int Next()
			{
				_x0 = Math.Abs(16807 * _x0 + 1); // % Bound;
				return _x0;
			}

			public void Fill(byte[] buffer, int offset, int length)
			{
				if (buffer == null)
					throw EX.ArgumentNull("buffer");
				if (offset < 0)
					throw EX.ArgumentOutOfRange("offset", offset);
				if (length < 0)
					throw EX.ArgumentOutOfRange("length", length);
				if (offset + length > buffer.Length)
					throw EX.ArgumentOutOfRange("offset + length", offset + length, buffer.Length - 1);
				for (int i = 0; i < length; i++)
				{
					int k = Next();
					buffer[offset + i] = (byte)(k ^ (k >> 16));
				}
			}
		}

		class Rand2
		{
			private const int Bound1 = Int32.MaxValue;			// 2^31 - 1
			private const int Bound2 = Int32.MaxValue - 248;

			private int _x0;
			private int _y0;

			public Rand2()
				: this((int)WatchTimer.Query(0))
			{
			}

			public Rand2(int seed)
			{
				seed = Math.Max(Math.Abs(seed), 1);
				_x0 = Math.Abs(62089911 * seed); // % Bound1;
				_y0 = seed % Bound2;
			}

			public int Next()
			{
				int x1 = Math.Abs(48271 * _x0 + 1); // % Bound1;
				int y1 = Math.Abs(40692 * _y0 + 1) % Bound2;
				int z = Math.Abs(x1 - y1); // % Bound1;
				_x0 = x1;
				_y0 = y1;
				return z;
			}

			public void Fill(byte[] buffer, int offset, int length)
			{
				if (buffer == null)
					throw EX.ArgumentNull("buffer");
				if (offset < 0)
					throw EX.ArgumentOutOfRange("offset", offset);
				if (length < 0)
					throw EX.ArgumentOutOfRange("length", length);
				if (offset + length >= buffer.Length)
					throw EX.ArgumentOutOfRange("offset + length", offset + length, buffer.Length - 1);
				for (int i = 0; i < length; i++)
				{
					int k = Next();
					buffer[offset + i] = (byte)(k ^ (k >> 16));
				}
			}
		}

		class Rand3
		{
			private const int Bound = Int32.MaxValue;			// 2^31 - 1

			private int _x0;
			private int _x1;

			public Rand3()
				: this((int)WatchTimer.Query(0))
			{
			}

			public Rand3(int seed)
			{
				_x0 = Math.Max(Math.Abs(seed), 1); // % Bound;
				_x1 = Math.Abs(_x0 * 16807);
			}

			public int Next()
			{
				int x2 = Math.Abs(271828183 * _x1 - 314159269 * _x0 + 1); // % Bound;
				_x0 = _x1;
				_x1 = x2;
				return _x1;
			}

			public void Fill(byte[] buffer, int offset, int length)
			{
				if (buffer == null)
					throw EX.ArgumentNull("buffer");
				if (offset < 0)
					throw EX.ArgumentOutOfRange("offset", offset);
				if (length < 0)
					throw EX.ArgumentOutOfRange("length", length);
				if (offset + length > buffer.Length)
					throw EX.ArgumentOutOfRange("offset + length", offset + length, buffer.Length - 1);
				for (int i = 0; i < length; i++)
				{
					int k = Next();
					buffer[offset + i] = (byte)(k ^ (k >> 16));
				}
			}
		}
	}
}
