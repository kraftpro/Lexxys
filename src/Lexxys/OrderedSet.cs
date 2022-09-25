// Lexxys Infrastructural library.
// file: OrderedSet.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lexxys
{
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	public class OrderedSet<T>: IList<T>, ISet<T>
	{
		private List<T> _list;
		private readonly IEqualityComparer<T> _comparer;

		public OrderedSet()
		{
			_comparer = EqualityComparer<T>.Default;
			_list = new List<T>();
		}

		public OrderedSet(IEnumerable<T> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			_comparer = EqualityComparer<T>.Default;
			_list = new List<T>();
			foreach (var item in items)
			{
				Add(item);
			}
		}

		public OrderedSet(IEqualityComparer<T> comparer)
		{
			_comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			_list = new List<T>();
		}

		public OrderedSet(IEnumerable<T> items, IEqualityComparer<T> comparer)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			_comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			_list = new List<T>();
			foreach (var item in items)
			{
				Add(item);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IndexOf(T item)
		{
			return _list.FindIndex(o => _comparer.Equals(o, item));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Insert(int index, T item)
		{
			if (_list.FindIndex(o => _comparer.Equals(o, item)) < 0)
				_list.Insert(index, item);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RemoveAt(int index)
		{
			_list.RemoveAt(index);
		}

		public T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return _list[index]; }
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set { _list[index] = value; }
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Add(T item)
		{
			if (_list.FindIndex(o => _comparer.Equals(o, item)) >= 0)
				return false;
			_list.Add(item);
			return true;
		}

		public void AddRange(IEnumerable<T> items)
		{
			if (items == null)
				return;
			foreach (var item in items)
			{
				Add(item);
			}
		}

		void ICollection<T>.Add(T item)
		{
			Add(item);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			_list.Clear();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(T item)
		{
			return _list.FindIndex(o => _comparer.Equals(o, item)) >= 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Predicate<T> predicate)
		{
			return _list.FindIndex(predicate) >= 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyTo(T[] array, int arrayIndex)
		{
			_list.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return _list.Count; }
		}

		public bool IsReadOnly
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return false; }
		}

		public bool Remove(T item)
		{
			int i = _list.FindIndex(o => _comparer.Equals(o, item));
			if (i < 0)
				return false;
			_list.RemoveAt(i);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public void ExceptWith(IEnumerable<T> other)
		{
			if (other == null)
				return;
			foreach (var item in other)
			{
				int i = _list.FindIndex(o => _comparer.Equals(o, item));
				if (i >= 0)
					_list.RemoveAt(i);
			}
		}

		public void IntersectWith(IEnumerable<T> other)
		{
			if (other == null)
				return;
			var temp = new List<T>();
			foreach (var item in other)
			{
				int i = _list.FindIndex(o => _comparer.Equals(o, item));
				if (i >= 0)
					temp.Add(_list[i]);
			}
			_list = temp;
		}

		private bool IsSubsetOf(IEnumerable<T> other, bool strict)
		{
			if (other is ICollection c1)
			{
				if (c1.Count < _list.Count || strict && c1.Count == _list.Count)
					return false;
			}
			else if (other is ICollection<T> c2)
			{
				if (c2.Count < _list.Count || strict && c2.Count == _list.Count)
					return false;
			}

			bool extra = false;
			int count = 0;
			var bits = new BitArray(_list.Count);
			foreach (var item in other)
			{
				int i = _list.FindIndex(o => _comparer.Equals(o, item));
				if (i < 0)
				{
					extra = true;
					if (count == _list.Count)
						return true;
				}
				else if (!bits[i])
				{
					if (++count == _list.Count && (!strict || extra))
						return true;
					bits[i] = true;
				}
			}
			return false;
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return IsSubsetOf(other, false);
		}

		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return IsSubsetOf(other, true);
		}

		private bool IsSupersetOf(IEnumerable<T> other, bool strict)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			int count = 0;
			var bits = new BitArray(_list.Count);
			foreach (var item in other)
			{
				int i = _list.FindIndex(o => _comparer.Equals(o, item));
				if (i < 0)
					return false;
				if (!bits[i])
				{
					bits[i] = true;
					++count;
				}
			}
			return strict ? count < _list.Count: count <= _list.Count;
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return IsSupersetOf(other, false);
		}

		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return IsSupersetOf(other, true);
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			foreach (var item in other)
			{
				if (_list.FindIndex(o => _comparer.Equals(o, item)) >= 0)
					return true;
			}
			return false;
		}

		public bool SetEquals(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			int count = 0;
			var bits = new BitArray(_list.Count);
			foreach (var item in other)
			{
				int i = _list.FindIndex(o => _comparer.Equals(o, item));
				if (i < 0)
					return false;
				if (!bits[i])
				{
					++count;
					bits[i] = true;
				}
			}
			return count == _list.Count;
		}

		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			int count = 0;
			var bits = new BitArray(_list.Count);
			foreach (var item in other)
			{
				int i = _list.FindIndex(o => _comparer.Equals(o, item));
				if (i >= 0 && !bits[i])
				{
					++count;
					bits[i] = true;
				}
			}
			if (count < _list.Count)
			{
				if (count > 0)
				{
					var tmp = new List<T>(count);
					foreach (int index in bits)
					{
						tmp.Add(_list[index]);
					}
					_list = tmp;
				}
				else
				{
					_list.Clear();
				}
			}
		}

		public void UnionWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			foreach (var item in other)
			{
				Add(item);
			}
		}
	}
}


