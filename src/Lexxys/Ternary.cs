// Lexxys Infrastructural library.
// file: Ternary.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys
{
	public struct Ternary: IEquatable<Ternary>
	{
		public static readonly Ternary True = new Ternary(true);
		public static readonly Ternary False = new Ternary(false);
		public static readonly Ternary Unknown = new Ternary();
		public static readonly string TrueString = "True";
		public static readonly string FalseString = "False";
		public static readonly string UnknownString = "Unknown";

		private readonly byte _value;

		private const byte UnknownValue = 0;
		private const byte TrueValue = 1;
		private const byte FalseValue = 2;

		public Ternary(bool value)
		{
			_value = value ? TrueValue: FalseValue;
		}

		public Ternary(bool? value)
		{
			_value = value.HasValue ? value.Value ? TrueValue: FalseValue: UnknownValue;
		}

		public bool IsTrue => _value == TrueValue;

		public bool IsFalse => _value == FalseValue;

		public bool IsEmpty => _value == UnknownValue;

		public bool HasValue => _value != UnknownValue;

		public bool IsNotTrue => _value != TrueValue;

		public bool IsNotFalse => _value != FalseValue;

		public static bool operator true(Ternary value)
		{
			return value._value == TrueValue;
		}

		public static bool operator false(Ternary value)
		{
			return value._value == FalseValue;
		}

		public static implicit operator bool?(Ternary value)
		{
			return value._value == FalseValue ? false:
				value._value == TrueValue ? true: (bool?)null;
		}

		public static implicit operator Ternary(bool? value)
		{
			return value.HasValue ? new Ternary(value.Value): Unknown;
		}

		public static implicit operator Ternary(bool value)
		{
			return value ? True: False;
		}

		public static Ternary operator |(Ternary left, Ternary right)
		{
			return left._value == TrueValue || right._value == TrueValue ? True:
		        left._value == UnknownValue || right._value == UnknownValue ? Unknown: False;
		}

		public static Ternary operator &(Ternary left, Ternary right)
		{
			return left._value == FalseValue || right._value == FalseValue ? False:
		        left._value == UnknownValue || right._value == UnknownValue ? Unknown: True;
		}

		public static Ternary operator ^(Ternary left, Ternary right)
		{
			return left._value == FalseValue ? right:
				right._value == FalseValue ? left:
				left._value == TrueValue && right._value == TrueValue ? False: Unknown;
		}

		public static Ternary operator !(Ternary value)
		{
			return value.IsEmpty ? Unknown: value.IsTrue ? False: True;
		}

		public static bool operator ==(Ternary left, Ternary right)
		{
			return left._value == right._value;
		}

		public static bool operator !=(Ternary left, Ternary right)
		{
			return left._value != right._value;
		}

		public override bool Equals(object obj)
		{
			return obj switch
			{
				null => _value == UnknownValue,
				Ternary ternary => _value == ternary._value,
				_ => false
			};
		}

		public bool Equals(Ternary other)
		{
			return _value == other._value;
		}

		public override int GetHashCode()
		{
			return _value;
		}

		public override string ToString()
		{
			return _value == TrueValue ? TrueString:
				_value == FalseValue ? FalseString: UnknownString;
		}

		public static Ternary Parse(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (value == TrueString)
				return Ternary.True;
			if (value == FalseString)
				return Ternary.False;
			if (value == UnknownString)
				return Ternary.Unknown;

			throw new FormatException(SR.TRN_BadFormat());
		}

		public static bool TryParse(string value, out Ternary result)
		{
			if (value == TrueString)
			{
				result = Ternary.True;
				return true;
			}
			if (value == FalseString)
			{
				result = Ternary.False;
				return true;
			}
			if (value == UnknownString)
			{
				result = Ternary.Unknown;
				return true;
			}
			result = default;
			return false;
		}

		public static Ternary TrueOrUnknown(bool value)
		{
			return value ? True: Unknown;
		}

		public static Ternary FalseOrUnknown(bool value)
		{
			return value ? Unknown: False;
		}
	}
}
