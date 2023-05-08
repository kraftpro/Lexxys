// Lexxys Infrastructural library.
// file: FlagSet.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Buffers;
using System.Collections;
using System.Text;

#pragma warning disable CA2225 // Operator overloads have named alternates

namespace Lexxys;
public sealed class FlagSet: ISet<string>, IReadOnlySet<string>, IEquatable<FlagSet>
{
	private const char NameDelimiter = ':';
	private const char GroupDelimiter = ';';
	private const string NameDelimiterStr = ":";

	private readonly SortedSet<string> _set;

	public FlagSet(IComparer<string>? comparer = null)
	{
		_set = new SortedSet<string>(comparer ?? StringComparer.OrdinalIgnoreCase);
	}

	public FlagSet(FlagSet? value)
	{
		_set = value is null ?
			new SortedSet<string>(StringComparer.OrdinalIgnoreCase):
			new SortedSet<string>(value._set, value._set.Comparer);
	}

	public FlagSet(string? value, IComparer<string>? comparer = null)
	{
		_set = value is null ?
			new SortedSet<string>(comparer ?? StringComparer.OrdinalIgnoreCase):
			new SortedSet<string>(Split(value), comparer ?? StringComparer.OrdinalIgnoreCase);
	}

	public static string? Clean(string? item)
	{
		if (string.IsNullOrEmpty(item))
			return default;
		
		var s = item.AsSpan();
		int i = 0;
		while (IsBlankOrDelimiter(s[i]))
		{
			if (++i == s.Length)
				return default;
		}
		s = s.Slice(i);
		i = s.Length - 1;
		if (IsBlankOrDelimiter(s[i]))
		{
			while (IsBlankOrDelimiter(s[--i])) { }
			s = s.Slice(0, i + 1);
		}
		static bool IsBlankOrDelimiter(char c)
			=> IsBlank(c) || c is NameDelimiter or GroupDelimiter;
		static bool IsBlank(char c)
			=> c is <= '\u0020' or >= '\u007F' and <= '\u00A0' or >= '\uD800';

		using var mem = MemoryPool<char>.Shared.Rent(s.Length);
		var buf = mem.Memory.Span;
		int n = 0;
		bool colon = false;
		bool group = false;
		foreach(var c in s)
		{
			if (IsBlank(c)) continue;

			if (c == GroupDelimiter)
			{
				group = true;
				colon = false;
			}
			else if (c == NameDelimiter)
			{
				colon = !group;
			}
			else
			{
				if (group)
				{
					group = false;
					buf[n++] = GroupDelimiter;
				}
				else if (colon)
				{
					colon = false;
					buf[n++] = NameDelimiter;
				}
				buf[n++] = c;
			}
		}

		return buf.Slice(0, n).ToString();
	}

	private static IEnumerable<string> Split(string? value)
	{
		var s = Clean(value);
		if (s is null)
			yield break;

		int k = s.IndexOf(GroupDelimiter);
		if (k < 0)
		{
			int i = s.IndexOf(NameDelimiter);
			while (i > 0)
			{
				yield return s.Substring(0, i);
				i = s.IndexOf(NameDelimiter, i + 1);
			}
			yield return s;
			yield break;
		}

		int l = 0;
		for (;;)
		{
			int i = s.IndexOf(NameDelimiter, l);
			while (i > 0 && i < k)
			{
				yield return s.Substring(l, i - l);
				i = s.IndexOf(NameDelimiter, i + 1);
			}

			yield return s.Substring(l, k - l);
			if (k == s.Length)
				yield break;
			l = k + 1;
			k = s.IndexOf(GroupDelimiter, l);
			if (k < 0)
				k = s.Length;
		}
	}
	
	public static FlagSet? operator +(FlagSet? left, FlagSet? right)
	{
		if (right is null || right.Count == 0)
			return left is null && right is null ? null: new FlagSet(left);
		if (left is null || left.Count == 0)
			return new FlagSet(right);
		var result = new FlagSet(left);
		result._set.UnionWith(right._set);
		return result;
	}

	public static FlagSet? operator +(FlagSet? left, string? right)
	{
		if (String.IsNullOrEmpty(right))
			return left is null ? null: new FlagSet(left);
		return left is null || left.Count == 0 ?
			new FlagSet(right):
			new FlagSet(left) { right };
	}

	public static FlagSet? operator -(FlagSet? left, FlagSet? right)
	{
		if (left is null)
			return null;
		var result = new FlagSet(left);
		if (right is null || right.Count == 0)
			return result;
		result._set.ExceptWith(right._set);
		return result;
	}

	public static FlagSet? operator -(FlagSet? left, string? right)
	{
		if (left is null)
			return null;
		var result = new FlagSet(left);
		if (String.IsNullOrEmpty(right))
			return result;
		result.Remove(right);
		return result;
	}

	public static FlagSet? Parse(string? value) => value == null ? null : new FlagSet(value);

	public static explicit operator FlagSet?(string? value) => value == null ? null : new FlagSet(value);

	public static explicit operator string?(FlagSet? value) => value?.ToString();

	public static bool operator ==(FlagSet? left, FlagSet? right) => right?.Equals(left) ?? left is null;

	public static bool operator !=(FlagSet? left, FlagSet? right) => !right?.Equals(left) ?? left is not null;

	public override int GetHashCode() => HashCode.Join(_set.Count * 18775, _set);

	public override bool Equals(object? obj) => obj is FlagSet other && Equals(other);

	public bool Equals(FlagSet? other)
	{
		if (other is null)
			return false;
		if (ReferenceEquals(this, other))
			return true;

		if (_set.Count != other._set.Count)
			return false;
		if (_set.Count == 0)
			return true;

		using IEnumerator<string> a = _set.GetEnumerator();
		using IEnumerator<string> b = other._set.GetEnumerator();
		while (a.MoveNext() && b.MoveNext())
		{
			if (!String.Equals(a.Current, b.Current, StringComparison.OrdinalIgnoreCase))
				return false;
		}
		return true;
	}

	public override string ToString() => ToString(false);

	public string ToString(bool fast)
	{
		if (Count == 0)
			return "";

		var text = new StringBuilder();
		text.Append(GroupDelimiter);
		string? last = null;
		foreach (var item in _set)
		{
			if (last is not null && !item.StartsWith(last, StringComparison.Ordinal))
				text.Append(last).Append(GroupDelimiter);
			last = item + NameDelimiterStr;
		}
		return text.Append(last).Append(GroupDelimiter).ToString();
	}

	#region ISet<string>

	public bool Add(string? item)
	{
		int k = _set.Count;
		foreach (var s in Split(item))
		{
			_set.Add(s);
		}
		return k < _set.Count;
	}

	public bool Remove(string? item)
	{
		int k = _set.Count;
		foreach (var s in Split(item))
		{
			_set.Remove(s);
		}
		return k > _set.Count;
	}

	public void ExceptWith(IEnumerable<string> other) => _set.ExceptWith(other);

	public void IntersectWith(IEnumerable<string> other) => _set.IntersectWith(other);

	public bool IsProperSubsetOf(IEnumerable<string> other) => _set.IsProperSubsetOf(other);

	public bool IsProperSupersetOf(IEnumerable<string> other) => _set.IsProperSupersetOf(other);

	public bool IsSubsetOf(IEnumerable<string> other) => _set.IsSubsetOf(other);

	public bool IsSupersetOf(IEnumerable<string> other) => _set.IsSupersetOf(other);

	public bool Overlaps(IEnumerable<string> other) => _set.Overlaps(other);

	public bool SetEquals(IEnumerable<string> other) => _set.SetEquals(other);

	public void SymmetricExceptWith(IEnumerable<string> other) => _set.SymmetricExceptWith(other);

	public void UnionWith(IEnumerable<string> other) => _set.UnionWith(other);

	void ICollection<string>.Add(string item) => Add(item);

	public void Clear() => _set.Clear();

	public bool Contains(string item) => _set.Contains(item);

	public void CopyTo(string[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

	public int Count => _set.Count;

	public bool IsReadOnly => false;

	public IEnumerator<string> GetEnumerator() => _set.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();

	#endregion
}


