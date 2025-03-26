// Lexxys Infrastructural library.
// file: FlagSet.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Lexxys;

using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Lexxys;

[DebuggerDisplay("Count = {Count}")]
[Serializable]
public sealed class FlagSet: ISet<string>, IReadOnlySet<string>, IEquatable<FlagSet>
{
	private const char NameDelimiter = ':';
	private const char GroupDelimiter = ';';

	private readonly HashSet<string> _set;

	public FlagSet(IEqualityComparer<string>? comparer = null)
	{
		_set = new HashSet<string>(comparer ?? StringComparer.OrdinalIgnoreCase);
	}

	public FlagSet(FlagSet value)
	{
		if (value is null) throw new ArgumentNullException(nameof(value));
		_set = new HashSet<string>(value._set, value._set.Comparer);
	}

	public FlagSet(string? value, IEqualityComparer<string>? comparer = null)
	{
		_set = value is { Length: >0 } ?
			new HashSet<string>(Split(value), comparer ?? StringComparer.OrdinalIgnoreCase):
			new HashSet<string>(comparer ?? StringComparer.OrdinalIgnoreCase);
	}

	private static string? Clean(string? item)
	{
		if (String.IsNullOrEmpty(item))
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
		
		static bool IsBlankOrDelimiter(char c) => IsBlank(c) || c is NameDelimiter or GroupDelimiter;
		static bool IsBlank(char c) => c is <= '\u0020' or >= '\u007F' and <= '\u00A0' or >= '\uD800';

		char[]? mem = s.Length > Tools.SafeStackAllocChar ? ArrayPool<char>.Shared.Rent(s.Length): null;
		var buf = mem == null ? stackalloc char[s.Length]: mem.AsSpan();
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

		var result = buf.Slice(0, n).ToString();
		if (mem != null)
			ArrayPool<char>.Shared.Return(mem);
		return result;
	}

	private static IEnumerable<string> Split(string? value)
	{
		var s = Clean(value);
		if (s is null)
			yield break;

		int l = 0;
		do
		{
			int k = GroupIndex(s, l);
			int i = s.IndexOf(NameDelimiter, l);
			while (i > 0 && i < k)
			{
				yield return s.Substring(l, i - l);
				i = s.IndexOf(NameDelimiter, i + 1);
			}

			yield return s.Substring(l, k - l);
			l = k + 1;
		} while (l < s.Length);

		static int GroupIndex(string value, int startIndex)
		{
			int i = value.IndexOf(GroupDelimiter, startIndex);
			return i < 0 ? value.Length: i;
		}
	}
	
	public static FlagSet operator +(FlagSet? left, FlagSet? right)
	{
		if (right is not { Count: >0 })
			return left is { Count: >0 } ? new FlagSet(left): [];
		if (left is not  { Count: >0 })
			return new FlagSet(right);
		var result = new FlagSet(left);
		result._set.UnionWith(right._set);
		return result;
	}

	public static FlagSet operator +(FlagSet? left, string? right)
	{
		if (String.IsNullOrEmpty(right))
			return left is { Count: >0 } ? new FlagSet(left): new FlagSet();
		return left is { Count: >0 } ? new FlagSet(left) { right }: new FlagSet(right);
	}

	public static FlagSet operator -(FlagSet? left, FlagSet? right)
	{
		if (left is not { Count: >0 })
			return new FlagSet();
		var result = new FlagSet(left);
		if (right is { Count: >0 })
			result._set.ExceptWith(right._set);
		return result;
	}

	public static FlagSet operator -(FlagSet? left, string? right)
	{
		if (left is not { Count: > 0 })
			return new FlagSet();
		var result = new FlagSet(left);
		result.Remove(right);
		return result;
	}

	public static FlagSet Parse(string? value) => new FlagSet(value);

	public static explicit operator FlagSet(string? value) => new FlagSet(value);

	public static explicit operator string(FlagSet value) => value.ToString();

	public static bool operator ==(FlagSet? left, FlagSet? right) => right?.Equals(left) ?? left is null;

	public static bool operator !=(FlagSet? left, FlagSet? right) => !right?.Equals(left) ?? left is not null;

	public override int GetHashCode() => HashCode.Join(_set.Count * 18775, _set);

	public override bool Equals(object? obj) => obj is FlagSet other && Equals(other);

	public bool Equals(FlagSet? other) => other is not null && (ReferenceEquals(this, other) || _set.SetEquals(other._set));

	public override string ToString()
	{
		if (Count == 0)
			return String.Empty;

		var text = new StringBuilder();
		text.Append(GroupDelimiter);
		string? last = null;
		var items = _set.ToArray();
		Array.Sort(items);
		foreach (var item in items)
		{
			if (last is not null && !PartOf(item, last))
				text.Append(last).Append(NameDelimiter).Append(GroupDelimiter);
			last = item;
		}
		return text.Append(last).Append(NameDelimiter).Append(GroupDelimiter).ToString();

		static bool PartOf(string current, string previous)
		{
			if (current.Length <= previous.Length)
				return false;
			if (!current.StartsWith(previous, StringComparison.Ordinal))
				return false;
			return current[previous.Length] == NameDelimiter;
		}
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


