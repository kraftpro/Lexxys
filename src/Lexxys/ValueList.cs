// Lexxys Infrastructural library.
// file: ValueList.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections;

namespace Lexxys;

public class ValueList<TValue>: IList<TValue>
	where TValue: struct
{
	private static readonly TValue[]?[] NoItems = Array.Empty<TValue[]>();
	private TValue[]?[] _items;
	private int _count;
	private int _capacity;
	private int _topI;
	private int _topJ;

	private const int DefaultCapacity = 128;
	private const int DefaultChain = 4;

	public ValueList()
	{
		_items = NoItems;
		_capacity = 0;
		_count = 0;
		_topI = 0;
		_topJ = 0;
	}

	public ValueList(int capacity)
	{
		_capacity = capacity;
		_items = new TValue[DefaultChain][];
		_items[0] = new TValue[capacity];
		_count = 0;
		_topI = 0;
		_topJ = 0;
	}

	public int Capacity
	{
		get
		{
			return _capacity;
		}
		set
		{
			if (value != _capacity)
			{
				if (value < _count)
					throw new ArgumentOutOfRangeException(nameof(value), value, null).Add(nameof(Count), _count);
				if (value <= _capacity)
					return;

				if (++_topI >= _items.Length)
				{
					if (_items.Length == 0)
					{
						_topI = 0;
						_items = new TValue[DefaultChain][];
					}
					else
					{
						var tmp = new TValue[_topI][];
						Array.Copy(_items, 0, tmp, 0, _items.Length);
						_items = tmp;
					}
				}
				_items[_topI] = new TValue[value - _capacity];
				_capacity = value;
			}
		}
	}

	public TValue Head
	{
		get
		{
			if (_count == 0)
				throw new InvalidOperationException(SR.CollectionIsEmpty());
			return _items[0]![0];
		}
		set
		{
			if (_count == 0)
				throw new InvalidOperationException(SR.CollectionIsEmpty());
			_items[0]![0] = value;
		}
	}

	public TValue Tail
	{
		get
		{
			if (_count == 0)
				throw new InvalidOperationException(SR.CollectionIsEmpty());
			return _topJ == 0 ?
				_items[_topI-1]![_items[_topI-1]!.Length - 1]:
				_items[_topI]![_topJ - 1];
		}
		set
		{
			if (_count == 0)
				throw new InvalidOperationException(SR.CollectionIsEmpty());
			if (_topJ == 0)
				_items[_topI-1]![_items[_topI-1]!.Length - 1] = value;
			else
				_items[_topI]![_topJ - 1] = value;
		}
	}

	public TValue this[int index]
	{
		get
		{
			if (index < 0 || index >= _count)
				throw new ArgumentOutOfRangeException(nameof(index), index, null);
			for (int i = 0; i <= _topI; ++i)
			{
				if (index < _items[i]!.Length)
					return _items[i]![index];
				index -= _items[i]!.Length;
			}
			throw new ArgumentOutOfRangeException(nameof(index), index, null);
		}
		set
		{
			if (index < 0 || index >= _count)
				if (index == _count)
				{
					Add(value);
					return;
				}
				else
					throw new ArgumentOutOfRangeException(nameof(index), index, null);

			for (int i = 0; i <= _topI; ++i)
			{
				if (index < _items[i]!.Length)
				{
					_items[i]![index] = value;
					return;
				}
				index -= _items[i]!.Length;
			}
			throw new ArgumentOutOfRangeException(nameof(index), index, null);
		}
	}

	public bool TryFind(Predicate<TValue> predicate, out TValue result)
	{
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		int ii = -1;
		int ij = 0;
		int len = 0;

		for (int i = 0; i < _count; ++i)
		{
			if (++ij >= len)
			{
				++ii;
				len = _items[ii]!.Length;
				ij = 0;
			}
			if (predicate(_items[ii]![ij]))
			{
				result = _items[ii]![ij];
				return true;
			}
		}
		result = new TValue();
		return false;
	}

	public int FindIndex(Predicate<TValue> predicate)
	{
		if (predicate == null)
			throw new ArgumentNullException(nameof(predicate));
		if (_count == 0)
			return -1;

		int ii = 0;
		int ij = 0;
		int len = _items[0]!.Length;

		for (int i = 0; i < _count; ++i, ++ij)
		{
			if (ij >= len)
			{
				++ii;
				len = _items[ii]!.Length;
				ij = 0;
			}
			if (predicate(_items[ii]![ij]))
			{
				return i;
			}
		}
		return -1;
	}

	private bool FindIndexPosition(int index, out int ii, out int ij)
	{
		if (index >= 0 && index < _count)
		{
			for (int i = 0; i <= _topI; ++i)
			{
				if (index < _items[i]!.Length)
				{
					ii = i;
					ij = index;
					return true;
				}
				index -= _items[i]!.Length;
			}
		}
		ii = -1;
		ij = -1;
		return false;
	}

	public int FindIndex(Predicate<TValue> predicate, int start)
	{
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		if (FindIndexPosition(start, out int ii, out int ij))
		{
			int len = _items[ii]!.Length;
			for (int i = start; i < _count; ++i, ++ij)
			{
				if (ij >= len)
				{
					++ii;
					len = _items[ii]!.Length;
					ij = 0;
				}
				if (predicate(_items[ii]![ij]))
				{
					return i;
				}
			}
		}
		return -1;
	}

	#region IList<_Type> Members
	public int IndexOf(TValue item)
	{
		return FindIndex(x => item.Equals(x));
	}

	public void Insert(int index, TValue item)
	{
		if (index != _count)
			throw new ArgumentOutOfRangeException(nameof(index), index, null).Add("expected", _count);
		Add(item);
	}

	public void RemoveAt(int index)
	{
		if (index != _count - 1 || _count == 0)
			throw new ArgumentOutOfRangeException(nameof(index), index, null).Add("expected", _count);
		--_count;
		if (--_topJ < 0)
		{
			_topJ = 0;
			--_topI;
		}
	}
	#endregion

	#region ICollection<_Type> Members
	public void Add(TValue item)
	{
		if (_count == _capacity)
		{
			_topJ = 0;
			if (++_topI < _items.Length)
			{
				_items[_topI] = new TValue[_capacity];
				_capacity += _capacity;
			}
			else
			{
				if (_items.Length == 0)
				{
					_items = new TValue[DefaultChain][];
					_items[0] = new TValue[DefaultCapacity];
					_capacity = DefaultCapacity;
					_topI = 0;
				}
				else
				{
					var tmp = new TValue[_items.Length*2][];
					for (int i = 0; i<_items.Length; ++i)
					{
						tmp[i] = _items[i]!;
						_items[i] = null;
					}
					_items = tmp;
					_items[_topI] = new TValue[_capacity];
					_capacity += _capacity;
				}
			}
		}
		_items[_topI]![_topJ] = item;
		++_topJ;
		++_count;
	}

	public void Clear()
	{
		_items = NoItems;
		_capacity = 0;
		_count = 0;
		_topI = 0;
		_topJ = 0;
	}

	public bool Contains(TValue item)
	{
		int left = _count;
		foreach (TValue[]? t in _items)
		{
			if (t == null)
				return false;
			int cnt = t.Length;
			if (cnt < left)
				cnt = left;
			for (int i = 0; i < cnt; ++i)
				if (item.Equals(t[i]))
					return true;
			if ((left -= cnt) <= 0)
				return false;
		}
		left = _count;
		foreach (TValue[]? t in _items)
		{
			if (t == null)
				return false;
			left -= t.Length;
			if (left >= 0)
			{
				for (int i = 0; i < t.Length; ++i)
					if (item.Equals(t[i]))
						return true;
			}
			else
			{
				left += t.Length;
				for (int i = 0; i < left; ++i)
					if (item.Equals(t[i]))
						return true;
				return false;
			}
		}
		return false;
	}

	public void CopyTo(TValue[] array, int arrayIndex)
	{
		int left = _count;
		foreach (TValue[]? t in _items)
		{
			if (t == null)
				return;
			int cnt = t.Length;
			if (cnt < left)
				cnt = left;
			Array.Copy(t, 0, array, arrayIndex, cnt);
			arrayIndex += cnt;
			if ((left -= cnt) <= 0)
				return;
		}
		left = _count;
		foreach (TValue[]? t in _items)
		{
			if (t == null)
				return;
			left -= t.Length;
			if (left >= 0)
			{
				Array.Copy(t, 0, array, arrayIndex, t.Length);
			}
			else
			{
				Array.Copy(t, 0, array, arrayIndex, left + t.Length);
				return;
			}
		}
	}

	public int Count
	{
		get { return _count; }
	}

	public bool IsReadOnly
	{
		get { return false; }
	}

	public bool Remove(TValue item)
	{
		throw new NotSupportedException(SR.OperationNotSupported("ValueList.Remove"));
	}
	#endregion

	#region IEnumerable<_Type> Members
	public IEnumerator<TValue> GetEnumerator()
	{
		int ii = 0;
		int ij = -1;
		for (int i = 0; i < _count; ++i)
		{
			if (++ij >= _items[ii]!.Length)
			{
				if (++ii >= _items.Length)
					break;
				ij = 0;
			}
			yield return _items[ii]![ij];
		}
	}
	#endregion

	#region IEnumerable Members
	IEnumerator IEnumerable.GetEnumerator()
	{
		int ii = 0;
		int ij = -1;
		for (int i = 0; i < _count; ++i)
		{
			if (++ij >= _items[ii]!.Length)
			{
				if (++ii >= _items.Length)
					break;
				ij = 0;
			}
			yield return _items[ii]![ij];
		}
	}
	#endregion
}


