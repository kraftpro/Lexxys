// Lexxys Infrastructural library.
// file: StaticSet.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#if false
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Lexxys
{
	public class StaticHasherTest<T>
	{
		public Func<T, int> Hasher { get; set; }
		public string Body { get; set; }
		public int Count { get; set; }
	}

	public static class StaticSet
	{
		public static StaticSet<T> Create<T>(Func<T, int> hasher, IEqualityComparer<T> comparer, params T[] items)
		{
			StaticSet<T> qs = StaticSet<T>.TryCreate(hasher, comparer, items);
			if (qs == null)
				throw EX.Argument(SR.TLS_BadHashFunction(), "hasher");
			return qs;
		}
		public static StaticSet<T> Create<T>(Func<T, int> hasher, params T[] items)
		{
			return Create<T>(hasher, null, items);
		}
		public static StaticSet<T> Create<T>(params T[] items)
		{
			return Create<T>(null, null, items);
		}
		public static StaticSet<T> TryCreate<T>(Func<T, int> hasher, IEqualityComparer<T> comparer, params T[] items)
		{
			return StaticSet<T>.TryCreate(hasher, comparer, items);
		}
		public static StaticSet<T> TryCreate<T>(Func<T, int> hasher, params T[] items)
		{
			return StaticSet<T>.TryCreate(hasher, null, items);
		}
		public static StaticSet<T> TryCreate<T>(params T[] items)
		{
			return StaticSet<T>.TryCreate(null, null, items);
		}

		public static StaticHasherTest<T> FindHashFunction<T>(StaticSet<T> set)
		{
			if (set == null)
				throw EX.ArgumentNull("set");
			return FindHashFunction<T>(set.Original);
		}

		public static StaticHasherTest<T> FindHashFunction<T>(params T[] items)
		{
			int len = Int32.MaxValue;
			int k1 = -1;
			for (int i = 0; i < 17; ++i)
			{
				StaticSet<T> at = StaticSet.TryCreate(x => x.GetHashCode() >> i, items);
				if (at != null && at.Length < len)
				{
					len = at.Length;
					k1 = i;
				}
			}

			int k2 = -1;
			for (int i = 1; i < 17; ++i)
			{
				StaticSet<T> at = StaticSet.TryCreate(x => x.GetHashCode() ^ (x.GetHashCode() >> i), items);
				if (at != null && at.Length < len)
				{
					len = at.Length;
					k2 = i;
				}
			}

			int k3 = -1;
			for (int i = 0; i < 17; ++i)
			{
				StaticSet<T> at = StaticSet.TryCreate(x => x.GetHashCode() + (x.GetHashCode() >> i), items);
				if (at != null && at.Length < len)
				{
					len = at.Length;
					k3 = i;
				}
			}

			int k4 = -1;
			for (int i = 0; i < 17; ++i)
			{
				StaticSet<T> at = StaticSet.TryCreate(x => x.GetHashCode() ^ (x.GetHashCode() + (x.GetHashCode() >> i)), items);
				if (at != null && at.Length < len)
				{
					len = at.Length;
					k4 = i;
				}
			}
			if (k4 > 0)
				return new StaticHasherTest<T>()
				{
					Hasher = delegate(T x) { int h = x.GetHashCode(); return h ^ (h + (h >> k4)); },
					Body = "delegate(" + typeof(T).ToString() + " x){ int h = x.GetHashCode(); return h ^ (h + (h >> " + k4.ToString(CultureInfo.InvariantCulture) + "));}",
					Count = len
				};
			if (k4 == 0)
				return new StaticHasherTest<T>()
				{
					Hasher = delegate(T x) { int h = x.GetHashCode(); return h ^ (h + h); },
					Body = "delegate(" + typeof(T).ToString() + " x){ int h = x.GetHashCode(); return h ^ (h + h); }",
					Count = len
				};
			if (k3 > 0)
				return new StaticHasherTest<T>()
				{
					Hasher = delegate(T x) { int h = x.GetHashCode(); return h + (h >> k3); },
					Body = "delegate(" + typeof(T).ToString() + " x){ int h = x.GetHashCode(); return h + (h >> " + k3.ToString(CultureInfo.InvariantCulture) + ");}",
					Count = len
				};
			if (k3 == 0)
				return new StaticHasherTest<T>()
				{
					Hasher = delegate(T x) { return x.GetHashCode() * 2; },
					Body = "delegate(" + typeof(T).ToString() + " x){ return x.GetHashCode() * 2; }",
					Count = len
				};
			if (k2 >= 0)
				return new StaticHasherTest<T>()
				{
					Hasher = delegate(T x) { int h = x.GetHashCode(); return h ^ (h >> k2); },
					Body = "delegate(" + typeof(T).ToString() + " x){ int h = x.GetHashCode(); return h ^ (h >> " + k2.ToString(CultureInfo.InvariantCulture) + "); }",
					Count = len
				};
			if (k1 > 0)
				return new StaticHasherTest<T>()
				{
					Hasher = delegate(T x) { return x.GetHashCode() >> k1;},
					Body = "delegate(T x) { return x.GetHashCode() >> " + k1.ToString(CultureInfo.InvariantCulture) + "; }",
					Count = len
				};
			if (k1 == 0)
				return new StaticHasherTest<T>()
				{
					Hasher = delegate(T x) { return x.GetHashCode(); },
					Body = "delegate(T x) { return x.GetHashCode(); }",
					Count = len
				};
			return null;
		}
	}

	public class StaticSet<T>
	{
		private T[] _items;
		private T[] _original;
		private IEqualityComparer<T> _comparer;
		private Func<T, int> _hasher;
		private int _shift;
		private int _bound;

		private static Func<T, int> _defaultHasher = delegate(T item)
			{
				int h = item.GetHashCode();
				return h ^ (h >> 8);
			};

		internal int Length { get { return _items.Length; } }
		internal T[] Original { get { return _original; } }

		private StaticSet(T[] original)
		{
			_original = original;
		}

		public bool Contains(T item)
		{
			int i = ((_hasher(item) & Int32.MaxValue) % _bound) - _shift;
			return  i >= 0 && i < _items.Length && _comparer.Equals(_items[(_hasher(item) & Int32.MaxValue) % _bound - _shift], item);
		}


		internal static StaticSet<T> TryCreate(Func<T, int> hasher, IEqualityComparer<T> comparer, params T[] items)
		{
			if (items == null)
				throw EX.ArgumentNull("items");

			if (hasher == null)
			{
				StaticHasherTest<T> h = StaticSet.FindHashFunction(items);
				if (h == null)
					throw EX.ArgumentNull("hasher");
				hasher = h.Hasher;
			}

			StaticSet<T> qs = new StaticSet<T>(items);

			qs._comparer = comparer ?? EqualityComparer<T>.Default;
			qs._hasher = hasher ?? _defaultHasher;

			int k = Array.BinarySearch(_primes, items.Length);
			if (k < 0)
			{
				k = ~k;
				if (k >= _primes.Length)
					throw EX.ArgumentOutOfRange("items");
			}

			int[] hash = new int[items.Length];
			for (int i = 0; i < items.Length; ++i)
			{
				hash[i] = qs._hasher(items[i]) & Int32.MaxValue;
			}

			for (int i = k; i < _primes.Length; ++i)
			{
				int n = _primes[i];
				int min = int.MaxValue;
				int max = 0;
				BitArray bits = new BitArray(n);
				for (int j = 0; j < hash.Length; ++j)
				{
					int h = hash[j] % n;
					if (bits[h])
						goto nextPrime;
					bits[h] = true;
					if (min > h)
						min = h;
					if (max < h)
						max = h;
				}

				qs._bound = n;
				qs._shift = min;
				qs._items = new T[max - min + 1];
				for (int j = 0; j < items.Length; ++j)
				{
					qs._items[hash[j] % n - min] = items[j];
				}
				return qs;

			nextPrime: ;
			}
			return null;
		}

		private static readonly int[] _primes = new int[] { 3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919 };
	}
}
#endif


