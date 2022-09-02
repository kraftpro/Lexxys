// Lexxys Infrastructural library.
// file: VoteScore.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

#pragma warning disable CA2225 // Operator overloads have named alternates

namespace Lexxys
{
	public readonly struct VoteScore: IEquatable<VoteScore>, IComparable<VoteScore>
	{
		//private const float MaxScoreValue = 1.0f;
		//private const float ScoreValueEpsilon = 5.960465e-8f;
		private const int MaxScoreValue = 65536;
		private const int ScoreValueEpsilon = 1;
		public static readonly VoteScore MinValue = new VoteScore(0);
		public static readonly VoteScore MaxValue = new VoteScore(MaxScoreValue);

		private readonly int _value;

		private VoteScore(int value)
		{
			_value = value < 0 ? 0 : value > MaxScoreValue ? MaxScoreValue : value;
		}

		public double Value => (double) _value / MaxScoreValue;

		public VoteScore AndMore()
			=> _value + ScoreValueEpsilon >= MaxScoreValue ? this : new VoteScore(_value + ScoreValueEpsilon);

		public VoteScore AndMore(int multiplier)
			=> multiplier <= 0 ? this : new VoteScore(_value + multiplier * ScoreValueEpsilon > MaxScoreValue ? MaxScoreValue - ScoreValueEpsilon : _value + multiplier * ScoreValueEpsilon);

		public VoteScore AndLess()
			=> _value - ScoreValueEpsilon <= 0 ? this : new VoteScore(_value - ScoreValueEpsilon);

		public VoteScore AndLess(int multiplier)
			=> multiplier <= 0 ? this : new VoteScore(_value - multiplier * ScoreValueEpsilon <= 0 ? ScoreValueEpsilon : _value - multiplier * ScoreValueEpsilon);

		public static bool operator ==(VoteScore left, VoteScore right) => left._value == right._value;
		public static bool operator !=(VoteScore left, VoteScore right) => left._value != right._value;
		public static bool operator >(VoteScore left, VoteScore right) => left._value > right._value;
		public static bool operator >=(VoteScore left, VoteScore right) => left._value >= right._value;
		public static bool operator <(VoteScore left, VoteScore right) => left._value < right._value;
		public static bool operator <=(VoteScore left, VoteScore right) => left._value <= right._value;

		public static VoteScore operator +(VoteScore left, VoteScore right) => new VoteScore((left._value + right._value) / 2);

		public override bool Equals(object? obj) => obj is VoteScore score && _value == score._value;

		public bool Equals(VoteScore other) => _value == other._value;

		public override int GetHashCode() => _value.GetHashCode();

		public override string ToString() => Value.ToString();

		public int CompareTo(VoteScore other) => _value.CompareTo(other._value);

		public static readonly VoteScore Yes = new VoteScore(MaxScoreValue);
		public static readonly VoteScore No = new VoteScore(0);

		public static readonly VoteScore AlmostYes = Yes.AndLess();
		public static readonly VoteScore AlmostNo = No.AndMore();

		public static readonly VoteScore Maybe = Yes + No;

		public static readonly VoteScore ProbablyYes = Maybe + Yes;
		public static readonly VoteScore ProbablyNot = Maybe + No;
	}
}
