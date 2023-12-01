// Lexxys Infrastructural library.
// file: Tools.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lexxys;

public static class Tools
{
	internal const int MaxStackAllocSize = 4096;
	internal const int SafeStackAllocByte = 1024;
	internal const int SafeStackAllocChar = SafeStackAllocByte / sizeof(char);

	/// <summary>
	/// Converts floating point value to rational number
	/// </summary>
	/// <param name="value">The value to convert</param>
	/// <param name="precision">Precision of the conversion</param>
	/// <param name="maxWidth">Maximum possible number of digits in minimum of both the numerator and the denominator</param>
	/// <returns>Tuple type with Item = enumerator, Item2 = denominator, Item3 = precision</returns>
	/// <remarks>
	///	Conversion stops when achieved required <paramref name="precision"/> or minimal number of digits in numerator or denominator is greater or equal then <paramref name="maxWidth"/>.
	///	special cases:
	///		0 = 0/1 with 0 precision
	///		NaN = 0/0 with NaN precision
	///		+infinity = 1/0 with positive infinity precision
	///		-infinity = -1/0 with negative infinity precision.
	/// </remarks>
	public static (long Numerator, long Denominator, double Precision) ToRational(double value, double precision = 0, int maxWidth = 0)
	{
		if (Double.IsNaN(value))
			return (0L, 0L, Double.NaN);
		if (Double.IsPositiveInfinity(value))
			return (1L, 0L, Double.PositiveInfinity);
		if (Double.IsNegativeInfinity(value))
			return (-1L, 0L, Double.PositiveInfinity);

		if (maxWidth <= 0)
			maxWidth = 20;
		if (precision < Double.Epsilon)
			precision = Double.Epsilon;
		bool neg = false;
		if (value < 0)
		{
			value = -value;
			neg = true;
		}

		if (value < precision)
			return (0L, 1L, value);

		long h0 = 0, h1 = 1;
		long k0 = 1, k1 = 0;
		long n = 1;
		double v = value;
		while (v != Math.Floor(v))
		{
			n *= 2;
			v *= 2;
		}
		long d = (long)v;
		long num = 0;
		long den = 1;
		int w = 0;
		double delta = value;

		for (int i = 0; i < 64; ++i)
		{
			long a = n > 0 ? d / n: 0;
			if (i > 0 && a == 0)
				break;
			long x = d;
			d = n;
			n = x % n;
			x = a;
			long h2 = x * h1 + h0; h0 = h1; h1 = h2;
			long k2 = x * k1 + k0; k0 = k1; k1 = k2;
			int w2 = DigitsNumber(Math.Min(k1, h1));
			if (w2 > maxWidth)
				break;
			if (delta <= precision)
			{
				if (w == 0)
					w = DigitsNumber(Math.Min(den, num));
				if (w2 > w)
					break;
			}
			num = h1;
			den = k1;
			delta = Math.Abs((double)num / den - value);
		}
		return (neg ? -num: num, den, delta);
	}

	/// <summary>
	/// Calculates number of decimal digits of the <paramref name="value"/>, i.e. ceil(1 + log10(<paramref name="value"/>))
	/// </summary>
	/// <param name="value">The value</param>
	/// <returns>With of the value</returns>
	public static int DigitsNumber(long value)
	{
		return (value < 0 ? -value: value) switch
		{
			< 10 => 1,
			< 100 => 2,
			< 1000 => 3,
			< 10000 => 4,
			< 100000 => 5,
			< 1000000 => 6,
			< 10000000 => 7,
			< 100000000 => 8,
			< 1000000000 => 9,
			< 10000000000 => 10,
			< 100000000000 => 11,
			< 1000000000000 => 12,
			< 10000000000000 => 13,
			< 100000000000000 => 14,
			< 1000000000000000 => 15,
			< 10000000000000000 => 16,
			< 100000000000000000 => 17,
			< 1000000000000000000 => 18,
			_ => 19
		};
	}

	public static ulong Power2Round(ulong value)
	{
		--value;
		value |= value >> 1;
		value |= value >> 2;
		value |= value >> 4;
		value |= value >> 8;
		value |= value >> 16;
		value |= value >> 32;
		return value + 1;
	}

	public static object? GetUnderlyingValue(object? value)
	{
		if (value == null)
			return null;
		if (value is IValue iv)
			value = iv.Value;
		if (value == null)
			return null;
		if (DBNull.Value.Equals(value))
			return null;
		if (!value.GetType().IsEnum)
			return value;

		return Type.GetTypeCode(Enum.GetUnderlyingType(value.GetType())) switch
		{
			TypeCode.Byte => (byte)value,
			TypeCode.Char => (char)value,
			TypeCode.Int16 => (short)value,
			TypeCode.Int32 => (int)value,
			TypeCode.Int64 => (long)value,
			TypeCode.SByte => (sbyte)value,
			TypeCode.UInt16 => (ushort)value,
			TypeCode.UInt32 => (uint)value,
			TypeCode.UInt64 => (ulong)value,
			_ => (int)value
		};
	}

	public static BitArray InitializeBitArray(params int[] values)
	{
		if (values == null)
			throw new ArgumentNullException(nameof(values));
		if (values.Length <= 1)
			return values.Length == 0 ? new BitArray(0): new BitArray(1, true);

		int mi = Int32.MaxValue;
		int ma = Int32.MinValue;
		for (int i = 0; i < values.Length; ++i)
		{
			if (mi > values[i])
				mi = values[i];
			if (ma < values[i])
				ma = values[i];
		}
		var ba = new BitArray(ma - mi + 1);
		for (int i = 0; i < values.Length; ++i)
		{
			ba[values[i] - mi] = true;
		}
		return ba;
	}

	public static T Cast<T>(object value)
	{
		return (T)value;
	}

	internal static string MachineName
	{
		get
		{
			try
			{
				return Environment.MachineName;
			}
			catch (InvalidOperationException)
			{
				return "local";
			}
		}
	}

	/// <summary>
	/// Get current system process ID.
	/// </summary>
	internal static int ProcessId =>
#if NET5_0_OR_GREATER
		Environment.ProcessId;
#else
		__processId ??= Process.GetCurrentProcess().Id;
	private static int? __processId;
#endif

	/// <summary>
	/// Get executable name of the current module.
	/// </summary>
	internal static string ModuleFileName => __moduleFileName ??= Process.GetCurrentProcess().MainModule?.FileName ?? "internal";
	private static string? __moduleFileName;

	internal static string DomainName
	{
		get
		{
			try
			{
				return (__domainName ??= AppDomain.CurrentDomain.FriendlyName);
			}
			catch (AppDomainUnloadedException)
			{
				return "unloaded";
			}
		}
	}
	private static string? __domainName;
}
