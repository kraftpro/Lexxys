// Lexxys Infrastructural library.
// file: SafeList.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public class SafeList<T>: ISafeList<T>
	{
		private readonly List<T> _list;
		private readonly T _emptyValue = default!;

		public SafeList()
		{
			_list = new List<T>();
		}

		public SafeList(int count)
		{
			_list = new List<T>(count);
		}

		public SafeList(T emptyValue)
		{
			_list = new List<T>();
			_emptyValue = emptyValue;
		}

		public SafeList(int count, T emptyValue)
		{
			_list = new List<T>(count);
			_emptyValue = emptyValue;
		}

		public SafeList(IEnumerable<T> data)
		{
			_list = new List<T>(data);
		}

		public SafeList(IEnumerable<T> data, T emptyValue)
		{
			_list = new List<T>(data);
			_emptyValue = emptyValue;
		}

		public T this[int index]
		{
			get
			{
				if (index < 0)
					index = _list.Count + index;
				return index >= 0 && index < _list.Count ? _list[index]: _emptyValue;
			}
			set
			{
				if (index < 0)
					index = _list.Count + index;
				if (index  >= 0 && index <= _list.Count)
					_list[index] = value;
			}
		}

		public int IndexOf(T item)
		{
			return _list.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			if (index < 0)
				index = _list.Count + index;
			_list.Insert(index < 0 ? 0: index > _list.Count ? _list.Count: index, item);
		}

		public void RemoveAt(int index)
		{
			if (index < 0)
				index = _list.Count + index;
			if (index  >= 0 && index < _list.Count)
				_list.RemoveAt(index);
		}

		public void Add(T item)
		{
			_list.Add(item);
		}

		public void Clear()
		{
			_list.Clear();
		}

		public bool Contains(T item)
		{
			return _list.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_list.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _list.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			return _list.Remove(item);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}


