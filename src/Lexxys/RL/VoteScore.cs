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

namespace Lexxys.RL
{
	public struct VoteScore
	{
		//private const float MaxScoreValue = 1.0f;
		//private const float ScoreValueEpsilon = 5.960465e-8f;
		private const int MaxScoreValue = 65536;
		private const int ScoreValueEpsilon = 1;
		public static readonly VoteScore MinValue = new VoteScore(0);
		public static readonly VoteScore MaxValue = new VoteScore(MaxScoreValue);

		private VoteScore(int value)
		{
			Value = value < 0 ? 0: value > MaxScoreValue ? MaxScoreValue: value;
		}

		public int Value { get; }

		[Pure]
		public VoteScore AndMore()
		{
			return Value + ScoreValueEpsilon >= MaxScoreValue ? this: new VoteScore(Value + ScoreValueEpsilon);
		}

		[Pure]
		public VoteScore AndMore(int multiplier)
		{
			return multiplier <= 0 ? this: new VoteScore(Value + multiplier * ScoreValueEpsilon > MaxScoreValue ? MaxScoreValue - ScoreValueEpsilon: Value + multiplier * ScoreValueEpsilon);
		}

		[Pure]
		public VoteScore AndLess()
		{
			return Value - ScoreValueEpsilon <= 0 ? this: new VoteScore(Value - ScoreValueEpsilon);
		}

		[Pure]
		public VoteScore AndLess(int multiplier)
		{
			return multiplier <= 0 ? this: new VoteScore(Value - multiplier * ScoreValueEpsilon <= 0 ? ScoreValueEpsilon: Value - multiplier * ScoreValueEpsilon);
		}

		public static bool operator ==(VoteScore left, VoteScore right)
		{
			return left.Value == right.Value;
		}
		public static bool operator !=(VoteScore left, VoteScore right)
		{
			return left.Value != right.Value;
		}
		public static bool operator >(VoteScore left, VoteScore right)
		{
			return left.Value > right.Value;
		}
		public static bool operator >=(VoteScore left, VoteScore right)
		{
			return left.Value >= right.Value;
		}
		public static bool operator <(VoteScore left, VoteScore right)
		{
			return left.Value < right.Value;
		}
		public static bool operator <=(VoteScore left, VoteScore right)
		{
			return left.Value <= right.Value;
		}

		public static VoteScore operator +(VoteScore left, VoteScore right)
		{
			return new VoteScore((left.Value + right.Value) / 2);
		}

		public static int Compare(VoteScore left, VoteScore right)
		{
			return left.Value < right.Value ? -1:
				left.Value > right.Value ? 1: 0;
		}

		public override bool Equals(object obj)
		{
			return obj is VoteScore score && Value == score.Value;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static readonly VoteScore Yes = new VoteScore(MaxScoreValue);
		public static readonly VoteScore No = new VoteScore(0);

		public static readonly VoteScore AlmostYes = Yes.AndLess();
		public static readonly VoteScore AlmostNo = No.AndMore();

		public static readonly VoteScore Maybe = Yes + No;

		public static readonly VoteScore ProbablyYes = Maybe + Yes;
		public static readonly VoteScore ProbablyNot = Maybe + No;
	}
}


