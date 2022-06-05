// Lexxys Infrastructural library.
// file: LogRecordQueue.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#if false
﻿//#define UseSpinLock
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 0420

namespace Lexxys.Logging
{
	class LogRecordQueue: IEnumerable<LogRecord>
	{
		readonly int _segmentWidth;
		volatile LogRecordQueueSegment _head;
		volatile LogRecordQueueSegment _tail;
		volatile int _count;

		public LogRecordQueue(int segmentWidth)
		{
			_segmentWidth = segmentWidth;
			_head = _tail = new LogRecordQueueSegment(_segmentWidth);
		}

		public int Count
		{
			get { return _count; }
		}

#if UseSpinLock
		public void Enqueue(LogRecord value)
		{
			bool lockTaken = false;
			try
			{
				_locker.Enter(ref lockTaken);
				_head.Items[_head.Index] = value;
				++_head.Count;
				if (++_head.Index >= _segmentWidth)
				{
					Debug.Assert(_head.Index == _segmentWidth);
					_head = _head.Next = new LogRecordQueueSegment(_segmentWidth);
				}
				++_count;
			}
			finally
			{ 
				if (lockTaken)
					_locker.Exit(false);
			}
		}
		private SpinLock _locker = new SpinLock(false);
#else

		public void Enqueue(LogRecord value)
		{
			SpinWait sw = new SpinWait();

			for (;;)
			{
				LogRecordQueueSegment tmp = _head;
				int index;
				try
				{
				}
				finally
				{
					index = Interlocked.Increment(ref tmp.Count);						// try to reserve a slot for writing
					if (index >= _segmentWidth)											// need new segment
					{
						var next = new LogRecordQueueSegment(_segmentWidth);
						if (Interlocked.CompareExchange(ref _head, next, tmp) == tmp)	// create new segment
							tmp.Next = next;
					}
					if (index <= _segmentWidth)											// slot has been received
					{
						tmp.Items[index - 1] = value;
						Interlocked.Increment(ref _count);
					}
				}
				if (index <= _segmentWidth)
					return;

				sw.SpinOnce();
			}
		}

#endif

		// Single thread method
		public LogRecordQueueSegment Dequeue()
		{
			if (_count == 0)
				return null;

			SpinWait sw = new SpinWait();
			LogRecordQueueSegment next = null;

			for (;;)
			{
				int index = _tail.Count;
				LogRecordQueueSegment tmp = _tail;
				if (tmp.Next != null)
				{
					Interlocked.Add(ref _count, -_segmentWidth);
					_tail = tmp.Next;
					tmp.Next = null;
					tmp.Count = _segmentWidth;
					return tmp;
				}
				if (index < _segmentWidth && index == tmp.Count && tmp.Items[index - 1] != null)
				{
					if (next == null)
						next = new LogRecordQueueSegment(_segmentWidth);
					if (Interlocked.CompareExchange(ref _head, next, tmp) == tmp)
					{
						_tail = next;
						int n = Interlocked.CompareExchange(ref tmp.Count, _segmentWidth, index);		// barier for reserving a new slot
						if (n > _segmentWidth)
							n = _segmentWidth;
						for (int i = 0; i < n; ++i)
						{
							while (tmp.Items[i] == null)
								sw.SpinOnce();
						}
						tmp.Count = n;
						return tmp;
					}
				}
				sw.SpinOnce();
			}
		}

		//public IList<LogRecord> DequeueAll()
		//{
		//	Interlocked.Exchange(ref _head, new Segment(_segmentWidth));
		//	Segment tail = _tail;
		//	_tail = _head;

		//	int left = _count;
		//	LogRecord[] buffer = new LogRecord[left];
		//	int index = 0;
		//	Segment current = _tail;
		//	while (left > 0)
		//	{
		//		int n = current.Count;
		//		if (current.Count == _segmentWidth || current.Count >= left)
		//		{
		//			int n = left <= _segmentWidth ? left: _segmentWidth;
		//			Array.Copy(current.Items, 0, buffer, index, n);
		//			left -= n;
		//		}
		//		else
		//		{
		//			int n = left <= _segmentWidth ? left: _segmentWidth;
		//		}

		//		var items = current.Items;
		//		for (int i = 0; i < items.Length; ++i)
		//		{
		//		}
		//		current = current.Next;
		//	}
		//}

		public void Clear()
		{
			//_peeked = null;
			var tmp = Interlocked.Exchange(ref _tail, new LogRecordQueueSegment(_segmentWidth));
			_head = _tail;
			_count = 0;
			while (tmp != null)
			{
				var x = tmp.Next;
				tmp.Clear();
				tmp = x;
			}
		}

		public IEnumerator<LogRecord> GetEnumerator()
		{
			LogRecordQueueSegment current = _tail;
			while (current != null)
			{
				var items = current.Items;
				for (int i = 0; i < items.Length; ++i)
				{
					if (items[i] != null)
						yield return items[i];

				}
				current = current.Next;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	class LogRecordQueueSegment: IEnumerable<LogRecord>
	{
		volatile public LogRecord[] Items;
		volatile public int Count;
		volatile public LogRecordQueueSegment Next;

		public LogRecordQueueSegment(int width)
		{
			Items = new LogRecord[width];
		}

		public LogRecord Single()
		{
			for (int i = 0; i < Items.Length; ++i)
			{
				if (Items[i] != null)
					return Items[i];
			}
			return null;
		}

		public void Clear()
		{
			Array.Clear(Items, 0, Items.Length);
			Count = 0;
			Next = null;
		}

		public IEnumerator<LogRecord> GetEnumerator()
		{
			if (Count > 0)
			{
				int k = 0;
				for (int i = 0; i < Items.Length; ++i)
				{
					if (Items[i] != null)
					{
						yield return Items[i];
						if (++k == Count)
							break;
					}
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}


#if OLD
	class LogRecordQueue: IEnumerable<LogRecord>
	{
		int _segmentWidth;
		Segment _head;
		int _positionHead;
		Segment _tail;
		int _positionTail;
		Segment _pool;
		int _count;

		public LogRecordQueue(int segmentWidth)
		{
			_segmentWidth = segmentWidth;
			_head = _tail = new Segment(_segmentWidth);
		}

		public int Count
		{
			get { return _count; }
		}

		public void Enqueue(LogRecord value)
		{
			if (_positionHead < _head.Items.Length)
			{
				_head.Items[_positionHead++] = value;
			}
			else
			{
				if (_pool == null)
				{
					_head = _head.Next = new Segment(_segmentWidth);
				}
				else
				{
					_head = _head.Next = _pool;
					_pool = _pool.Next;
					_head.Next = null;
				}
				_head.Items[0] = value;
				_positionHead = 1;
			}
			++_count;
		}

		public LogRecord Dequeue()
		{
			if (_count == 0)
				throw EX.InvalidOperation();

			LogRecord r = _tail.Items[_positionTail];
			_tail.Items[_positionTail++] = null;
			--_count;
			if (_positionTail >= _segmentWidth)
			{
				if (_count == 0)
				{
					Debug.Assert(_head == _tail);
					_positionHead = _positionTail = 0;
				}
				else
				{
					var tmp = _tail;
					_tail = _tail.Next;
					tmp.Next = _pool;
					_pool = tmp;
				}
			}
			return r;
		}

		public IList<LogRecord> DequeueBlock()
		{
			if (_count == 0)
				return NoResults<LogRecord>.Items;

			if (_positionTail == 0)
			{
				if (_count >= _segmentWidth)
				{
					_count -= _segmentWidth;
					if (_count == 0)
					{
						Debug.Assert(_head == _tail);
						var tmp = _tail.Items;
						_tail.Items = new LogRecord[_segmentWidth];
						_positionHead = 0;
						return tmp;
					}
					else
					{
						Segment tmp = _tail;
						_tail = _tail.Next;
						tmp.Next = null;
						return tmp.Items;
					}
				}
				Debug.Assert(_head == _tail);
				LogRecord[] result;
				if (_count == 1)
				{
					result = new LogRecord[] { _tail.Items[0] };
				}
				else if (_count == 2)
				{
					result = new LogRecord[] { _tail.Items[0], _tail.Items[1] };
				}
				else
				{
					result = new LogRecord[_count];
					Array.Copy(_tail.Items, result, _count);
				}
				_positionHead = _count = 0;
				return result;
			}

			if (_head != _tail)
			{
				Segment tmp = _tail;
				_tail = _tail.Next;
				tmp.Next = null;
				_count -= _segmentWidth - _positionTail;
				var result = ReadOnly.Wrap(tmp.Items, _positionTail);
				_positionTail = 0;
				return result;
			}
			else
			{
				var result = ReadOnly.Wrap(_tail.Items, _positionTail, _count);
				_head = _tail = new Segment(_segmentWidth);
				_positionHead = _positionTail = _count = 0;
				return result;
			}
		}

		public void Clear()
		{
			Segment tmp = _tail;
			do
			{
				Array.Clear(tmp.Items, 0, _segmentWidth);
				tmp = tmp.Next;
			} while (tmp != null);
			if (_head != _tail)
			{
				_head.Next = _pool;
				_pool = _tail.Next;
				_head = _tail;
			}
			_positionHead = _positionTail = 0;
		}

		class Segment
		{
			public LogRecord[] Items;
			public Segment Next;

			public Segment(int width)
			{
				Items = new LogRecord[width];
			}
		}

		public IEnumerator<LogRecord> GetEnumerator()
		{
			int position = _positionTail;
			Segment current = _tail;
			while (current != _head)
			{
				while (position < _segmentWidth)
				{
					yield return current.Items[position++];
				}
				current = current.Next;
				position = 0;
			}
			while (position < _positionHead)
			{
				yield return current.Items[position++];
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
#endif
#if false
	class LogRecordQueue2: IEnumerable<LogRecord>
	{
		SemaphoreSlim _semaphore;
		int _segmentWidth;
		Segment _head;
		int _position;
		ConcurrentQueue<Segment> _tail;
		int _count;

		public LogRecordQueue2(int segmentWidth)
		{
			_semaphore = new SemaphoreSlim(segmentWidth, segmentWidth);
			_segmentWidth = segmentWidth;
			_tail = new ConcurrentQueue<Segment>();
			_head = new Segment(_segmentWidth);
		}

		public int Count
		{
			get { return _count; }
		}

		public void Enqueue(LogRecord value)
		{
		again:
			Segment tmp = _head;
			int index = Interlocked.Increment(ref tmp.Index);
			if (index >= _segmentWidth)
			{
				Segment old = Interlocked.CompareExchange(ref _head, new Segment(_segmentWidth), tmp);
				if (old == tmp)
					_tail.Enqueue(tmp);
				if (index > _segmentWidth)
					goto again;
			}
			tmp.Item[index-1] = value;
			Interlocked.Increment(ref tmp.Count);
			Interlocked.Increment(ref _count);
		}

		Segment _peeked;
		int _peekedAttempts;
		public IList<LogRecord> DequeueBlock()
		{
			Segment tmp = _peeked;
			if (tmp != null)
			{
				if (tmp.Count != tmp.Item.Length)
				{
					if (++_peekedAttempts < 32)
						return NoResults<LogRecord>.Items;
				}
				_peeked = null;
				return tmp.Item;
			}
			if (_tail.TryDequeue(out tmp))
			{
				if (tmp.Count != tmp.Item.Length)
				{
					_peeked = tmp;
					_peekedAttempts = 0;
					return NoResults<LogRecord>.Items;
				}
				_tail.TryDequeue(out tmp);
				return tmp.Item;
			}

			return tmp.Count == tmp.Item.Length ? tmp.Item: NoResults<LogRecord>.Items;

			if (_count == 0)
				return NoResults<LogRecord>.Items;

			if (_positionTail == 0)
			{
				if (_count >= _segmentWidth)
				{
					_count -= _segmentWidth;
					if (_count == 0)
					{
						Debug.Assert(_head == _tail);
						var tmp = _tail.Items;
						_tail.Items = new LogRecord[_segmentWidth];
						_position = 0;
						return tmp;
					}
					else
					{
						Segment tmp = _tail;
						_tail = _tail.Next;
						tmp.Next = null;
						return tmp.Item;
					}
				}
				Debug.Assert(_head == _tail);
				LogRecord[] result;
				if (_count == 1)
				{
					result = new LogRecord[] { _tail.Items[0] };
				}
				else if (_count == 2)
				{
					result = new LogRecord[] { _tail.Items[0], _tail.Items[1] };
				}
				else
				{
					result = new LogRecord[_count];
					Array.Copy(_tail.Items, result, _count);
				}
				_position = _count = 0;
				return result;
			}

			if (_head != _tail)
			{
				Segment tmp = _tail;
				_tail = _tail.Next;
				tmp.Next = null;
				_count -= _segmentWidth - _positionTail;
				var result = ReadOnly.Wrap(tmp.Item, _positionTail);
				_positionTail = 0;
				return result;
			}
			else
			{
				var result = ReadOnly.Wrap(_tail.Items, _positionTail, _count);
				_head = _tail = new Segment(_segmentWidth);
				_position = _positionTail = _count = 0;
				return result;
			}
		}

		public void Clear()
		{
			Segment tmp = _tail;
			do
			{
				Array.Clear(tmp.Item, 0, _segmentWidth);
				tmp = tmp.Next;
			} while (tmp != null);
			if (_head != _tail)
			{
				_head.Next = _pool;
				_pool = _tail.Next;
				_head = _tail;
			}
			_position = _positionTail = 0;
		}

		class Segment
		{
			public LogRecord[] Item;
			public int Index;
			public int Count;

			public Segment(int width)
			{
				Item = new LogRecord[width];
			}
		}

		public IEnumerator<LogRecord> GetEnumerator()
		{
			int position = _positionTail;
			Segment current = _tail;
			while (current != _head)
			{
				while (position < _segmentWidth)
				{
					yield return current.Item[position++];
				}
				current = current.Next;
				position = 0;
			}
			while (position < _position)
			{
				yield return current.Item[position++];
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
#endif
}
#endif

