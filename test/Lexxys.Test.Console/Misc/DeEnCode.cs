using Lexxys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con
{
	class DeEnCode
	{
		public static void Go()
		{
			EncodeId(123456789, 12345551232322232, new byte[16] { 121, 11, 23, 44, 22, 21, 23, 57, 45, 76, 33, 252, 62, 112, 111, 17 });
		}

		private const string CharLin2 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

		public static unsafe string EncodeId(long id, ulong mult, byte[] mask)
		{
			if (mask == null)
				throw new ArgumentNullException(nameof(mask));
			if (mask.Length != 16)
				throw new ArgumentOutOfRangeException(nameof(mask.Length), mask.Length, null);

			ulong uid = (ulong)id;
			uint u0, u1, u2 = 0, u3 = 0;
			if (uid <= uint.MaxValue)
				if (mult <= uint.MaxValue)
					(u0, u1) = Mult((uint)uid, (uint)mult);
				else
					(u0, u1, u2) = Mult(mult, (uint)uid);
			else if (mult <= uint.MaxValue)
				(u0, u1, u2) = Mult(uid, (uint)mult);
			else
				(u0, u1, u2, u3) = Mult(uid, mult);

			uint v0;
			fixed (byte* m = mask)
			{
				uint* v = (uint*)m;
				u0 ^= v0 = v[0];
				u1 ^= v[1];
				u2 ^= v[2];
				u3 ^= v[3];
			}
			ulong p0 = (ulong)u1 << 32 | u0;
			ulong p1 = (ulong)u3 << 32 | u2;

			if (p0 == 0 && p1 == 0)
				return "0";

			char* buffer = stackalloc char[34];
			char* p = buffer + 33;
			*p = '\0';
			while (p0 > 0)
			{
				*--p = CharLin2[(int)(p0 % 62)];
				p0 /= 62;
			}
			int i = 0;
			while (p1 > 0)
			{
				++i;
				*--p = CharLin2[(int)(p1 % 62)];
				p1 /= 62;
			}
			*--p = CharLin2[i + 30];
			return new String(p);
		}

		public static (uint U0, uint U1) Mult(uint a, uint b)
		{
			var v = (ulong)a * b;
			return ((uint)v, (uint)(v >> 32));
		}

		public static (uint U0, uint U1, uint U2) Mult(ulong ab, uint c)
		{
			var bc = (ab & 0xFFFFFFFF) * c;
			var ac = (ab >> 32) * c;
			ac += bc >> 32;
			return ((uint)bc, (uint)ac, (uint)(ac >> 32));
		}

		public static (uint U0, uint U1, uint U2, uint U3) Mult(ulong ab, ulong cd)
		{
			var (u0, u1, u2) = Mult(ab, (uint)cd);
			var (v0, v1, _) = Mult(ab, (uint)(cd >> 32));
			var w1 = (ulong)u1 + v0;
			var w2 = (w1 >> 32) + u2 + v1;
			return (u0, (uint)w1, (uint)w2, (uint)(w2 >> 32));
		}

		public static (uint[] Div, uint Rem) DivRem(uint[] value, uint dv)
		{
			uint rem = 0;
			var res = new uint[value.Length];
			for (int i = value.Length - 1; i >= 0; --i)
			{
				ulong v = (ulong)value[i] + rem;
				res[i] = (uint)(v / dv);
				rem = (uint)(v % dv);
			}
			return (res, rem);
		}

		public static string EncDes(int id)
		{
			return default;
		}
	}
}
