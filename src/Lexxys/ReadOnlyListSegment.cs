using Lexxys;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys;

public readonly struct ReadOnlyListSegment<T>: IReadOnlyList<T>
{
	private readonly IReadOnlyList<T> _list;
	private readonly int _start;
	private readonly int _length;

	public ReadOnlyListSegment(IReadOnlyList<T> list)
	{
		if (list is null) throw new ArgumentNullException(nameof(list));
		_list = list;
		_start = 0;
		_length = list.Count;
	}

	public ReadOnlyListSegment(IReadOnlyList<T> list, int start)
	{
		if (list == null) throw new ArgumentNullException(nameof(list));
		if ((uint)start > list.Count) throw new ArgumentOutOfRangeException(nameof(start));
		_list = list;
		_start = start;
		_length = list.Count - start;
	}

	public ReadOnlyListSegment(IReadOnlyList<T> list, int start, int length)
	{
		if (list == null) throw new ArgumentNullException(nameof(list));
		if ((uint)start > list.Count) throw new ArgumentOutOfRangeException(nameof(start));
		if ((uint)length > list.Count - start) throw new ArgumentOutOfRangeException(nameof(length));
		_list = list;
		_start = start;
		_length = length;
	}

	public T this[int index] => (uint)index < (uint)_length ? _list[_start + index]: throw new ArgumentOutOfRangeException(nameof(index));

	public int Count => _length;

	public bool IsEmpty => _length == 0;

	public ReadOnlyListSegment<T> Slice(int start) => new(_list, _start + start, _length - start);

	public ReadOnlyListSegment<T> Slice(int start, int length) => new(_list, _start + start, length);

	public IEnumerator<T> GetEnumerator() => new Enumerator(this);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private struct Enumerator: IEnumerator<T>
	{
		private readonly IReadOnlyList<T> _list;
		private readonly int _start;
		private readonly int _end;
		private int _index;

		public Enumerator(ReadOnlyListSegment<T> segment)
		{
			_list = segment._list;
			_start = segment._start;
			_end = segment._start + segment._length;
			_index = _start - 1;
		}
		public T Current => _index >= _start && _index < _end ? _list[_index]: throw new InvalidOperationException();

		public bool MoveNext() => ++_index < _end;

		public void Reset() => _index = _start - 1;

		object? IEnumerator.Current => Current;

		public void Dispose()
		{
		}
	}
}
