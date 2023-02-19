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
using System.Linq;
using System.Runtime.CompilerServices;

namespace Lexxys
{
	/// <summary>
	/// Simple implementation of <see cref="ISet{T}"/> based on internal <see cref="List{T}"/> with the complexity O(n).
	/// Keeps order of the elements. Suitable for small number of items.
	/// </summary>
	/// <typeparam name="T"></typeparam>
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

		/// <inheritdoc />
		public int IndexOf(T item) => _list.FindIndex(o => _comparer.Equals(o, item));

		/// <inheritdoc />
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Insert(int index, T item)
		{
			if (_list.FindIndex(o => _comparer.Equals(o, item)) < 0)
				_list.Insert(index, item);
		}

		/// <inheritdoc />
		public void RemoveAt(int index) => _list.RemoveAt(index);

		/// <inheritdoc />
		public T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return _list[index];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (_list.FindIndex(o => _comparer.Equals(o, value)) < 0)
					_list[index] = value;
			}
		}

		/// <inheritdoc />
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Add(T item)
		{
			if (_list.FindIndex(o => _comparer.Equals(o, item)) >= 0)
				return false;
			_list.Add(item);
			return true;
		}

		/// <summary>
		/// Adds the elements of the given collection to the end of this list / set.
		/// </summary>
		/// <param name="items"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public void AddRange(IEnumerable<T> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			foreach (var item in items)
			{
				Add(item);
			}
		}

		void ICollection<T>.Add(T item) => Add(item);

		/// <inheritdoc />
		public void Clear() => _list.Clear();

		/// <inheritdoc />
		public bool Contains(T item) => _list.FindIndex(o => _comparer.Equals(o, item)) >= 0;

		/// <inheritdoc />
		public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public int Count => _list.Count;

		/// <inheritdoc />
		public bool IsReadOnly => false;

		/// <inheritdoc />
		public bool Remove(T item)
		{
			int i = _list.FindIndex(o => _comparer.Equals(o, item));
			if (i < 0)
				return false;
			_list.RemoveAt(i);
			return true;
		}

		/// <inheritdoc />
		public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

		/// <inheritdoc />
		public void ExceptWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			foreach (var item in other)
			{
				int i = _list.FindIndex(o => _comparer.Equals(o, item));
				if (i >= 0)
					_list.RemoveAt(i);
			}
		}

		/// <inheritdoc />
		public void IntersectWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

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

		/// <inheritdoc />
		public bool IsSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			return IsSubsetOf(other, false);
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public bool IsSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			return IsSupersetOf(other, false);
		}

		/// <inheritdoc />
		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			return IsSupersetOf(other, true);
		}

		/// <inheritdoc />
		public bool Overlaps(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return other.Any(item => _list.FindIndex(o => _comparer.Equals(o, item)) >= 0);
		}

		/// <inheritdoc />
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
				if (bits[i])
					continue;
				++count;
				bits[i] = true;
			}
			return count == _list.Count;
		}

		/// <inheritdoc />
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
			if (count == _list.Count)
				return;

			if (count == 0)
			{
				_list.Clear();
			}
			else
			{
				var tmp = new List<T>(count);
				for (int i = 0; i < _list.Count; ++i)
				{
					if (bits[i])
						tmp.Add(_list[i]);
				}
				_list = tmp;
			}
		}

		/// <inheritdoc />
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


