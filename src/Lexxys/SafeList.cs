// Lexxys Infrastructural library.
// file: SafeList.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys
{
	[Serializable]
	public class SafeList<T>: ISafeList<T>
	{
		private readonly List<T> _list;
		private readonly T _emptyValue;

		public SafeList(T emptyValue)
		{
			_list = new List<T>();
			_emptyValue = emptyValue;
		}

		public SafeList(T emptyValue, int count)
		{
			_list = new List<T>(count);
			_emptyValue = emptyValue;
		}

		public SafeList(T emptyValue, IEnumerable<T> data)
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

		public int IndexOf(T item) => _list.IndexOf(item);

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
			if (index >= 0 && index < _list.Count)
				_list.RemoveAt(index);
		}

		public void Add(T item) => _list.Add(item);

		public void Clear() => _list.Clear();

		public bool Contains(T item) => _list.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

		public int Count => _list.Count;

		public bool IsReadOnly => false;

		public bool Remove(T item) => _list.Remove(item);

		public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	}
}


