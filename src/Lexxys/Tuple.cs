// Lexxys Infrastructural library.
// file: Tuple.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys
{
	public static class Tuple
	{
		public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
		{
			return new Tuple<T1, T2>(item1, item2);
		}

		public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
		{
			return new Tuple<T1, T2, T3>(item1, item2, item3);
		}
	}

	public class Tuple<T1, T2>: IComparable
	{
		public T1 Item1 { get; private set; }
		public T2 Item2 { get; private set; }

		public Tuple(T1 item1, T2 item2)
		{
			Item1 = item1;
			Item2 = item2;
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			Tuple<T1, T2> tuple = obj as Tuple<T1, T2>;
			if (tuple == null)
				throw EX.ArgumentWrongType("obj", obj.GetType());
			int r = Comparer<T1>.Default.Compare(this.Item1, tuple.Item1);
			if (r != 0)
				return r;
			return Comparer<T2>.Default.Compare(this.Item2, tuple.Item2);
		}

		public static readonly Comparer<Tuple<T1, T2>> Comparer = new TupleComparer();

		private class TupleComparer: Comparer<Tuple<T1, T2>>
		{
			public override int Compare(Tuple<T1, T2> x, Tuple<T1, T2> y)
			{
				int r = Comparer<T1>.Default.Compare(x.Item1, y.Item1);
				if (r != 0)
					return r;
				return Comparer<T2>.Default.Compare(x.Item2, y.Item2);
			}
		}
	}

	public class Tuple<T1, T2, T3>: IComparable
	{
		public T1 Item1 { get; private set; }
		public T2 Item2 { get; private set; }
		public T3 Item3 { get; private set; }

		public Tuple(T1 item1, T2 item2, T3 item3)
		{
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			Tuple<T1, T2, T3> tuple = obj as Tuple<T1, T2, T3>;
			if (tuple == null)
				throw EX.ArgumentWrongType("obj", obj.GetType());
			int r = Comparer<T1>.Default.Compare(this.Item1, tuple.Item1);
			if (r != 0)
				return r;
			r = Comparer<T2>.Default.Compare(this.Item2, tuple.Item2);
			if (r != 0)
				return r;
			return Comparer<T3>.Default.Compare(this.Item3, tuple.Item3);
		}

		public static readonly Comparer<Tuple<T1, T2, T3>> Comparer = new TupleComparer();

		private class TupleComparer: Comparer<Tuple<T1, T2, T3>>
		{
			public override int Compare(Tuple<T1, T2> x, Tuple<T1, T2> y)
			{
				int r = Comparer<T1>.Default.Compare(x.Item1, y.Item1);
				if (r != 0)
					return r;
				r = Comparer<T2>.Default.Compare(x.Item2, y.Item2);
				if (r != 0)
					return r;
				return Comparer<T3>.Default.Compare(x.Item3, y.Item3);
			}
		}
	}
}

#endif

