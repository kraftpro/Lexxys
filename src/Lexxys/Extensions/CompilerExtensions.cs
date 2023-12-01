﻿#if !NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

//namespace Lexxys
//{
//	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
//	internal sealed class NotNullWhenAttribute: Attribute
//	{
//		public bool ReturnValue { get; }

//		public NotNullWhenAttribute(bool returnValue)
//		{
//			ReturnValue = returnValue;
//		}
//	}

//	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
//	internal sealed class MaybeNullWhenAttribute: Attribute
//	{
//		public bool ReturnValue { get; }

//		public MaybeNullWhenAttribute(bool returnValue)
//		{
//			ReturnValue = returnValue;
//		}
//	}

//	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
//	internal sealed class NotNullIfNotNullAttribute: Attribute
//	{
//		public string ParameterName { get; }

//		public NotNullIfNotNullAttribute(string parameterName)
//		{
//			ParameterName = parameterName;
//		}
//	}

//	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
//	internal sealed class MaybeNullAttribute: Attribute
//	{
//	}
//}

namespace System.Runtime.CompilerServices
{
	internal class IsExternalInit
	{
	}
}

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
	internal sealed class NotNullAttribute: Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	internal sealed class MaybeNullAttribute: Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
	internal sealed class DoesNotReturnIfAttribute: Attribute
	{
		public bool ParameterValue { get; }

		public DoesNotReturnIfAttribute(bool parameterValue)
		{
			ParameterValue = parameterValue;
		}
	}

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
	internal sealed class NotNullWhenAttribute: Attribute
	{
		public bool ReturnValue { get; }

		public NotNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}
	}

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	internal sealed class NotNullIfNotNullAttribute: Attribute
	{
		public string ParameterName { get; }

		public NotNullIfNotNullAttribute(string parameterName)
		{
			ParameterName = parameterName;
		}
	}

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
	internal sealed class MaybeNullWhenAttribute: Attribute
	{
		public bool ReturnValue { get; }

		public MaybeNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}
	}

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
	internal sealed class DoesNotReturnAttribute: Attribute
	{
	}
}

namespace System.Runtime.InteropServices
{
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class OutAttribute: Attribute
	{
	}
}

namespace System
{
	/// <summary>Represent a type can be used to index a collection either from the start or the end.</summary>
	/// <remarks>
	/// Index is used by the C# compiler to support the new index syntax
	/// <code>
	/// int[] someArray = new int[5] { 1, 2, 3, 4, 5 } ;
	/// int lastElement = someArray[^1]; // lastElement = 5
	/// </code>
	/// </remarks>
#if SYSTEM_PRIVATE_CORELIB
    public
#else
	internal
#endif
	readonly struct Index: IEquatable<Index>
	{
		private readonly int _value;

		/// <summary>Construct an Index using a value and indicating if the index is from the start or from the end.</summary>
		/// <param name="value">The index value. it has to be zero or positive number.</param>
		/// <param name="fromEnd">Indicating if the index is from the start or from the end.</param>
		/// <remarks>
		/// If the Index constructed from the end, index value 1 means pointing at the last element and index value 0 means pointing at beyond last element.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Index(int value, bool fromEnd = false)
		{
			if (value < 0)
			{
				ThrowValueArgumentOutOfRange_NeedNonNegNumException();
			}

			if (fromEnd)
				_value = ~value;
			else
				_value = value;
		}

		// The following private constructors mainly created for perf reason to avoid the checks
		private Index(int value)
		{
			_value = value;
		}

		/// <summary>Create an Index pointing at first element.</summary>
		public static Index Start => new Index(0);

		/// <summary>Create an Index pointing at beyond last element.</summary>
		public static Index End => new Index(~0);

		/// <summary>Create an Index from the start at the position indicated by the value.</summary>
		/// <param name="value">The index value from the start.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Index FromStart(int value)
		{
			if (value < 0)
			{
				ThrowValueArgumentOutOfRange_NeedNonNegNumException();
			}

			return new Index(value);
		}

		/// <summary>Create an Index from the end at the position indicated by the value.</summary>
		/// <param name="value">The index value from the end.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Index FromEnd(int value)
		{
			if (value < 0)
			{
				ThrowValueArgumentOutOfRange_NeedNonNegNumException();
			}

			return new Index(~value);
		}

		/// <summary>Returns the index value.</summary>
		public int Value
		{
			get
			{
				if (_value < 0)
					return ~_value;
				else
					return _value;
			}
		}

		/// <summary>Indicates whether the index is from the start or the end.</summary>
		public bool IsFromEnd => _value < 0;

		/// <summary>Calculate the offset from the start using the giving collection length.</summary>
		/// <param name="length">The length of the collection that the Index will be used with. length has to be a positive value</param>
		/// <remarks>
		/// For performance reason, we don't validate the input length parameter and the returned offset value against negative values.
		/// we don't validate either the returned offset is greater than the input length.
		/// It is expected Index will be used with collections which always have non negative length/count. If the returned offset is negative and
		/// then used to index a collection will get out of range exception which will be same affect as the validation.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetOffset(int length)
		{
			int offset = _value;
			if (IsFromEnd)
			{
				// offset = length - (~value)
				// offset = length + (~(~value) + 1)
				// offset = length + value + 1

				offset += length + 1;
			}
			return offset;
		}

		/// <summary>Indicates whether the current Index object is equal to another object of the same type.</summary>
		/// <param name="value">An object to compare with this object</param>
		public override bool Equals([NotNullWhen(true)] object? value) => value is Index && _value == ((Index)value)._value;

		/// <summary>Indicates whether the current Index object is equal to another Index object.</summary>
		/// <param name="other">An object to compare with this object</param>
		public bool Equals(Index other) => _value == other._value;

		/// <summary>Returns the hash code for this instance.</summary>
		public override int GetHashCode() => _value;

		/// <summary>Converts integer number to an Index.</summary>
		public static implicit operator Index(int value) => FromStart(value);

		/// <summary>Converts the value of the current Index object to its equivalent string representation.</summary>
		public override string ToString()
		{
			if (IsFromEnd)
				return ToStringFromEnd();

			return ((uint)Value).ToString();
		}

		private static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
		{
#if SYSTEM_PRIVATE_CORELIB
            throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedNonNegNum);
#else
			throw new ArgumentOutOfRangeException("value", "value must be non-negative");
#endif
		}

		private string ToStringFromEnd()
		{
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
            Span<char> span = stackalloc char[11]; // 1 for ^ and 10 for longest possible uint value
            bool formatted = ((uint)Value).TryFormat(span.Slice(1), out int charsWritten);
            Debug.Assert(formatted);
            span[0] = '^';
            return new string(span.Slice(0, charsWritten + 1));
#else
			return '^' + Value.ToString();
#endif
		}
	}

	/// <summary>Represent a range has start and end indexes.</summary>
	/// <remarks>
	/// Range is used by the C# compiler to support the range syntax.
	/// <code>
	/// int[] someArray = new int[5] { 1, 2, 3, 4, 5 };
	/// int[] subArray1 = someArray[0..2]; // { 1, 2 }
	/// int[] subArray2 = someArray[1..^0]; // { 2, 3, 4, 5 }
	/// </code>
	/// </remarks>
#if SYSTEM_PRIVATE_CORELIB
    public
#else
	internal
#endif
	readonly struct Range: IEquatable<Range>
	{
		/// <summary>Represent the inclusive start index of the Range.</summary>
		public Index Start { get; }

		/// <summary>Represent the exclusive end index of the Range.</summary>
		public Index End { get; }

		/// <summary>Construct a Range object using the start and end indexes.</summary>
		/// <param name="start">Represent the inclusive start index of the range.</param>
		/// <param name="end">Represent the exclusive end index of the range.</param>
		public Range(Index start, Index end)
		{
			Start = start;
			End = end;
		}

		/// <summary>Indicates whether the current Range object is equal to another object of the same type.</summary>
		/// <param name="value">An object to compare with this object</param>
		public override bool Equals([NotNullWhen(true)] object? value) =>
			value is Range r &&
			r.Start.Equals(Start) &&
			r.End.Equals(End);

		/// <summary>Indicates whether the current Range object is equal to another Range object.</summary>
		/// <param name="other">An object to compare with this object</param>
		public bool Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);

		/// <summary>Returns the hash code for this instance.</summary>
		public override int GetHashCode()
		{
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
            return HashCode.Combine(Start.GetHashCode(), End.GetHashCode());
#else
			return Lexxys.HashCode.Join(Start.GetHashCode(), End.GetHashCode());
#endif
		}

		/// <summary>Converts the value of the current Range object to its equivalent string representation.</summary>
		public override string ToString()
		{
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
            Span<char> span = stackalloc char[2 + (2 * 11)]; // 2 for "..", then for each index 1 for '^' and 10 for longest possible uint
            int pos = 0;

            if (Start.IsFromEnd)
            {
                span[0] = '^';
                pos = 1;
            }
            bool formatted = ((uint)Start.Value).TryFormat(span.Slice(pos), out int charsWritten);
            Debug.Assert(formatted);
            pos += charsWritten;

            span[pos++] = '.';
            span[pos++] = '.';

            if (End.IsFromEnd)
            {
                span[pos++] = '^';
            }
            formatted = ((uint)End.Value).TryFormat(span.Slice(pos), out charsWritten);
            Debug.Assert(formatted);
            pos += charsWritten;

            return new string(span.Slice(0, pos));
#else
			return Start.ToString() + ".." + End.ToString();
#endif
		}

		/// <summary>Create a Range object starting from start index to the end of the collection.</summary>
		public static Range StartAt(Index start) => new Range(start, Index.End);

		/// <summary>Create a Range object starting from first element in the collection to the end Index.</summary>
		public static Range EndAt(Index end) => new Range(Index.Start, end);

		/// <summary>Create a Range object starting from first element to the end.</summary>
		public static Range All => new Range(Index.Start, Index.End);

		/// <summary>Calculate the start offset and length of range object using a collection length.</summary>
		/// <param name="length">The length of the collection that the range will be used with. length has to be a positive value.</param>
		/// <remarks>
		/// For performance reason, we don't validate the input length parameter against negative values.
		/// It is expected Range will be used with collections which always have non negative length/count.
		/// We validate the range is inside the length scope though.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public (int Offset, int Length) GetOffsetAndLength(int length)
		{
			int start = Start.GetOffset(length);
			int end = End.GetOffset(length);

			if ((uint)end > (uint)length || (uint)start > (uint)end)
			{
				ThrowArgumentOutOfRangeException();
			}

			return (start, end - start);
		}

		private static void ThrowArgumentOutOfRangeException()
		{
			throw new ArgumentOutOfRangeException("length");
		}
	}
}


#endif