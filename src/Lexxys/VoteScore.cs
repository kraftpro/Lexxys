// Lexxys Infrastructural library.
// file: VoteScore.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using System.Globalization;

namespace Lexxys;

using ScoreType = float;

[Serializable]
public readonly struct VoteScore: IEquatable<VoteScore>, IComparable<VoteScore>
{
	// float
	private const ScoreType MaxScoreValue = 1.0f;
	private const ScoreType ScoreValueEpsilon = 2.9802326E-08f; // the smallest value, so MaxValue - Epsilon is not equal to MaxValue
	// int
	//private const ValueType MaxScoreValue = 65536;
	//private const ValueType ScoreValueEpsilon = 1;

	public static readonly VoteScore MinValue = new VoteScore(0);
	public static readonly VoteScore MaxValue = new VoteScore(MaxScoreValue);

	public static readonly VoteScore Yes = new VoteScore(MaxScoreValue);
	public static readonly VoteScore No = new VoteScore(0);

	public static readonly VoteScore AlmostYes = Yes.AndLess();
	public static readonly VoteScore AlmostNo = No.AndMore();

	public static readonly VoteScore Perhaps = Yes + No;

	public static readonly VoteScore ProbablyYes = Perhaps + Yes;
	public static readonly VoteScore ProbablyNot = Perhaps + No;

	private readonly ScoreType _value;

	private VoteScore(ScoreType value)
	{
		_value = value < 0 ? 0: value > MaxScoreValue ? MaxScoreValue: value;
	}

	public double Value => (double) _value / MaxScoreValue;

	public VoteScore AndMore()
		=> _value >= MaxScoreValue - ScoreValueEpsilon ? this: new VoteScore(_value + ScoreValueEpsilon);

	public VoteScore AndMore(int multiplier)
	{
		if (multiplier <= 0 || _value >= MaxScoreValue - ScoreValueEpsilon)
			return this;
		var value = _value + multiplier * ScoreValueEpsilon;
		return new VoteScore(value >= MaxScoreValue ? MaxScoreValue - ScoreValueEpsilon: value);
	}

	public VoteScore AndLess()
		=> _value - ScoreValueEpsilon <= 0 ? this: new VoteScore(_value - ScoreValueEpsilon);

	public VoteScore AndLess(int multiplier)
	{
		if (multiplier <= 0 || _value <= ScoreValueEpsilon)
			return this;
		var value = _value - multiplier * ScoreValueEpsilon;
		return new VoteScore(value <= ScoreValueEpsilon ? ScoreValueEpsilon: value);
	}

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

	public override string ToString() => Value.ToString(CultureInfo.CurrentCulture);

	public int CompareTo(VoteScore other) => _value.CompareTo(other._value);
}
