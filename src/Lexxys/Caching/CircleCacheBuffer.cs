// Lexxys Infrastructural library.
// file: CircleCacheBuffer.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using Lexxys;

#nullable enable

namespace Lexxys
{
	public class CircleCacheBuffer<TKey, TValue> where TKey: notnull
	{
		private const int MaxCapacity = 128 * 1024;
		private const int MinCapacity = 4;

		private ConcurrentDictionary<TKey, int> _map;
		private TValue[] _buffer;
		private BitArray _used;
		private int _top;
		private readonly Func<TValue, TKey> _extractKey;
		private readonly Func<TValue, bool> _testDirty;

		public CircleCacheBuffer(int capacity, Func<TValue, TKey> extractKey, Func<TValue, bool>? testDirty = null)
		{
			if (capacity < MinCapacity || capacity > MaxCapacity)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);
			_map = new ConcurrentDictionary<TKey, int>(4 * Environment.ProcessorCount, capacity);
			_buffer = new TValue[capacity];
			_used = new BitArray(capacity);
			_extractKey = extractKey ?? throw new ArgumentNullException(nameof(extractKey));
			_testDirty = testDirty ?? AlwaysValid;
		}

		public CircleCacheBuffer(CircleCacheBuffer<TKey, TValue> that)
		{
			if (that == null)
				throw new ArgumentNullException(nameof(that));

			_map = new ConcurrentDictionary<TKey, int>(that._map);
			_buffer = new TValue[that._buffer.Length];
			that._buffer.CopyTo(_buffer, 0);
			_used = new BitArray(that._used);
			_top = that._top;
			_testDirty = that._testDirty;
			_extractKey = that._extractKey;
		}

		public int Capacity
		{
			get => _buffer.Length;
			set
			{
				if (_buffer.Length == value)
					return;
				if (value < MinCapacity || value > MaxCapacity)
					throw new ArgumentOutOfRangeException(nameof(value), value, null);

				var keys = new TKey[_buffer.Length];
				foreach (var item in _map)
				{
					keys[item.Value] = item.Key;
				}

				var buffer = new TValue[value];
				var map = new Dictionary<TKey, int>(value);
				var used = new BitArray(value);
				int k = _top;
				int j = value - 1;
				for (int i = 0; i < _buffer.Length; ++i)
				{
					if (--k < 0)
						k = _buffer.Length - 1;
					if (_used[k] && !_testDirty(_buffer[k]))
					{
						buffer[j] = _buffer[k];
						map[keys[k]] = j;
						used[j] = true;
						if (--j < 0)
							break;
					}
				}
				_top = 0;
				_map = new ConcurrentDictionary<TKey, int>(4 * Environment.ProcessorCount, map, EqualityComparer<TKey>.Default);
				_used = used;
				_buffer = buffer;
			}
		}

		public int Count => _map.Count;

		public void Add(TValue value)
		{
			TKey key = _extractKey(value);
			if (!_map.TryGetValue(key, out int i))
			{
				i = _top++;
				if (_top >= _buffer.Length)
					_top = 0;
				if (_used[i])
					_map.TryRemove(_extractKey(_buffer[i]), out _);
				_used[i] = true;
				_map[key] = i;
			}
			_buffer[i] = value;
		}

		public TValue? Get(TKey key)
		{
			int i = IndexOf(key);
			return i < 0 ? default: _buffer[i];
		}

		public bool Contains(TKey key)
		{
			return IndexOf(key) >= 0;
		}

		public TValue Remove(TKey key)
		{
			if (!_map.TryGetValue(key, out int i))
#pragma warning disable CS8603 // Possible null reference return.
				return default;
#pragma warning restore CS8603 // Possible null reference return.
			TValue value = _buffer[i];
			_map.TryRemove(key, out i);
#pragma warning disable CS8601 // Possible null reference assignment.
			_buffer[i] = default;
#pragma warning restore CS8601 // Possible null reference assignment.
			_used[i] = false;
			if (i == _top - 1 || _top == 0 && i == _buffer.Length - 1)
				_top = i;
			return value;
		}

		private int IndexOf(TKey key)
		{
			if (!_map.TryGetValue(key, out int i))
				return -1;
			if (!_testDirty(_buffer[i]))
				return i;
			_map.TryRemove(key, out i);
#pragma warning disable CS8601 // Possible null reference assignment.
			_buffer[i] = default;
#pragma warning restore CS8601 // Possible null reference assignment.
			_used[i] = false;
			if (i == _top - 1 || _top == 0 && i == _buffer.Length - 1)
				_top = i;
			return -1;
		}

		private static bool AlwaysValid(TValue value)
		{
			return false;
		}
	}
}
