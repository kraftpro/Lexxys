// Lexxys Infrastructural library.
// file: Segment.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lexxys
{
	public static class Segment
	{
		public static IList<T> Create<T>(List<T> list, int offset)
		{
			return Create((IList<T>)list, offset, -1);
		}

		public static IList<T> Create<T>(List<T> list, int offset, int length)
		{
			return Create((IList<T>)list, offset, length);
		}

		public static IList<T> Create<T>(T[] list, int offset)
		{
			return Create((IList<T>)list, offset, -1);
		}

		public static IList<T> Create<T>(T[] list, int offset, int length)
		{
			return Create((IList<T>)list, offset, length);
		}

		public static IList<T> Create<T>(IList<T> list, int offset)
		{
			return new ListSegment<T>(list, offset, -1);
		}

		public static IList<T> Create<T>(IList<T> list, int offset, int length)
		{
			return new ListSegment<T>(list, offset, length);
		}

		public static IReadOnlyList<T> Create<T>(IReadOnlyList<T> list, int offset)
		{
			return new ReadOnlyListSegment<T>(list, offset, -1);
		}

		public static IReadOnlyList<T> Create<T>(IReadOnlyList<T> list, int offset, int length)
		{
			return new ReadOnlyListSegment<T>(list, offset, length);
		}

		public static (IList<T>, IList<T>) Split<T>(IList<T> list, int boundary)
		{
			if (boundary < 0 || boundary > list.Count)
				throw new ArgumentOutOfRangeException(nameof(boundary), boundary, null);
			if (boundary == 0)
				return (EmptyArray<T>.Value, list);
			if (boundary == list.Count)
				return (list, EmptyArray<T>.Value);
			return (Create(list, 0, boundary), Create(list, boundary));
		}

		public static Tuple<IReadOnlyList<T>, IReadOnlyList<T>> Split<T>(IReadOnlyList<T> list, int boundary)
		{
			if (boundary < 0 || boundary > list.Count)
				throw new ArgumentOutOfRangeException(nameof(boundary), boundary, null);
			if (boundary == 0)
				return new Tuple<IReadOnlyList<T>, IReadOnlyList<T>>(EmptyArray<T>.Value, list);
			if (boundary == list.Count)
				return new Tuple<IReadOnlyList<T>, IReadOnlyList<T>>(list, EmptyArray<T>.Value);
			return new Tuple<IReadOnlyList<T>, IReadOnlyList<T>>(Create(list, 0, boundary), Create(list, boundary));
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
		class ListSegment<T>: IList<T>
		{
			private readonly IList<T> _list;
			private readonly int _start;
			private int _end;

			public ListSegment(IList<T> list, int offset, int length)
			{
				_list = list ?? throw new ArgumentNullException(nameof(list));

				if (offset < 0)
					offset = 0;
				else if (offset >= list.Count)
					offset = list.Count;
				_start = offset;

				if (length < 0)
				{
					_end = -1;
				}
				else
				{
					_end = offset + length;
					if (_end > list.Count)
						_end = list.Count;
				}
			}

			public int Count => _end < 0 ? _list.Count - _start: _end - _start;

			public bool IsReadOnly => _list.IsReadOnly;

			public T this[int index]
			{
				get
				{
					int i = index + _start;
					if (i < 0 || i >= (_end < 0 ? _list.Count: _end))
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
					return _list[i];
				}
				set
				{
					int i = index + _start;
					if (i < 0 || i >= (_end < 0 ? _list.Count: _end))
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
					_list[i] = value;
				}
			}

			public void Add(T item)
			{
				if (_end < 0)
				{
					_list.Add(item);
				}
				else if (_end == _list.Count)
				{
					_list.Add(item);
					++_end;
				}
				else
				{
					_list.Insert(_end, item);
					++_end;
				}
			}

			public void Clear()
			{
				int count = (_end < 0 ? _list.Count : _end) - _start;
				while (count > 0)
				{
					_list.RemoveAt(_start);
					--count;
				}
				if (_end > 0)
					_end = _start;
			}

			public bool Contains(T item)
			{
				return IndexOf(item) >= 0;
			}

			public void CopyTo(T[] array, int arrayIndex)
			{
				int end = (_end < 0 ? _list.Count: _end);
				if (arrayIndex < 0 || array.Length - arrayIndex < end - _start)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, null);

				for (int i = _start; i < end; ++i)
				{
					array[i - _start] = _list[i];
				}
			}

			public int IndexOf(T item)
			{
				if (_start == 0)
				{
					if (_end < 0 || _end == _list.Count)
						return _list.IndexOf(item);
					int i = _list.IndexOf(item);
					return i < _end ? i: -1;
				}
				int end = _end < 0 ? _list.Count: _end;
				for (int i = _start; i < end; ++i)
				{
					if (Object.Equals(_list[i], item))
						return i - _start;
				}
				return -1;
			}

			public void Insert(int index, T item)
			{
				int i = index + _start;
				if (i < 0 || i > (_end < 0 ? _list.Count: _end))
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				_list.Insert(i, item);
				if (_end >= 0)
					++_end;
			}

			public bool Remove(T item)
			{
				int i = IndexOf(item);
				if (i < 0)
					return false;
				_list.RemoveAt(i + _start);
				if (_end > 0)
					--_end;
				return true;
			}

			public void RemoveAt(int index)
			{
				int i = index + _start;
				if (i < 0 || i >= (_end < 0 ? _list.Count: _end))
					throw new ArgumentOutOfRangeException(nameof(index), index, null);
				_list.RemoveAt(i);
				if (_end > 0)
					--_end;
			}

			public IEnumerator<T> GetEnumerator()
			{
				for (int i = _start; i < (_end < 0 ? _list.Count: _end); ++i)
				{
					yield return _list[i];
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		}

		[DebuggerDisplay("Count = {Count}")]
		[DebuggerTypeProxy(typeof(ReadOnlyCollectionDebugView<>))]
		class ReadOnlyListSegment<T>: IReadOnlyList<T>
		{
			private readonly IReadOnlyList<T> _list;
			private readonly int _start;
			private readonly int _end;

			public ReadOnlyListSegment(IReadOnlyList<T> list, int offset, int length)
			{
				_list = list ?? throw new ArgumentNullException(nameof(list));

				if (offset < 0)
					offset = 0;
				else if (offset >= list.Count)
					offset = list.Count;
				_start = offset;

				if (length < 0)
				{
					_end = list.Count;
				}
				else
				{
					_end = offset + length;
					if (_end > list.Count)
						_end = list.Count;
				}
			}

			public int Count => _end - _start;

			public T this[int index]
			{
				get
				{
					int i = index + _start;
					if (i < 0 || i >= _end)
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
					return _list[i];
				}
			}

			public IEnumerator<T> GetEnumerator()
			{
				for (int i = _start; i < _end; ++i)
				{
					yield return _list[i];
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}


