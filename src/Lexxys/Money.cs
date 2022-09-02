// Lexxys Infrastructural library.
// file: Money.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#undef CSHARP_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;

#pragma warning disable CA2225 // Operator overloads have named alternates
#pragma warning disable CA1051 // Do not declare visible instance fields

namespace Lexxys
{
	using Xml;

	/// <summary>
	/// Represents a money value.
	/// </summary>
	[Serializable]
	public readonly struct Money:
#if NET6_0_OR_GREATER && CSHARP_PREVIEW
		ISignedNumber<Money>, IConvertible, IDumpValue, IDumpXml, IDumpJson
#else
#if NET6_0_OR_GREATER
		ISpanFormattable,
#endif
		IComparable, IComparable<Money>, IConvertible, IEquatable<Money>, IFormattable, ISerializable, IDumpValue, IDumpXml, IDumpJson
#endif
	{
		private readonly long _value;
		private readonly Currency _currency;

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
		/// </summary>
		/// <param name="value">Amount value</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		public Money(int value, Currency? currency = null)
		{
			_currency = currency ?? Currency.ApplicationDefault;
			_value = (long)value * _currency.Multiplier;
		}

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
		/// </summary>
		/// <param name="value">Amount value</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		public Money(long value, Currency? currency = null)
		{
			_currency = currency ?? Currency.ApplicationDefault;
			_value = checked(value * _currency.Multiplier);
		}

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
		/// </summary>
		/// <param name="value">Amount value</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		public Money(decimal value, Currency? currency = null)
		{
			_currency = currency ?? Currency.ApplicationDefault;
			var v = value * _currency.Multiplier;
			if (v < long.MinValue || v > long.MaxValue)
				throw new OverflowException($"The computed value {v} overflows of the valid range.")
					.Add(nameof(value), value);
			_value = (long)v;
		}

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
		/// </summary>
		/// <param name="value">Amount value</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		public Money(double value, Currency? currency = null)
		{
			_currency = currency ?? Currency.ApplicationDefault;
			var v = value * _currency.Multiplier;
			if (!IsInRange(v))
				throw new OverflowException($"The computed value {v} overflows of the valid range.")
					.Add(nameof(value), value);
			_value = (long)v;
		}

		private Money(ulong value, Currency currency)
		{
			_value = (long)value;
			_currency = currency;
		}

		private Money(SerializationInfo info, StreamingContext context)
		{
			if (info is null)
				throw new ArgumentNullException(nameof(info));

			_value = info.GetInt64("value");
			string? code = info.GetString("cur");
			if (code == null)
			{
				_currency = Currency.ApplicationDefault;
			}
			else
			{
				int i = __internalCurrencies.FindIndex(o => o.Code == code);
				_currency = i >= 0 ? __internalCurrencies[i]: Currency.Create(code, info.GetByte("prec"), info.GetString("sym"));
			}
		}

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified amount in cents and currency.
		/// </summary>
		/// <param name="minorValue">The amount value in cents.</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		/// <returns></returns>
		public static Money Create(long minorValue, Currency? currency = null)
		{
			return new Money((ulong)minorValue, currency ?? Currency.ApplicationDefault);
		}

		/// <summary>
		/// Integer part of the the amount.
		/// </summary>
		public long MajorAmount => _value / Currency.Multiplier;
		/// <summary>
		/// Amount in cents.
		/// </summary>
		public long MinorAmount => _value;
		public decimal Amount => (decimal)_value / Currency.Multiplier;
		public Currency Currency => _currency ?? Currency.ApplicationDefault;

		/// <summary>
		/// Splits the <see cref="Money"/> value into two baskets according to the the specified <paramref name="ratio"/>.
		/// </summary>
		/// <param name="ratio">The ratio osed to splint the <see cref="Money"/> value.</param>
		/// <returns></returns>
		public (Money, Money) Split(double ratio)
		{
			var value = _value * ratio;
			if (!IsInRange(value))
				throw new OverflowException($"The computed value {value} overflows of the valid range.")
					.Add(nameof(_value), _value)
					.Add(nameof(ratio), ratio);
			var first = Create((long)Math.Round(value), _currency);
			return (first, Create(_value - first._value, _currency));
		}

		/// <summary>
		/// Splits the <see cref="Money"/> value into two baskets according to the the specified ratio as <paramref name="numerator"/>/<paramref name="denominator"/>.
		/// </summary>
		/// <param name="numerator">Numerator value of the ratio.</param>
		/// <param name="denominator">Denominator value of the ratio.</param>
		/// <returns></returns>
		public (Money, Money) Split(int numerator, int denominator)
		{
			if (denominator == 0)
				throw new ArgumentOutOfRangeException(nameof(denominator), denominator, null);

			var value = (decimal)_value * numerator / denominator;
			if (value < long.MinValue || value > long.MaxValue)
				throw new OverflowException($"The computed value {value} overflows of the valid range.")
					.Add(nameof(_value), _value)
					.Add(nameof(numerator), numerator)
					.Add(nameof(denominator), denominator);
			var first = Create((long)Math.Round(value), _currency);
			return (first, Create(_value - first._value, _currency));
		}

		/// <summary>
		/// Distribute the money value by two baskets according to their weights.
		/// </summary>
		/// <param name="basket1">Weight of the basket 1</param>
		/// <param name="basket2">Weight of the basket 2</param>
		/// <returns></returns>
		public Money[] Allocate(double basket1, double basket2)
		{
			if (!(basket1 >= 0) || double.IsPositiveInfinity(basket1))
				throw new ArgumentOutOfRangeException(nameof(basket1), basket1, null);
			if (!(basket2 >= 0) || double.IsPositiveInfinity(basket2))
				throw new ArgumentOutOfRangeException(nameof(basket2), basket2, null);

			double total = basket1 + basket2;
			double value = _value * basket1 / total;
			if (!IsInRange(value))
				throw new OverflowException($"The computed value {value} overflows of the valid range.")
					.Add(nameof(_value), _value)
					.Add(nameof(basket1), basket1)
					.Add(nameof(basket2), basket2);
			var first = Create((long)Math.Round(value), _currency);
			var second = Create(_value - first._value, _currency);
			return new[] { first, second };
		}

		/// <summary>
		/// Distribute the money value by three baskets according to their weights.
		/// </summary>
		/// <param name="basket1">Weight of the basket 1</param>
		/// <param name="basket2">Weight of the basket 2</param>
		/// <param name="basket3">Weight of the basket 3</param>
		/// <returns></returns>
		public Money[] Allocate(double basket1, double basket2, double basket3)
		{
			if (!(basket1 >= 0) || double.IsPositiveInfinity(basket1))
				throw new ArgumentOutOfRangeException(nameof(basket1), basket1, null);
			if (!(basket2 >= 0) || double.IsPositiveInfinity(basket2))
				throw new ArgumentOutOfRangeException(nameof(basket2), basket2, null);
			if (!(basket3 >= 0) || double.IsPositiveInfinity(basket3))
				throw new ArgumentOutOfRangeException(nameof(basket3), basket3, null);

			double total = basket1 + basket2 + basket3;
			double value = _value * basket1 / total;
			if (!IsInRange(value))
				throw new OverflowException($"The computed value {value} overflows of the valid range.")
					.Add(nameof(_value), _value)
					.Add(nameof(basket1), basket1)
					.Add(nameof(basket2), basket2)
					.Add(nameof(basket3), basket3);

			var first = Create((long)Math.Round(value), _currency);
			var second = Create((long)Math.Round((_value - first._value) * basket2 / (total - basket1)), _currency);
			var third = Create(_value - first._value - second._value, _currency);
			return new[] { first, second, third };
		}

		/// <summary>
		/// Distribute the money value by baskets according to their weights.
		/// </summary>
		/// <param name="baskets">Array of the baskets weights</param>
		/// <returns></returns>
		public Money[] Allocate(params double[] baskets)
		{
			if (baskets == null)
				throw new ArgumentNullException(nameof(baskets));
			if (baskets.Length < 2)
				return baskets.Length == 0 ? Array.Empty<Money>() : new[] { this };

			double total = 0;
			for (int i = 0; i < baskets.Length; i++)
			{
				var basket = baskets[i];
				if (!(basket >= 0) || double.IsPositiveInfinity(basket))
					throw new ArgumentOutOfRangeException($"{nameof(baskets)}[{i}]", basket, null);
				total += basket;
			}
			var result = new Money[baskets.Length];
			long rest = _value;
			for (int i = 0; i < baskets.Length - 1; ++i)
			{
				double value = rest * baskets[i] / total;
				if (!IsInRange(value))
					throw new OverflowException($"The computed value {value} overflows of the valid range.")
						.Add(nameof(_value), _value)
						.Add(nameof(baskets), "[" + String.Join(",", baskets) + "]");
				var item = Create((long)Math.Round(value), _currency);
				rest -= item._value;
				total -= baskets[i];
				result[i] = item;
			}
			result[result.Length - 1] = Create(rest, _currency);
			return result;
		}

		/// <summary>
		/// Distributes the money value evenly by specified <paramref name="count"/> of baskets.
		/// </summary>
		/// <param name="count">Number of buskets.</param>
		public Money[] Distribute(int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, null);
			if (count == 0)
				return Array.Empty<Money>();
			if (count == 1)
				return new[] { this };

			var result = new Money[count];
			long rest = _value;
			for (int i = 0; i < result.Length - 1; ++i)
			{
				var item = Create((long)Math.Round((double)rest / count), _currency);
				rest -= item._value;
				--count;
				result[i] = item;
			}
			result[result.Length - 1] = Create(rest, _currency);
			return result;
		}

		private static bool IsInRange(double value) => !double.IsNaN(value) && value >= long.MinValue && value <= long.MaxValue;

		#region Object, IFormattable, IComparable, IComparable<Money>, IEquatable<Money>

		public override bool Equals(object? obj)
		{
			return (obj is Money money) && Equals(money);
		}

		public bool Equals(Money other)
		{
			return Object.ReferenceEquals(Currency, other.Currency) && _value == other._value;
		}

		public override int GetHashCode()
		{
			return HashCode.Join(_value.GetHashCode(), Currency.GetHashCode());
		}

		public override string ToString()
		{
			return Amount.ToString("N" + Currency.Precision, CultureInfo.InvariantCulture) + " " + Currency.Code;
		}

		public string ToString(string? format, IFormatProvider? formatProvider = null)
		{
			return Amount.ToString(format, formatProvider)
				.Replace("s", Currency.Symbol)
				.Replace("S", Currency.Code);
		}

		public int CompareTo(Money other)
		{
			if (Currency == other.Currency)
				return Amount.CompareTo(other.Amount);
			throw new ArgumentException(SR.DifferentCurrencyCodes(Currency, other.Currency));
		}

		public int CompareTo(object? obj)
		{
			if (obj is Money money)
				return CompareTo(money);
			throw new ArgumentOutOfRangeException(nameof(obj));
		}

		public DumpWriter DumpContent(DumpWriter writer)
		{
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));
			return writer.Dump(Amount).Text('[').Text(Currency.Code).Text(']');
		}

		string IDumpXml.XmlElementName => "money";

		#region INumber

#if NET6_0_OR_GREATER && CSHARP_PREVIEW
		static Money ISignedNumber<Money>.NegativeOne => __negOne;
		private static readonly Money __negOne = new Money(-1);
		static Money INumber<Money>.One => __one;
		private static readonly Money __one = new Money(1);
		static Money INumber<Money>.Zero => __zero;
		private static readonly Money __zero = new Money(0);
		static Money IAdditiveIdentity<Money, Money>.AdditiveIdentity => __zero;
		static Money IMultiplicativeIdentity<Money, Money>.MultiplicativeIdentity => __one;
#endif

		public static Money Abs(Money value)
		{
			return Create(Math.Abs(value._value), value._currency);
		}

		public static Money Clamp(Money value, Money min, Money max)
		{
			if (min < max)
				throw new ArgumentException(SR.ValueCannotBeGreaterThan(min, max));
			if (value.Currency != min.Currency)
				throw new ArgumentException(SR.DifferentCurrencyCodes(value.Currency, min.Currency));
			if (value.Currency != max.Currency)
				throw new ArgumentException(SR.DifferentCurrencyCodes(value.Currency, max.Currency));
			return value < min ? min : value > max ? max : value;
		}

#if NET6_0_OR_GREATER && CSHARP_PREVIEW
		static Money INumber<Money>.Sign(Money value)
		{
			return value._value < 0 ? value.Currency.MinusOne : value._value > 0 ? value.Currency.One : value.Currency.Zero;
			//return value._value < 0 ? Create(-1, value._currency): value._value > 0 ? Create(1, value._currency): Create(0, value._currency);
		}

		static Money INumber<Money>.Create<TOther>(TOther value)
		{
			return TryCreate(value, out var result) ? result : throw new NotSupportedException();
		}

		static Money INumber<Money>.CreateSaturating<TOther>(TOther value)
		{
			return TryCreate(value, out var result) ? result : throw new NotSupportedException();
		}

		static Money INumber<Money>.CreateTruncating<TOther>(TOther value)
		{
			return TryCreate(value, out var result) ? result : throw new NotSupportedException();
		}

		static (Money Quotient, Money Remainder) INumber<Money>.DivRem(Money left, Money right)
		{
			return (left / right.Amount, left % right.Amount);
		}
#endif

		public static (Money Quotient, Money Remainder) DivRem(Money left, decimal right)
		{
			return (left / right, left % right);
		}

		public static (Money Quotient, Money Remainder) DivRem(Money left, long right)
		{
			return (left / right, left % right);
		}

		public static Money Max(Money x, Money y)
		{
			if (x.Currency.Code != y.Currency.Code)
				throw new ArgumentException(SR.DifferentCurrencyCodes(x.Currency, y.Currency));
			return x._value < y._value ? y : x;
		}

		public static Money Min(Money x, Money y)
		{
			if (x.Currency.Code != y.Currency.Code)
				throw new ArgumentException(SR.DifferentCurrencyCodes(x.Currency, y.Currency));
			return x._value > y._value ? y : x;
		}

		public static Money Parse(string value, NumberStyles style, IFormatProvider? provider)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (!TryParse(value, style, provider, out var result))
				throw new FormatException(SR.CannotParseValue(value.ToString()));
			return result;
		}

#if NET6_0_OR_GREATER
		public static Money Parse(ReadOnlySpan<char> value, NumberStyles style, IFormatProvider? provider)
		{
			if (!TryParse(value, style, provider, out var result))
				throw new FormatException(SR.CannotParseValue(value.ToString()));
			return result;
		}

#if CSHARP_PREVIEW
		static bool INumber<Money>.TryCreate<TOther>(TOther value, out Money result) => TryCreate(value, out result);

		private static bool TryCreate<TOther>(TOther value, out Money result) where TOther : INumber<TOther>
		{
			if (value is Money m)
			{
				result = m;
				return true;
			}
			if (TryCreate<decimal>(value, out decimal d))
			{
				result = (Money)d;
				return true;
			}
			result = default;
			return false;

			static bool TryCreate<T>(TOther value, out T result) where T : INumber<T> => T.TryCreate(value, out result);
		}
#endif

		public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
		{
			bool start = false;
			bool small = false;
			if (format.Length > 0)
			{
				if ((format[0] | ('s' ^ 'S')) == 's')
				{
					start = true;
					small = format[0] == 's';
					format = format[1..];
				}
				else if ((format[^1] | ('s' ^ 'S')) == 's')
				{
					small = format[^1] == 's';
					format = format[0..^1];
				}
			}
			var code = small ? Currency.Symbol.AsSpan(): Currency.Code.AsSpan();
			if (start)
			{
				if (!Append(destination, code, true))
				{
					charsWritten = 0;
					return false;
				}
				destination = destination.Slice(code.Length + 1);
			}
			if (!Amount.TryFormat(destination, out var written, format, provider))
			{
				charsWritten = 0;
				return false;
			}
			if (!start && !Append(destination, code, false))
			{
				charsWritten = 0;
				return false;
			}
			charsWritten = written + code.Length + 1;
			return true;

			static bool Append(Span<char> span, ReadOnlySpan<char> value, bool start)
			{
				if (span.Length <= value.Length)
					return false;
				if (!start)
				{
					span[0] = ' ';
					span = span.Slice(1);
				}
				value.CopyTo(span);
				span = span.Slice(value.Length);
				if (start)
					span[0] = ' ';
				return true;
			}
		}
#endif

#if NET6_0_OR_GREATER
		public static Money Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
		{
			if (!TryParse(s, provider, out var value))
				throw new FormatException(SR.CannotParseValue(s.ToString()));
			return value;
		}
#endif

		public static Money Parse(string s, IFormatProvider? provider)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));
			if (!TryParse(s, provider, out var value))
				throw new FormatException(SR.CannotParseValue(s));
			return value;
		}

		public static bool TryParse(string value, out Money result)
		{
			return TryParse(value, NumberStyles.Currency, CultureInfo.CurrentCulture, out result);
		}

		public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Money result)
		{
			return TryParse(s, NumberStyles.Currency, provider, out result);
		}

		/// <summary>
		/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="value"/>.</param>
		/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="value"/>.</param>
		/// <param name="result">The parsed <see cref="Money"/> value.</param>
		/// <returns>true if <paramref name="value"/> was converted successfully; otherwise, false.</returns>
		public static bool TryParse(string? value, NumberStyles style, IFormatProvider? provider, out Money result)
		{
#if NET6_0_OR_GREATER
			if (value == null)
			{
				result = default;
				return false;
			}
			return TryParse(value.AsSpan(), style, provider, out result);
#else
			if ((value = value.TrimToNull()) == null)
			{
				result = default;
				return false;
			}
			Currency? currency = null;
			if (value.Length > 4 && value[value.Length - 4] == ' ')
			{
				var symbol = value.Substring(value.Length - 3);
				if (Char.IsLetter(symbol[0]) && Char.IsLetter(symbol[1]) && Char.IsLetter(symbol[2]))
				{
					currency = Currency.Find(symbol);
					if (currency == null)
					{
						result = default;
						return false;
					}
					value = value.Slice(0, value.Length - 4).TrimEnd();
				}
			}
			if (decimal.TryParse(value, style, provider, out var d))
			{
				result = new Money(d, currency);
				return true;
			}
			result = default;
			return false;
#endif
		}

#if NET6_0_OR_GREATER
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Money result)
		{
			return TryParse(s, NumberStyles.Currency, provider, out result);
		}

		public static bool TryParse(ReadOnlySpan<char> value, NumberStyles style, IFormatProvider? provider, out Money result)
		{
			value = value.Trim();
			if (value.Length == 0)
			{
				result = default;
				return false;
			}
			Currency? currency = null;
			if (value.Length > 4 && value[value.Length - 4] == ' ')
			{
				var symbol = value.Slice(value.Length - 3);
				if (Char.IsLetter(symbol[0]) && Char.IsLetter(symbol[1]) && Char.IsLetter(symbol[2]))
				{
					currency = Currency.Find(symbol.ToString());
					if (currency == null)
					{
						result = new Money();
						return false;
					}
					value = value.Slice(0, value.Length - 5).TrimEnd();
				}
			}
			if (decimal.TryParse(value, style, provider, out var d))
			{
				result = new Money(d, currency);
				return true;
			}
			result = default;
			return false;
		}
#endif

		#endregion

		public XmlBuilder ToXmlContent(XmlBuilder builder)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));
			return builder.InAttribute ? builder.Value(XmlTools.Convert(Amount) + " " + Currency.Code) :
				builder.Item("amount", Amount).Item("currency", Currency.Code);
		}

		public JsonBuilder ToJsonContent(JsonBuilder json)
		{
			if (json is null)
				throw new ArgumentNullException(nameof(json));
			return json.InArray ? json.Val(XmlTools.Convert(Amount) + " " + Currency.Code) :
				json.Item("amount").Val(Amount).Item("currency").Val(Currency.Code);
		}

		#endregion

		#region IConvertable

		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Decimal;
		}

		bool IConvertible.ToBoolean(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToBoolean(provider);
		}

		char IConvertible.ToChar(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToChar(provider);
		}

		sbyte IConvertible.ToSByte(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToSByte(provider);
		}

		byte IConvertible.ToByte(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToByte(provider);
		}

		short IConvertible.ToInt16(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToInt16(provider);
		}

		ushort IConvertible.ToUInt16(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToUInt16(provider);
		}

		int IConvertible.ToInt32(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToInt32(provider);
		}

		uint IConvertible.ToUInt32(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToUInt32(provider);
		}

		long IConvertible.ToInt64(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToInt64(provider);
		}

		ulong IConvertible.ToUInt64(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToUInt64(provider);
		}

		float IConvertible.ToSingle(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToSingle(provider);
		}

		double IConvertible.ToDouble(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToDouble(provider);
		}

		decimal IConvertible.ToDecimal(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToDecimal(provider);
		}

		DateTime IConvertible.ToDateTime(IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToDateTime(provider);
		}

		string IConvertible.ToString(IFormatProvider? provider)
		{
			return Amount.ToString(provider);
		}

		object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
		{
			return ((IConvertible)Amount).ToType(conversionType, provider);
		}

		#endregion

		#region Convertion operators

		public static Money FromInt32(int value) => new Money(value);
		public static Money FromInt64(long value) => new Money(value);
		public static Money FromDecimal(decimal value) => new Money(value);
		public static Money FromDouble(double value) => new Money(value);
		public int ToInt32() => checked((int)(_value / Currency.Multiplier));
		public long ToInt64() => _value / Currency.Multiplier;
		public decimal ToDecimal() => Amount;
		public double ToDouble() => (double)_value / Currency.Multiplier;

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info is null)
				throw new ArgumentNullException(nameof(info));
			info.AddValue("value", _value);
			info.AddValue("cur", _currency.Code);
			if (__internalCurrencies.FindIndex(_currency) > 0)
			{
				info.AddValue("prec", (byte)_currency.Precision);
				info.AddValue("sym", _currency.Symbol);
			}
		}
		private static readonly Currency[] __internalCurrencies = new[] { Currency.Empty, Currency.Usd, Currency.Eur, Currency.Rub, Currency.Uzs };


		public static explicit operator Money(int value) => new Money(value);

		public static explicit operator Money(long value) => new Money(value);

		public static explicit operator Money(decimal value) => new Money(value);

		public static explicit operator Money(double value) => new Money(value);

		public static explicit operator int(Money value) => checked((int)(value._value / value.Currency.Multiplier));

		public static explicit operator long(Money value) => value._value / value.Currency.Multiplier;

		public static implicit operator decimal(Money value) => value.Amount;

		public static explicit operator double(Money value) => (double)value._value / value.Currency.Multiplier;

		#endregion

		#region Comparison operators

		public static bool operator ==(Money left, Money right) => left.Currency.Code == right.Currency.Code && left._value == right._value;

		public static bool operator !=(Money left, Money right) => left.Currency.Code != right.Currency.Code || left._value != right._value;

		public static bool operator >(Money left, Money right) => left.Currency.Code == right.Currency.Code && left._value > right._value;

		public static bool operator >=(Money left, Money right) => left.Currency.Code == right.Currency.Code && left._value >= right._value;

		public static bool operator <(Money left, Money right) => left.Currency.Code == right.Currency.Code && left._value < right._value;

		public static bool operator <=(Money left, Money right) => left.Currency.Code == right.Currency.Code && left._value <= right._value;

		public static bool operator ==(Money left, long right) => left._value == right * left.Currency.Multiplier;

		public static bool operator !=(Money left, long right) => left._value != right * left.Currency.Multiplier;

		public static bool operator >(Money left, long right) => left._value > right * left.Currency.Multiplier;

		public static bool operator >=(Money left, long right) => left._value >= right * left.Currency.Multiplier;

		public static bool operator <(Money left, long right) => left._value < right * left.Currency.Multiplier;

		public static bool operator <=(Money left, long right) => left._value <= right * left.Currency.Multiplier;

		public static bool operator ==(Money left, decimal right) => left._value == right * left.Currency.Multiplier;

		public static bool operator !=(Money left, decimal right) => left._value != right * left.Currency.Multiplier;

		public static bool operator >(Money left, decimal right) => left._value > right * left.Currency.Multiplier;

		public static bool operator >=(Money left, decimal right) => left._value >= right * left.Currency.Multiplier;

		public static bool operator <(Money left, decimal right) => left._value < right * left.Currency.Multiplier;

		public static bool operator <=(Money left, decimal right) => left._value <= right * left.Currency.Multiplier;

		public static bool operator ==(Money left, double right) => left._value == right * left.Currency.Multiplier;

		public static bool operator !=(Money left, double right) => left._value != right * left.Currency.Multiplier;

		public static bool operator >(Money left, double right) => left._value > right * left.Currency.Multiplier;

		public static bool operator >=(Money left, double right) => left._value >= right * left.Currency.Multiplier;

		public static bool operator <(Money left, double right) => left._value < right * left.Currency.Multiplier;

		public static bool operator <=(Money left, double right) => left._value <= right * left.Currency.Multiplier;

		#endregion

		#region Math operators

		public static Money operator +(Money left, Money right)
		{
			if (left.Currency.Code != right.Currency.Code)
				throw new ArgumentException(SR.DifferentCurrencyCodes(left.Currency, right.Currency));
			return Create(checked(left._value + right._value), left._currency);
		}

		public static Money operator +(Money left, long right)
		{
			return Create(checked(left._value + right * left.Currency.Multiplier), left._currency);
		}

		public static Money operator +(Money left, decimal right)
		{
			return Create(checked(left._value + (long)(right * left.Currency.Multiplier)), left._currency);
		}

		public static Money operator +(Money left, double right)
		{
			return Create(checked(left._value + (long)(right * left.Currency.Multiplier)), left._currency);
		}

		public static Money operator +(int left, Money right)
		{
			return Create(checked((long)left * right.Currency.Multiplier + right._value), right._currency);
		}

		public static Money operator +(long left, Money right)
		{
			return Create(checked(left * right.Currency.Multiplier + right._value), right._currency);
		}

		public static Money operator +(decimal left, Money right)
		{
			return Create(checked((long)(left * right.Currency.Multiplier) + right._value), right._currency);
		}

		public static Money operator +(double left, Money right)
		{
			return Create(checked((long)(left * right.Currency.Multiplier) + right._value), right._currency);
		}

		public static Money operator -(Money left, Money right)
		{
			if (left.Currency.Code != right.Currency.Code)
				throw new ArgumentException(SR.DifferentCurrencyCodes(left.Currency, right.Currency));
			return Create(checked(left._value - right._value), left._currency);
		}

		public static Money operator -(Money left, int right)
		{
			return Create(checked(left._value - (long)right * left.Currency.Multiplier), left._currency);
		}

		public static Money operator -(Money left, long right)
		{
			return Create(checked(left._value - right * left.Currency.Multiplier), left._currency);
		}

		public static Money operator -(Money left, decimal right)
		{
			return Create(checked(left._value - (long)(right * left.Currency.Multiplier)), left._currency);
		}

		public static Money operator -(Money left, double right)
		{
			return Create(checked(left._value - (long)(right * left.Currency.Multiplier)), left._currency);
		}

		public static Money operator -(long left, Money right)
		{
			return Create(checked(left * right.Currency.Multiplier - right._value), right._currency);
		}

		public static Money operator -(decimal left, Money right)
		{
			return Create(checked((long)(left * right.Currency.Multiplier) - right._value), right._currency);
		}

		public static Money operator -(double left, Money right)
		{
			return Create(checked((long)(left * right.Currency.Multiplier) - right._value), right._currency);
		}

		public static Money operator *(Money left, int right)
		{
			return Create(checked(left._value * right), left._currency);
		}

		public static Money operator *(Money left, long right)
		{
			return Create(checked(left._value * right), left._currency);
		}

		public static Money operator *(Money left, decimal right)
		{
			return Create((long)(left._value * right), left._currency);
		}

		public static Money operator *(Money left, double right)
		{
			return Create(checked((long)(left._value * right)), left._currency);
		}

		public static Money operator *(int left, Money right)
		{
			return Create(checked(left * right._value), right._currency);
		}

		public static Money operator *(long left, Money right)
		{
			return Create(checked(left * right._value), right._currency);
		}

		public static Money operator *(decimal left, Money right)
		{
			return Create((long)(left * right._value), right._currency);
		}

		public static Money operator *(double left, Money right)
		{
			return Create(checked((long)(left * right._value)), right._currency);
		}

		public static Money operator /(Money left, int right)
		{
			return Create(left._value / right, left._currency);
		}

		public static Money operator /(Money left, long right)
		{
			return Create(left._value / right, left._currency);
		}

		public static Money operator /(Money left, decimal right)
		{
			return Create((long)(left._value / right), left._currency);
		}

		public static Money operator /(Money left, double right)
		{
			return Create(checked((long)(left._value / right)), left._currency);
		}

		public static Money operator %(Money left, int right)
		{
			return Create(left._value % checked(right * left.Currency.Multiplier), left._currency);
		}

		public static Money operator %(Money left, long right)
		{
			return Create(left._value % checked(right * left.Currency.Multiplier), left._currency);
		}

		public static Money operator %(Money left, decimal right)
		{
			return Create(left._value % (long)(right * left.Currency.Multiplier), left._currency);
		}

		public static Money operator %(Money left, double right)
		{
			return Create(checked(left._value % (long)(right * left.Currency.Multiplier)), left._currency);
		}

		public static Money operator --(Money value)
		{
			return Create(checked(value._value - value.Currency.Multiplier), value._currency);
		}

		public static Money operator ++(Money value)
		{
			return Create(checked(value._value + value.Currency.Multiplier), value._currency);
		}

		public static Money operator -(Money value)
		{
			return Create(-value._value, value._currency);
		}

		public static Money operator +(Money value)
		{
			return value;
		}

#if NET6_0_OR_GREATER && PREVIW
		static Money IDivisionOperators<Money, Money, Money>.operator /(Money left, Money right)
		{
			return left / (decimal)right;
		}

		static Money IModulusOperators<Money, Money, Money>.operator %(Money left, Money right)
		{
			return Create(left._value % right._value, left._currency);
		}

		static Money IMultiplyOperators<Money, Money, Money>.operator *(Money left, Money right)
		{
			return left * (decimal)right;
		}
#endif

		#endregion
	}

	/// <summary>
	/// Represents a currency
	/// </summary>
	public sealed class Currency
	{
		/// <summary>
		/// ISO 4217 currency code (i.e. "EUR").
		/// </summary>
		public string Code { get; }
		/// <summary>
		/// Currency symbol (i.e "€").
		/// </summary>
		public string Symbol { get; }
		/// <summary>
		/// Number of dignificant digits after the decimal point.
		/// </summary>
		public int Precision { get; }
		/// <summary>
		/// Precision in power of 10.
		/// </summary>
		public int Multiplier { get; }

		/// <summary>
		/// Represents the largest possible <see cref="Money"/> value of the <see cref="Currency"/>.
		/// </summary>
		public readonly Money MaxValue;
		/// <summary>
		/// Represents the smallest possible <see cref="Money"/> value of the <see cref="Currency"/>.
		/// </summary>
		public readonly Money MinValue;
		/// <summary>
		/// Represents 1.00 <see cref="Money"/> value of the <see cref="Currency"/>.
		/// </summary>
		public readonly Money One;
		/// <summary>
		/// Represents -1.00 <see cref="Money"/> value of the <see cref="Currency"/>.
		/// </summary>
		public readonly Money MinusOne;
		/// <summary>
		/// Represents zero <see cref="Money"/> value of the <see cref="Currency"/>.
		/// </summary>
		public readonly Money Zero;

		private static Hashtable __mapByCode = new Hashtable();

		private Currency(string code, int precision, string? symbol = null)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));
			if (code.Length != 3)
				throw new ArgumentOutOfRangeException(nameof(code), code, null);
			if (precision < 0 || precision > 9)
				throw new ArgumentOutOfRangeException(nameof(precision), precision, null);

			Code = code;
			Symbol = symbol ?? code;
			Precision = precision;
			int multiplier = 1;
			for (int i = 0; i < Precision; ++i)
			{
				multiplier *= 10;
			}
			Multiplier = multiplier;

			One = new Money(1, this);
			MinusOne = new Money(-1, this);
			Zero = Money.Create(0, this);
			MinValue = Money.Create(long.MinValue, this);
			MaxValue = Money.Create(long.MaxValue, this);
		}

		/// <summary>
		/// Finds a currency by currency code.
		/// </summary>
		/// <param name="code">Three-letter currency code</param>
		/// <returns>A found currency or null.</returns>
		public static Currency? Find(string code)
		{
			return code == null ? null: (Currency?)__mapByCode[code.ToUpperInvariant()];
		}

		/// <summary>
		/// Creates a new currency or returns an exising one.
		/// </summary>
		/// <param name="code">Three-letters ISO 4217 currency code (i.e "EUR").</param>
		/// <param name="precision">Number of dignificant digits after the decimal point in range from 0 to 9.</param>
		/// <param name="symbol">Currency symbol (i.e "€").</param>
		/// <returns>A currency</returns>
		public static Currency Create(string code, int precision, string? symbol = null)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));

			code = code.ToUpperInvariant();
			Currency? result = null;

			Hashtable hash;
			Hashtable temp;
			do
			{
				hash = __mapByCode;
				var obj = __mapByCode[code];
				if (obj != null)
					return (Currency)obj;
				result ??= new Currency(code, precision, symbol);
				temp = (Hashtable)hash.Clone();
				temp[code] = result;
			} while (Interlocked.CompareExchange(ref __mapByCode, temp, hash) != hash);

			return result;
		}

		public static readonly Currency Current = Create(RegionInfo.CurrentRegion.ISOCurrencySymbol, CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalDigits, RegionInfo.CurrentRegion.CurrencySymbol);
		public static readonly Currency Empty = Create("CUR", 2, "\x00A4");
		public static readonly Currency Usd = Create("USD", 2, "$");
		public static readonly Currency Rub = Create("RUB", 2, "\x20BD");
		public static readonly Currency Uzs = Create("UZS", 2);
		public static readonly Currency Eur = Create("EUR", 2, "\x20AC");

		/// <summary>
		/// Gets or sets default <see cref="Currency"/> value.
		/// </summary>
		public static Currency ApplicationDefault { get; set; } = Current;
	}
}
