// Lexxys Infrastructural library.
// file: Vote.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.RL
{
	public static class Vote
	{
		public static Vote<T> New<T>(T value, VoteScore score)
		{
			return new Vote<T>(value, score);
		}

		public static Vote<T> Yes<T>(T value)
		{
			return new Vote<T>(value, VoteScore.Yes);
		}
		public static Vote<T> Yes<T>(T value, int andMoreOrLess)
		{
			return andMoreOrLess >= 0 ? new Vote<T>(value, VoteScore.Yes):
				new Vote<T>(value, VoteScore.Yes.AndLess(-andMoreOrLess));				
		}
		public static Vote<T> No<T>(T value)
		{
			return new Vote<T>(value, VoteScore.No);
		}
		public static Vote<T> No<T>(T value, int andMoreOrLess)
		{
			return andMoreOrLess <= 0 ? new Vote<T>(value, VoteScore.No):
				new Vote<T>(value, VoteScore.No.AndMore(andMoreOrLess));
		}
		public static Vote<T> AlmostYes<T>(T value)
		{
			return new Vote<T>(value, VoteScore.AlmostYes);
		}
		public static Vote<T> AlmostYes<T>(T value, int andMoreOrLess)
		{
			return andMoreOrLess >= 0 ? new Vote<T>(value, VoteScore.AlmostYes):
				new Vote<T>(value, VoteScore.AlmostYes.AndLess(-andMoreOrLess));
		}
		public static Vote<T> AlmostNo<T>(T value)
		{
			return new Vote<T>(value, VoteScore.AlmostNo);
		}
		public static Vote<T> AlmostNo<T>(T value, int andMoreOrLess)
		{
			return andMoreOrLess <= 0 ? new Vote<T>(value, VoteScore.AlmostNo):
				new Vote<T>(value, VoteScore.AlmostNo.AndMore(andMoreOrLess));
		}
		public static Vote<T> ProbablyYes<T>(T value)
		{
			return new Vote<T>(value, VoteScore.ProbablyYes);
		}
		public static Vote<T> ProbablyYes<T>(T value, int andMoreOrLess)
		{
			return andMoreOrLess < 0 ? new Vote<T>(value, VoteScore.ProbablyYes.AndLess(-andMoreOrLess)):
				new Vote<T>(value, VoteScore.ProbablyYes.AndMore(andMoreOrLess));
		}
		public static Vote<T> ProbablyNot<T>(T value)
		{
			return new Vote<T>(value, VoteScore.ProbablyNot);
		}
		public static Vote<T> ProbablyNot<T>(T value, int andMoreOrLess)
		{
			return andMoreOrLess < 0 ? new Vote<T>(value, VoteScore.ProbablyNot.AndLess(-andMoreOrLess)):
				new Vote<T>(value, VoteScore.ProbablyNot.AndMore(andMoreOrLess));
		}
		public static Vote<T> Maybe<T>(T value)
		{
			return new Vote<T>(value, VoteScore.Maybe);
		}
		public static Vote<T> Maybe<T>(T value, int andMoreOrLess)
		{
			return andMoreOrLess < 0 ? new Vote<T>(value, VoteScore.Maybe.AndLess(-andMoreOrLess)):
				new Vote<T>(value, VoteScore.Maybe.AndMore(andMoreOrLess));
		}
	}

	public struct Vote<T>: IEquatable<Vote<T>>
	{
		public static readonly Vote<T> Empty = new Vote<T>();

		public Vote(T value, VoteScore score)
		{
			Value = value;
			Score = score;
		}

		public T Value { get; }

		public VoteScore Score { get; }

		public override bool Equals(object obj) => obj is Vote<T> vote && Equals(vote);

		public bool Equals(Vote<T> other) => Score == other.Score && EqualityComparer<T>.Default.Equals(Value, other.Value);

		public override int GetHashCode() => HashCode.Join(Value?.GetHashCode() ?? 0, Score.GetHashCode());

		public static bool operator ==(Vote<T> left, Vote<T> right) => left.Equals(right);

		public static bool operator !=(Vote<T> left, Vote<T> right) => !(left == right);
	}
}
