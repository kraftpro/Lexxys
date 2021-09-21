// Lexxys Infrastructural library.
// file: FlagSet.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace Lexxys
{
	public sealed class FlagSet: ISet<string>, IReadOnlySet<string>, IEquatable<FlagSet>
	{
		private static readonly char[] ItemDelimiter = { ';' };
		private const char VariantDelimiter = ':';

		private readonly SortedSet<string> _set;

		public FlagSet(IComparer<string> comparer = null)
		{
			_set = new SortedSet<string>(comparer ?? StringComparer.OrdinalIgnoreCase);
		}

		public FlagSet(FlagSet value)
		{
			_set = value is null ?
				new SortedSet<string>(StringComparer.OrdinalIgnoreCase):
				new SortedSet<string>(value._set, value._set.Comparer);
		}

		public FlagSet(string value, IComparer<string> comparer = null)
		{
			_set = value is null ?
				new SortedSet<string>(comparer ?? StringComparer.OrdinalIgnoreCase):
				new SortedSet<string>(Collect(value.Split(ItemDelimiter)), comparer ?? StringComparer.OrdinalIgnoreCase);
		}

		public static unsafe string Clean(string item)
		{
			if (item == null)
				return null;
			fixed (char* str = item)
			{
				char* p = str;
				char* t = str + item.Length;
				char c;
				do
				{
					if (p == t)
						return null;
					c = *p++;
				} while (!((c > '\u0020' && c < '\u007F' && c != ':' && c != ';') || (c > '\u00A0' && c < '\uD800')));
				--p;
				do
				{
					c = *--t;
				} while (!((c > '\u0020' && c < '\u007F' && c != ':' && c != ';') || (c > '\u00A0' && c < '\uD800')));
				++t;

				int w = Math.Min((int)(t - p), 4096);
				char* buffer = stackalloc char[w];
				char* q = buffer;
				int n = 0;
				bool colon = false;
				do
				{
					c = *p++;
					if (!((c > '\u0020' && c < '\u007F' && c != ';') || (c > '\u00A0' && c < '\uD800')))
						continue;
					if (c != ':')
						colon = false;
					else if (!colon)
						colon = true;
					else
						continue;
					++n;
					*q++ = c;
					if (n == w)
						break;
				} while (p != t);

				if (n == 0)
					return null;
				do
				{
					--q;
					if (*q != ':' && *q != ';')
						break;
					--n;
				} while (n > 0);
				return n == item.Length ? item : new string(buffer, 0, n);
			}
		}

		private static List<string> Collect(IEnumerable<string> values)
		{
			var bag = new List<string>();
			if (values == null)
				return bag;
			foreach (var item in values)
			{
				string value = Clean(item);
				if (value == null)
					continue;
				int k = value.IndexOf(VariantDelimiter);
				if (k < 0)
				{
					bag.Add(value);
					continue;
				}
				int j = 0;
				string vv = "";
				do
				{
					string v = value.Substring(j, k - j).Trim();
					if (v.Length > 0)
					{
						vv += v;
						bag.Add(vv);
						vv += ":";
					}
					j = k + 1;
					k = value.IndexOf(VariantDelimiter, j);
				} while (k > 0);
				string rest = value.Substring(j).Trim();
				if (rest.Length > 0)
					bag.Add(vv + rest);
			}
			return bag;
		}

		public static FlagSet operator +(FlagSet left, FlagSet right)
		{
			if (right is null || right.Count == 0)
				return left is null && right is null ? null: new FlagSet(left);
			if (left is null || left.Count == 0)
				return new FlagSet(right);
			var result = new FlagSet(left);
			result._set.UnionWith(right._set);
			return result;
		}

		public static FlagSet operator +(FlagSet left, string right)
		{
			if (String.IsNullOrEmpty(right))
				return left is null && right == null ? null: new FlagSet(left);
			return left is null || left.Count == 0 ?
				new FlagSet(right):
				new FlagSet(left) { right };
		}

		public static FlagSet operator -(FlagSet left, FlagSet right)
		{
			if (left is null)
				return null;
			var result = new FlagSet(left);
			if (right is null || right.Count == 0)
				return result;
			result._set.ExceptWith(right._set);
			return result;
		}

		public static FlagSet operator -(FlagSet left, string right)
		{
			if (left is null)
				return null;
			var result = new FlagSet(left);
			if (String.IsNullOrEmpty(right))
				return result;
			result.Remove(right);
			return result;
		}

		public static explicit operator FlagSet(string value)
		{
			return value == null ? null: new FlagSet(value);
		}

		public static explicit operator string (FlagSet value)
		{
			return value?.ToString();
		}

		public static bool operator == (FlagSet left, FlagSet right)
		{
			return right is null ? left is null: right.Equals(left);
		}

		public static bool operator !=(FlagSet left, FlagSet right)
		{
			return right is null ? left is not null : !right.Equals(left);
		}

		public override int GetHashCode()
		{
			return HashCode.Join(18775, _set);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as FlagSet);
		}

		public bool Equals(FlagSet value)
		{
			if (value is null)
				return false;

			if (_set.Count != value._set.Count)
				return false;
			if (_set.Count == 0)
				return true;

			using IEnumerator<string> a = _set.GetEnumerator();
			using IEnumerator<string> b = value._set.GetEnumerator();
			while (a.MoveNext() && b.MoveNext())
			{
				if (!String.Equals(a.Current, b.Current, StringComparison.OrdinalIgnoreCase))
					return false;
			}
			return true;
		}

		public override string ToString()
		{
			return ToString(true);
		}

		public string ToString(bool compact)
		{
			if (Count == 0)
				return "";
			if (!compact)
				return ";" + String.Join(";", _set) + ";";
			var text = new StringBuilder();
			string last = "";
			foreach (var item in _set.Reverse())
			{
				if (!last.StartsWith(item + ":", StringComparison.Ordinal))
					text.Insert(0, ";" + item);
				last = item;
			}
			return text.Append(';').ToString();
		}

		#region ISet<string>

		public bool Add(string item)
		{
			if (item == null)
				return false;
			int k = _set.Count;
			foreach (var s in Collect(item.Split(ItemDelimiter)))
			{
				_set.Add(s);
			}
			return k < _set.Count;
		}

		public bool Remove(string item)
		{
			if (item == null)
				return false;
			int k = _set.Count;
			foreach (var s in Collect(item.Split(ItemDelimiter)))
			{
				_set.Remove(s);
			}
			return k > _set.Count;
		}

		public void ExceptWith(IEnumerable<string> other)
		{
			_set.ExceptWith(other);
		}

		public void IntersectWith(IEnumerable<string> other)
		{
			_set.IntersectWith(other);
		}

		public bool IsProperSubsetOf(IEnumerable<string> other)
		{
			return _set.IsProperSubsetOf(other);
		}

		public bool IsProperSupersetOf(IEnumerable<string> other)
		{
			return _set.IsProperSupersetOf(other);
		}

		public bool IsSubsetOf(IEnumerable<string> other)
		{
			return _set.IsSubsetOf(other);
		}

		public bool IsSupersetOf(IEnumerable<string> other)
		{
			return _set.IsSupersetOf(other);
		}

		public bool Overlaps(IEnumerable<string> other)
		{
			return _set.Overlaps(other);
		}

		public bool SetEquals(IEnumerable<string> other)
		{
			return _set.SetEquals(other);
		}

		public void SymmetricExceptWith(IEnumerable<string> other)
		{
			_set.SymmetricExceptWith(other);
		}

		public void UnionWith(IEnumerable<string> other)
		{
			_set.UnionWith(other);
		}

		void ICollection<string>.Add(string item)
		{
			Add(item);
		}

		public void Clear()
		{
			_set.Clear();
		}

		public bool Contains(string item)
		{
			return _set.Contains(item);
		}

		public void CopyTo(string[] array, int arrayIndex)
		{
			_set.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _set.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public IEnumerator<string> GetEnumerator()
		{
			return _set.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _set.GetEnumerator();
		}

		#endregion
	}
}


