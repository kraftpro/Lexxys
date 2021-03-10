// Lexxys Infrastructural library.
// file: Money.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Lexxys
{
	/// <summary>
	/// Represents a money value.
	/// </summary>
	public readonly struct Money: IFormattable, IComparable, IConvertible, IComparable<Money>, IEquatable<Money>, IDumpValue, IDumpXml, IDumpJson
	{
		private readonly long _value;
		private readonly Currency _currency;

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
		/// </summary>
		/// <param name="value">Amount value</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		public Money(int value, Currency currency = null)
		{
			_currency = currency ?? Currency.ApplicationDefault;
			_value = (long)value * _currency.Multiplier;
		}

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
		/// </summary>
		/// <param name="value">Amount value</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		public Money(long value, Currency currency = null)
		{
			_currency = currency ?? Currency.ApplicationDefault;
			_value = checked(value * _currency.Multiplier);
		}

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
		/// </summary>
		/// <param name="value">Amount value</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		public Money(decimal value, Currency currency = null)
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
		public Money(double value, Currency currency = null)
		{
			_currency = currency ?? Currency.ApplicationDefault;
			var v = value * _currency.Multiplier;
			if (!IsInRange(v))
				throw new OverflowException($"The computed value {v} overflows of the valid range.")
					.Add(nameof(value), value);
			_value = (long)v;
		}

		private Money(ulong value, Currency currency = null)
		{
			_value = (long)value;
			_currency = currency ?? Currency.ApplicationDefault;
		}

		/// <summary>
		/// Creates a new instance of <see cref="Money"/> for specified amount in cents and currency.
		/// </summary>
		/// <param name="minorValue">The amount value in cents.</param>
		/// <param name="currency">The money currency (when ommited <see cref="Currency"/>.ApplicationDefault will be used).</param>
		/// <returns></returns>
		public static Money Create(long minorValue, Currency currency = null)
		{
			return new Money((ulong)minorValue, currency);
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
				return baskets.Length == 0 ? Array.Empty<Money>(): new[] { this };

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
				return EmptyArray<Money>.Value;
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

		public override bool Equals(object obj)
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
			Contract.Ensures(Contract.Result<String>() != null);
			return Amount.ToString("N" + Currency.Precision) + " " + Currency.Code;
		}

		public string ToString(string format)
		{
			Contract.Ensures(Contract.Result<String>() != null);
			return Amount.ToString(format).Replace("s", Currency.Symbol).Replace("S", Currency.Code);
		}

		public string ToString(string format, IFormatProvider formatProvider)
		{
			Contract.Ensures(Contract.Result<String>() != null);
			return Amount.ToString(format, formatProvider).Replace("s", Currency.Symbol).Replace("S", Currency.Code);
		}

		public int CompareTo(Money other)
		{
			if (!Currency.Equals(other.Currency))
				throw new ArgumentOutOfRangeException(nameof(other), other, null);
			return Amount.CompareTo(other.Amount);
		}

		public int CompareTo(object obj)
		{
			if (obj is not Money money)
				throw new ArgumentOutOfRangeException(nameof(obj));
			return CompareTo(money);
		}

		public DumpWriter DumpContent(DumpWriter writer)
		{
			return writer.Dump(Amount).Text('[').Text(Currency.Code).Text(']');
		}

		string IDumpXml.XmlElementName => "money";

		public XmlBuilder ToXmlContent(XmlBuilder builder)
		{
			return builder.InAttribute ? builder.Value(Xml.XmlTools.Convert(Amount) + " " + Currency.Code):
				builder.Item("amount", Amount).Item("currency", Currency.Code);
		}

		public JsonBuilder ToJsonContent(JsonBuilder builder)
		{
			return builder.InArray ? builder.Val(Xml.XmlTools.Convert(Amount) + " " + Currency.Code):
				builder.Item("amount").Val(Amount).Item("currency").Val(Currency.Code);
		}

		public static bool TryParse(string value, out Money result)
		{
			return TryParse(value, NumberStyles.Currency, CultureInfo.CurrentCulture, out result);
		}

		/// <summary>
		/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="value"/>.</param>
		/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="value"/>.</param>
		/// <param name="result">The parsed <see cref="Money"/> value.</param>
		/// <returns>true if <paramref name="value"/> was converted successfully; otherwise, false.</returns>
		public static bool TryParse(string value, NumberStyles style, IFormatProvider provider, out Money result)
		{
			if ((value = value.TrimToNull()) == null)
			{
				result = new Money();
				return false;
			}
			Currency currency = null;
			if (value.Length > 4 && value[value.Length - 4] == ' ')
			{
				var symbol = value.Substring(value.Length - 3);
				if (Char.IsLetter(symbol[0]) && Char.IsLetter(symbol[1]) && Char.IsLetter(symbol[2]))
				{
					currency = Currency.Find(symbol);
					if (currency == null)
					{
						result = new Money();
						return false;
					}
					value = value.Substring(0, value.Length - 5).TrimEnd();
				}
			}
			if (decimal.TryParse(value, style, provider, out var d))
			{
				result = new Money(d, currency);
				return true;
			}
			result = new Money();
			return false;
		}

		#endregion

		#region IConvertable

		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Decimal;
		}

		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToBoolean(provider);
		}

		char IConvertible.ToChar(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToChar(provider);
		}

		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToSByte(provider);
		}

		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToByte(provider);
		}

		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToInt16(provider);
		}

		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToUInt16(provider);
		}

		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToInt32(provider);
		}

		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToUInt32(provider);
		}

		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToInt64(provider);
		}

		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToUInt64(provider);
		}

		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToSingle(provider);
		}

		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToDouble(provider);
		}

		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToDecimal(provider);
		}

		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToDateTime(provider);
		}

		string IConvertible.ToString(IFormatProvider provider)
		{
			return Amount.ToString(provider);
		}

		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return ((IConvertible)Amount).ToType(conversionType, provider);
		}

		#endregion

		#region Convertion operators

		public static explicit operator Money(int value)
		{
			return new Money(value);
		}

		public static explicit operator Money(long value)
		{
			return new Money(value);
		}

		public static explicit operator Money(decimal value)
		{
			return new Money(value);
		}

		public static explicit operator Money(double value)
		{
			return new Money(value);
		}

		public static implicit operator decimal(Money value)
		{
			return value.Amount;
		}

		public static explicit operator int(Money value)
		{
			return checked((int)(value._value / value.Currency.Multiplier));
		}

		public static explicit operator long(Money value)
		{
			return value._value / value.Currency.Multiplier;
		}

		public static explicit operator double(Money value)
		{
			return (double)value._value / value.Currency.Multiplier;
		}

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
				throw new ArgumentOutOfRangeException(nameof(right), right, null);
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
				throw new ArgumentOutOfRangeException(nameof(right), right, null);
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

		#endregion
	}

	/// <summary>
	/// Represents a currency
	/// </summary>
	public sealed class Currency: IEquatable<Currency>
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

		private Currency(string code, int precision, string symbol = null)
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
		public static Currency Find(string code)
		{
			return code == null ? null: (Currency)__mapByCode[code.ToUpperInvariant()];
		}

		/// <summary>
		/// Creates a new currency or returns an exising one.
		/// </summary>
		/// <param name="code">Three-letters ISO 4217 currency code (i.e "EUR").</param>
		/// <param name="precision">Number of dignificant digits after the decimal point in range from 0 to 9.</param>
		/// <param name="symbol">Currency symbol (i.e "€").</param>
		/// <returns>A currency</returns>
		public static Currency Create(string code, int precision, string symbol = null)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));

			code = code.ToUpperInvariant();
			Currency result = null;

			Hashtable hash;
			Hashtable temp;
			do
			{
				hash = __mapByCode;
				var obj = __mapByCode[code];
				if (obj != null)
					return (Currency)obj;
				if (result == null)
					result = new Currency(code, precision, symbol);
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

		public override bool Equals(object obj)
		{
			return Equals(obj as Currency);
		}

		public bool Equals(Currency other)
		{
			return other != null && other.Code == Code;
		}

		public override int GetHashCode()
		{
			return Code.GetHashCode();
		}
	}
}
