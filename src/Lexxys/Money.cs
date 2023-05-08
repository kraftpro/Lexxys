// Lexxys Infrastructural library.
// file: Money.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

#if NET7_0_OR_GREATER
using System.Numerics;
#endif

#pragma warning disable CA2225 // Operator overloads have named alternates
#pragma warning disable CA1051 // Do not declare visible instance fields

namespace Lexxys;

using Xml;

/// <summary>
/// Represents a money value.
/// </summary>
[Serializable]
public readonly struct Money:
#if NET7_0_OR_GREATER
	ISignedNumber<Money>, INumber<Money>,
#endif
#if NET6_0_OR_GREATER
	ISpanFormattable,
#else
	IFormattable,
#endif
	IComparable, IComparable<Money>, IConvertible, IEquatable<Money>, ISerializable, IDumpValue, IDumpXml, IDumpJson
{
	private readonly long _value;
	private readonly Currency _currency;

	/// <summary>
	/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
	/// </summary>
	/// <param name="value">Amount value</param>
	/// <param name="currency">The money currency (when omitted <see cref="Currency"/>.ApplicationDefault will be used).</param>
	public Money(int value, Currency? currency = null)
	{
		_currency = currency ?? Currency.ApplicationDefault;
		_value = (long)value * _currency.Multiplier;
	}

	/// <summary>
	/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
	/// </summary>
	/// <param name="value">Amount value</param>
	/// <param name="currency">The money currency (when omitted <see cref="Currency"/>.ApplicationDefault will be used).</param>
	public Money(long value, Currency? currency = null)
	{
		_currency = currency ?? Currency.ApplicationDefault;
		_value = checked(value * _currency.Multiplier);
	}

	/// <summary>
	/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
	/// </summary>
	/// <param name="value">Amount value</param>
	/// <param name="currency">The money currency (when omitted <see cref="Currency"/>.ApplicationDefault will be used).</param>
	public Money(decimal value, Currency? currency = null)
	{
		_currency = currency ?? Currency.ApplicationDefault;
		var v = value * _currency.Multiplier;
		if (v is < long.MinValue or > long.MaxValue)
			throw new OverflowException($"The computed value {v} overflows of the valid range.")
				.Add(nameof(value), value);
		_value = (long)v;
	}

	/// <summary>
	/// Creates a new instance of <see cref="Money"/> for specified <paramref name="value"/> and <paramref name="currency"/>.
	/// </summary>
	/// <param name="value">Amount value</param>
	/// <param name="currency">The money currency (when omitted <see cref="Currency"/>.ApplicationDefault will be used).</param>
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
			_currency = i >= 0 ? __internalCurrencies[i]: Currency.Create(code, info.GetByte("prc"), info.GetString("sym"));
		}
	}

	/// <summary>
	/// Creates a new instance of <see cref="Money"/> for specified amount in cents and currency.
	/// </summary>
	/// <param name="minorValue">The amount value in cents.</param>
	/// <param name="currency">The money currency (when omitted <see cref="Currency"/>.ApplicationDefault will be used).</param>
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
	/// <summary>
	/// Amount value.
	/// </summary>
	public decimal Amount => (decimal)_value / Currency.Multiplier;
	/// <summary>
	/// Used currency.
	/// </summary>
	public Currency Currency => _currency ?? Currency.ApplicationDefault;

	/// <summary>
	/// Splits the <see cref="Money"/> value into two baskets according to the the specified <paramref name="ratio"/>.
	/// </summary>
	/// <param name="ratio">The ratio is used to splint the <see cref="Money"/> value.</param>
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
		if (value is < long.MinValue or > long.MaxValue)
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
	/// <param name="count">Number of baskets.</param>
	public Money[] Distribute(int count)
	{
		switch (count)
		{
			case < 0: throw new ArgumentOutOfRangeException(nameof(count), count, null);
			case 0: return Array.Empty<Money>();
			case 1: return new[] { this };
		}

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

	private static bool IsInRange(double value) => !double.IsNaN(value) && value is >= long.MinValue and <= long.MaxValue;

	#region Object, IFormattable, IComparable, IComparable<Money>, IEquatable<Money>

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		return (obj is Money money) && Equals(money);
	}

	/// <inheritdoc />
	public bool Equals(Money other)
	{
		return Object.ReferenceEquals(Currency, other.Currency) && _value == other._value;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return HashCode.Join(_value.GetHashCode(), Currency.GetHashCode());
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Amount.ToString("N" + Currency.Precision, CultureInfo.InvariantCulture) + " " + Currency.Code;
	}

	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider = null)
	{
		return Amount.ToString(format, formatProvider)
			.Replace("s", Currency.Symbol)
			.Replace("S", Currency.Code);
	}

	/// <inheritdoc />
	public int CompareTo(Money other)
	{
		if (Currency == other.Currency)
			return Amount.CompareTo(other.Amount);
		throw new ArgumentException(SR.DifferentCurrencyCodes(Currency, other.Currency));
	}

	/// <inheritdoc />
	public int CompareTo(object? obj)
	{
		if (obj is Money money)
			return CompareTo(money);
		throw new ArgumentOutOfRangeException(nameof(obj));
	}

	/// <inheritdoc />
	public DumpWriter DumpContent(DumpWriter writer)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));
		return writer.Dump(Amount).Text('[').Text(Currency.Code).Text(']');
	}

	string IDumpXml.XmlElementName => "money";

	private static readonly Money __negOne = new Money(-1);
	private static readonly Money __one = new Money(1);
	private static readonly Money __zero = new Money(0);

	/// <summary>
	/// Value negative one in the default <see cref="Currency"/>.
	/// </summary>
	public static Money NegativeOne => __negOne;
	/// <summary>
	/// Value one in the default <see cref="Currency"/>.
	/// </summary>
	public static Money One => __one;
	/// <summary>
	/// Radix of the <see cref="Money"/>.
	/// </summary>
	public static int Radix => 10;
	/// <summary>
	/// Value zero in the default <see cref="Currency"/>.
	/// </summary>
	public static Money Zero => __zero;
	/// <summary>
	/// Additive identity of <see cref="Money"/> in the default <see cref="Currency"/>
	/// </summary>
	public static Money AdditiveIdentity => __zero;
	/// <summary>
	/// Multiplicative identity of <see cref="Money"/> in the default <see cref="Currency"/>
	/// </summary>
	public static Money MultiplicativeIdentity => __one;

	#region INumber

	/// <summary>
	/// Returns the absolute value of a <see cref="Money"/> number.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static Money Abs(Money value)
	{
		return Create(Math.Abs(value._value), value._currency);
	}

	/// <summary>
	/// Returns a <see cref="Money"/> number in the specified range.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static Money Clamp(Money value, Money min, Money max)
	{
		if (min > max)
			throw new ArgumentException(SR.ValueCannotBeGreaterThan(min, max));
		if (value.Currency != min.Currency)
			throw new ArgumentException(SR.DifferentCurrencyCodes(value.Currency, min.Currency));
		if (value.Currency != max.Currency)
			throw new ArgumentException(SR.DifferentCurrencyCodes(value.Currency, max.Currency));
		return value < min ? min : value > max ? max : value;
	}

	/// <summary>
	/// Return a quotient and remainder of the specified pair of values.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static (Money Quotient, Money Remainder) DivRem(Money left, decimal right)
	{
		return (left / right, left % right);
	}

	/// <summary>
	/// Return a quotient and remainder of the specified pair of values.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static (Money Quotient, Money Remainder) DivRem(Money left, long right)
	{
		return (left / right, left % right);
	}

	public static Money Max(Money x, Money y)
	{
		if (x.Currency != y.Currency)
			throw new ArgumentException(SR.DifferentCurrencyCodes(x.Currency, y.Currency));
		return x._value < y._value ? y : x;
	}

	public static Money Min(Money x, Money y)
	{
		if (x.Currency != y.Currency)
			throw new ArgumentException(SR.DifferentCurrencyCodes(x.Currency, y.Currency));
		return x._value > y._value ? y : x;
	}

	public static Money Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s is null)
			throw new ArgumentNullException(nameof(s));
		if (!TryParse(s, style, provider, out var result))
			throw new FormatException(SR.CannotParseValue(s));
		return result;
	}

#if NET6_0_OR_GREATER
	/// <summary>
	/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
	/// </summary>
	/// <param name="s">The string representation of the number to convert.</param>
	/// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.</param>
	/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="s"/>.</param>
	/// <returns>Converted <see cref="Money"/> value.</returns>
	/// <exception cref="FormatException"></exception>
	public static Money Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
	{
		if (!TryParse(s, style, provider, out var result))
			throw new FormatException(SR.CannotParseValue(s.ToString()));
		return result;
	}

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
	/// <summary>
	/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
	/// </summary>
	/// <param name="s">The string representation of the number to convert.</param>
	/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="s"/>.</param>
	/// <returns>Converted <see cref="Money"/> value.</returns>
	/// <exception cref="FormatException"></exception>
	public static Money Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (!TryParse(s, provider, out var value))
			throw new FormatException(SR.CannotParseValue(s.ToString()));
		return value;
	}
#endif

	/// <summary>
	/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
	/// </summary>
	/// <param name="s">The string representation of the number to convert.</param>
	/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="s"/>.</param>
	/// <returns>Converted <see cref="Money"/> value.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="FormatException"></exception>
	public static Money Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
			throw new ArgumentNullException(nameof(s));
		if (!TryParse(s, provider, out var value))
			throw new FormatException(SR.CannotParseValue(s));
		return value;
	}

	/// <summary>
	/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
	/// </summary>
	/// <param name="s">The string representation of the number to convert.</param>
	/// <param name="result">The parsed <see cref="Money"/> value.</param>
	/// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
	public static bool TryParse(string s, out Money result)
	{
		return TryParse(s, NumberStyles.Currency, CultureInfo.CurrentCulture, out result);
	}

	/// <summary>
	/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
	/// </summary>
	/// <param name="s">The string representation of the number to convert.</param>
	/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="s"/>.</param>
	/// <param name="result">The parsed <see cref="Money"/> value.</param>
	/// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Money result)
	{
		return TryParse(s, NumberStyles.Currency, provider, out result);
	}

	/// <summary>
	/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
	/// </summary>
	/// <param name="s">The string representation of the number to convert.</param>
	/// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.</param>
	/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="s"/>.</param>
	/// <param name="result">The parsed <see cref="Money"/> value.</param>
	/// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
	public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out Money result)
	{
#if NET6_0_OR_GREATER
		if (s == null)
		{
			result = default;
			return false;
		}
		return TryParse(s.AsSpan(), style, provider, out result);
#else
		if ((s = s.TrimToNull()) == null)
		{
			result = default;
			return false;
		}
		Currency? currency = null;
		if (s.Length > 4 && s[s.Length - 4] == ' ')
		{
			var symbol = s.Substring(s.Length - 3);
			if (Char.IsLetter(symbol[0]) && Char.IsLetter(symbol[1]) && Char.IsLetter(symbol[2]))
			{
				currency = Currency.Find(symbol);
				if (currency == null)
				{
					result = default;
					return false;
				}
				s = s.Slice(0, s.Length - 4).TrimEnd();
			}
		}
		if (decimal.TryParse(s, style, provider, out var d))
		{
			result = new Money(d, currency);
			return true;
		}
		result = default;
		return false;
#endif
	}

#if NET6_0_OR_GREATER
	/// <summary>
	/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
	/// </summary>
	/// <param name="s">The string representation of the number to convert.</param>
	/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="s"/>.</param>
	/// <param name="result">The parsed <see cref="Money"/> value.</param>
	/// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Money result)
	{
		return TryParse(s, NumberStyles.Currency, provider, out result);
	}

	/// <summary>
	/// Converts the string representation of a number to its <see cref="Money"/> equivalent.
	/// </summary>
	/// <param name="s">The string representation of the number to convert.</param>
	/// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.</param>
	/// <param name="provider">An object that supplies culture-specific parsing information about <paramref name="s"/>.</param>
	/// <param name="result">The parsed <see cref="Money"/> value.</param>
	/// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Money result)
	{
		s = s.Trim();
		if (s.Length == 0)
		{
			result = default;
			return false;
		}
		Currency? currency = null;
		if (s.Length > 4 && s[s.Length - 4] == ' ')
		{
			var symbol = s.Slice(s.Length - 3);
			if (Char.IsLetter(symbol[0]) && Char.IsLetter(symbol[1]) && Char.IsLetter(symbol[2]))
			{
				currency = Currency.Find(symbol.ToString());
				if (currency == null)
				{
					result = new Money();
					return false;
				}
				s = s.Slice(0, s.Length - 5).TrimEnd();
			}
		}
		if (decimal.TryParse(s, style, provider, out var d))
		{
			result = new Money(d, currency);
			return true;
		}
		result = default;
		return false;
	}
#endif

	#endregion

	/// <inheritdoc />
	public XmlBuilder ToXmlContent(XmlBuilder builder)
	{
		if (builder is null)
			throw new ArgumentNullException(nameof(builder));
		return builder.InAttribute ? builder.Value(XmlTools.Convert(Amount) + " " + Currency.Code) :
			builder.Item("amount", Amount).Item("currency", Currency.Code);
	}

	/// <inheritdoc />
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
			info.AddValue("prc", (byte)_currency.Precision);
			info.AddValue("sym", _currency.Symbol);
		}
	}

	public static bool IsCanonical(Money value) => true;

	public static bool IsComplexNumber(Money value) => false;

	public static bool IsEvenInteger(Money value) => ((long)value & 1) == 0;

	public static bool IsFinite(Money value) => true;

	public static bool IsImaginaryNumber(Money value) => false;

	public static bool IsInfinity(Money value) => false;

	public static bool IsInteger(Money value) => value._value % value.Currency.Multiplier == 0;

	public static bool IsNaN(Money value) => false;

	public static bool IsNegative(Money value) => value._value < 0;

	public static bool IsNegativeInfinity(Money value) => false;

	public static bool IsNormal(Money value) => value._value != 0;

	public static bool IsOddInteger(Money value) => ((long)value & 1) == 1;

	public static bool IsPositive(Money value) => value._value >= 0;

	public static bool IsPositiveInfinity(Money value) => false;

	public static bool IsRealNumber(Money value) => true;

	public static bool IsSubnormal(Money value) => false;

	public static bool IsZero(Money value) => value._value == 0;

	public static Money MaxMagnitude(Money x, Money y)
	{
		if (x.Currency != y.Currency)
			throw new ArgumentException(SR.DifferentCurrencyCodes(x.Currency, y.Currency));

		long xv = Math.Abs(x._value);
		long yv = Math.Abs(y._value);
		return
			xv > yv ? x:
			yv > xv ? y:
			x >= 0 ? x: y;
	}

	public static Money MaxMagnitudeNumber(Money x, Money y) => MaxMagnitude(x, y);

	public static Money MinMagnitude(Money x, Money y)
	{
		if (x.Currency != y.Currency)
			throw new ArgumentException(SR.DifferentCurrencyCodes(x.Currency, y.Currency));

		long xv = Math.Abs(x._value);
		long yv = Math.Abs(y._value);
		return
			xv < yv ? x:
			yv > xv ? y:
			x <= 0 ? x: y;
	}

	public static Money MinMagnitudeNumber(Money x, Money y) => MinMagnitude(x, y);

#if NET7_0_OR_GREATER
	static bool INumberBase<Money>.TryConvertFromChecked<TOther>(TOther value, out Money result)
	{
		if (typeof(TOther) == typeof(Money))
		{
			result = (Money)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			result = new Money((ulong)checked((long)((decimal)(object)value * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			result = new Money((ulong)checked((long)((double)(object)value * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			result = new Money((ulong)checked((long)((float)(object)value * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			result = new Money((ulong)checked((long)((Half)(object)value * (Half)Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			result = new Money((ulong)checked((long)(object)value * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			result = new Money((ulong)checked((long)((Int128)(object)value * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = new Money((ulong)((int)(object)value * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			result = new Money((ulong)((short)(object)value * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = new Money((ulong)((sbyte)(object)value * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		result = Zero;
		return false;
	}

	static bool INumberBase<Money>.TryConvertFromSaturating<TOther>(TOther value, out Money result)
	{
		if (typeof(TOther) == typeof(Money))
		{
			result = (Money)(object)value;
			return true;
		}
		var c = Currency.ApplicationDefault;
		if (typeof(TOther) == typeof(decimal))
		{
			var nv = (decimal)(object)value;
			result = new Money((ulong)(nv > c.MaxValue ? long.MaxValue: nv < c.MinValue ? long.MinValue: (long)(nv * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			var dv = (double)(object)value;
			result = new Money((ulong)(dv > (double)c.MaxValue ? long.MaxValue : dv < (double)c.MinValue ? long.MinValue : (long)(dv * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			var fv = (float)(object)value;
			result = new Money((ulong)(fv > (float)c.MaxValue ? long.MaxValue : fv < (float)c.MinValue ? long.MinValue : (long)(fv * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			var hv = (Half)(object)value;
			result = new Money((ulong)(long)(hv * (Half)Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			var lv = (long)(object)value;
			var minInt = long.MinValue / c.Multiplier;
			var maxInt = long.MaxValue / c.Multiplier;
			result = new Money((ulong)(lv > maxInt ? long.MaxValue : lv < minInt ? long.MinValue : lv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			var xv = (Int128)(object)value;
			var minInt = long.MinValue / c.Multiplier;
			var maxInt = long.MaxValue / c.Multiplier;
			result = new Money((ulong)(xv > maxInt ? long.MaxValue : xv < minInt ? long.MinValue : (long)xv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			var iv = (int)(object)value;
			var minInt = long.MinValue / c.Multiplier;
			var maxInt = long.MaxValue / c.Multiplier;
			result = new Money((ulong)((long)iv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			var sv = (short)(object)value;
			var minInt = long.MinValue / c.Multiplier;
			var maxInt = long.MaxValue / c.Multiplier;
			result = new Money((ulong)((long)sv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			var bv = (sbyte)(object)value;
			var minInt = long.MinValue / c.Multiplier;
			var maxInt = long.MaxValue / c.Multiplier;
			result = new Money((ulong)((long)bv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		result = Zero;
		return false;
	}

	static bool INumberBase<Money>.TryConvertFromTruncating<TOther>(TOther value, out Money result)
	{
		if (typeof(TOther) == typeof(Money))
		{
			result = (Money)(object)value;
			return true;
		}
		var c = Currency.ApplicationDefault;
		if (typeof(TOther) == typeof(decimal))
		{
			var nv = (decimal)(object)value;
			result = new Money((ulong)(nv > c.MaxValue ? long.MaxValue : nv < c.MinValue ? long.MinValue : (long)(nv * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			var dv = (double)(object)value;
			result = new Money((ulong)(dv > (double)c.MaxValue ? long.MaxValue : dv < (double)c.MinValue ? long.MinValue : (long)(dv * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			var fv = (float)(object)value;
			result = new Money((ulong)(fv > (float)c.MaxValue ? long.MaxValue : fv < (float)c.MinValue ? long.MinValue : (long)(fv * Currency.ApplicationDefault.Multiplier)), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			var hv = (Half)(object)value;
			result = new Money((ulong)(long)(hv * (Half)Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			var lv = (long)(object)value;
			result = new Money((ulong)(lv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			var xv = (Int128)(object)value;
			result = new Money((ulong)(long)(xv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			var iv = (int)(object)value;
			result = new Money((ulong)((long)iv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			var sv = (short)(object)value;
			result = new Money((ulong)((long)sv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			var bv = (sbyte)(object)value;
			result = new Money((ulong)((long)bv * Currency.ApplicationDefault.Multiplier), Currency.ApplicationDefault);
			return true;
		}
		result = Zero;
		return false;
	}

	static bool INumberBase<Money>.TryConvertToChecked<TOther>(Money value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(Money))
		{
			result = (TOther)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			result = (TOther)(object)(decimal)value;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			result = (TOther)(object)(double)value;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			result = (TOther)(object)(float)value;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			result = (TOther)(object)checked((Half)(float)value);
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			result = (TOther)(object)(long)value;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			result = (TOther)(object)(Int128)(long)value;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)checked((int)(long)value);
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			result = (TOther)(object)checked((short)(long)value);
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = (TOther)(object)checked((sbyte)(long)value);
			return true;
		}
		result = default;
		return false;
	}

	static bool INumberBase<Money>.TryConvertToSaturating<TOther>(Money value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(Money))
		{
			result = (TOther)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			result = (TOther)(object)(decimal)value;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			result = (TOther)(object)(double)value;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			result = (TOther)(object)(float)value;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			result = (TOther)(object)(value > 65504 ? Half.MaxValue: value < -65504 ? Half.MinValue: (Half)(float)value);
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			result = (TOther)(object)(long)value;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			result = (TOther)(object)(Int128)(long)value;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)(value > int.MaxValue ? int.MaxValue: value < int.MinValue ? int.MinValue: (int)value);
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			result = (TOther)(object)(value > short.MaxValue ? short.MaxValue : value < short.MinValue ? short.MinValue : (short)value);
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = (TOther)(object)(value > sbyte.MaxValue ? sbyte.MaxValue : value < sbyte.MinValue ? sbyte.MinValue : (sbyte)value);
			return true;
		}
		result = default;
		return false;
	}

	static bool INumberBase<Money>.TryConvertToTruncating<TOther>(Money value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(Money))
		{
			result = (TOther)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			result = (TOther)(object)(decimal)value;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			result = (TOther)(object)(double)value;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			result = (TOther)(object)(float)value;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			result = (TOther)(object)(value > 65504 ? Half.MaxValue : value < -65504 ? Half.MinValue : (Half)(float)value);
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			result = (TOther)(object)(long)value;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			result = (TOther)(object)(Int128)(long)value;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)(int)value;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			result = (TOther)(object)(short)value;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = (TOther)(object)(sbyte)value;
			return true;
		}
		result = default;
		return false;
	}

#endif

	private static readonly Currency[] __internalCurrencies = { Currency.Empty, Currency.Usd, Currency.Eur, Currency.Rub, Currency.Uzs };

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

	public static bool operator ==(Money left, Money right) => left.Currency == right.Currency && left._value == right._value;

	public static bool operator !=(Money left, Money right) => left.Currency != right.Currency || left._value != right._value;

	public static bool operator >(Money left, Money right) => left.Currency == right.Currency && left._value > right._value;

	public static bool operator >=(Money left, Money right) => left.Currency == right.Currency && left._value >= right._value;

	public static bool operator <(Money left, Money right) => left.Currency == right.Currency && left._value < right._value;

	public static bool operator <=(Money left, Money right) => left.Currency == right.Currency && left._value <= right._value;

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
		if (left.Currency != right.Currency)
			if (left._value == 0)
				return right;
			else if (right._value == 0)
				return left;
			else
				throw new ArgumentException(SR.DifferentCurrencyCodes(left.Currency, right.Currency));
		return Create(checked(left._value + right._value), left._currency);
	}

	public static Money operator +(Money left, int right)
	{
		return Create(checked(left._value + (long)right * left.Currency.Multiplier), left._currency);
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
		if (left.Currency != right.Currency)
			if (left._value == 0)
				return right;
			else if (right._value == 0)
				return left;
			else
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

	public static Money operator -(int left, Money right)
	{
		return Create(checked((long)left * right.Currency.Multiplier - right._value), right._currency);
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

	public static Money operator /(Money left, Money right)
	{
		return left / right.Amount;
	}

	public static Money operator *(Money left, Money right)
	{
		return left * right.Amount;
	}

#if NET7_0_OR_GREATER

	static Money IDivisionOperators<Money, Money, Money>.operator /(Money left, Money right) => left / right.Amount;

	static Money IModulusOperators<Money, Money, Money>.operator %(Money left, Money right) => left % right.Amount;

	static Money IMultiplyOperators<Money, Money, Money>.operator *(Money left, Money right) => left * right.Amount;

	static Money INumber<Money>.Clamp(Money value, Money min, Money max) => value < min ? min : value > max ? max : value;

	static Money INumber<Money>.CopySign(Money value, Money sign) => (value._value & long.MinValue) == (sign._value & long.MinValue) ? value : -value;

	static Money INumber<Money>.Max(Money x, Money y) => x > y ? x : y;

	static Money INumber<Money>.MaxNumber(Money x, Money y) => x > y ? x : y;

	static Money INumber<Money>.Min(Money x, Money y) => x < y ? x : y;

	static Money INumber<Money>.MinNumber(Money x, Money y) => x < y ? x : y;

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
	/// Number of significant digits after the decimal point.
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
		if (precision is < 0 or > 9)
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
	public static Currency? Find(string? code)
	{
		return code == null ? null: (Currency?)__mapByCode[code.ToUpperInvariant()];
	}

	/// <summary>
	/// Creates a new currency or returns an exising one.
	/// </summary>
	/// <param name="code">Three-letters ISO 4217 currency code (i.e "EUR").</param>
	/// <param name="precision">Number of significant digits after the decimal point in range from 0 to 9.</param>
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
