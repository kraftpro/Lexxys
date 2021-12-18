// Lexxys Infrastructural library.
// file: CircleCacheBuffer.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#if false // Multithreading issues
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

#nullable enable

namespace Lexxys
{
	/// <summary>
	/// FIFO key-value cache with fixed capacity.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class CircleCacheBuffer<TKey, TValue>
		where TKey: notnull
		where TValue: class
	{
		private const int MaxCapacity = 256 * 1024;
		private const int MinCapacity = 4;

		private Buffer _buffer;

		class Buffer
		{
			private readonly ConcurrentDictionary<TKey, int> _map;
			private readonly TValue[] _buffer;
			private readonly Func<TValue, TKey> _extractKey;
			private readonly Func<TValue, bool> _testDirty;
			private int _top;

			public Buffer(int capacity, Func<TValue, TKey> extractKey, Func<TValue, bool> testDirty)
			{
				_map = new ConcurrentDictionary<TKey, int>(4 * Environment.ProcessorCount, capacity);
				_buffer = new TValue[capacity];
				_extractKey = extractKey ?? throw new ArgumentNullException(nameof(extractKey));
				_testDirty = testDirty ?? throw new ArgumentNullException(nameof(testDirty));
			}

			public Buffer(Buffer that, int capacity)
			{
				_map = new ConcurrentDictionary<TKey, int>();
				_testDirty = that._testDirty;
				_extractKey = that._extractKey;
				var buffer = new TValue[that._buffer.Length];
				int j = that._top % buffer.Length;
				that._buffer.CopyTo(buffer, 0);
				int top = 0;
				_buffer = new TValue[capacity];
				for (int k = buffer.Length; k > 0; --k)
				{
					var v = buffer[j++];
					if (j == buffer.Length)
						j = 0;
					if (v is null)
						continue;

					_buffer[top] = v;
					_map[_extractKey(v)] = top;
					++top;
					if (top == capacity)
						break;
				}
				_top = top;
			}

			public int Capacity => _buffer.Length;

			public int Count => _map.Count;

			[return: MaybeNull]
			public TValue Get(TKey key)
			{
				if (!_map.TryGetValue(key, out int i))
					return default;
				var value = _buffer[i];
				if (!Equals(key, _extractKey(value)))
				{
					_map.TryRemove(key, out _);
					return default;
				}
				if (!_testDirty(value))
					return value;
				_map.TryRemove(key, out _);
				return default;
			}

			public void Add(TValue value)
			{
				if (value is null)
					return;
				var i = Interlocked.Increment(ref _top) % _buffer.Length;
				var x = Interlocked.Exchange(ref _buffer[i], value);
				_map[_extractKey(value)] = i;
				if (x is not null)
					_map.TryRemove(_extractKey(x), out _);
			}

			public void Remove(TKey key) => _map.TryRemove(key, out _);
		}

		/// <summary>
		/// Creates a new <see cref="CircleCacheBuffer{TKey,TValue}"/>
		/// </summary>
		/// <param name="capacity">Capacity of the cache</param>
		/// <param name="extractKey">Function to extract a key from the value</param>
		/// <param name="testDirty">Function to test if the value is dirty</param>
		/// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="capacity"/> is less then <value>MinValue</value> or greater then <value>MaxValue</value>.</exception>
		/// <exception cref="ArgumentNullException">The <paramref name="extractKey"/> function is null.</exception>
		public CircleCacheBuffer(int capacity, Func<TValue, TKey> extractKey, Func<TValue, bool>? testDirty = null)
		{
			if (capacity < MinCapacity || capacity > MaxCapacity)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);
			_buffer = new Buffer(capacity, extractKey, testDirty ?? AlwaysValid);

			static bool AlwaysValid(TValue value) => false;
		}

		/// <summary>
		/// Creates a copy of the <see cref="CircleCacheBuffer{TKey,TValue}"/>
		/// </summary>
		/// <param name="that"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public CircleCacheBuffer(CircleCacheBuffer<TKey, TValue> that)
		{
			if (that == null)
				throw new ArgumentNullException(nameof(that));
			_buffer = new Buffer(that._buffer, that.Capacity);
		}

		/// <summary>
		/// Get or set cache capacity
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">The value is less then <value>MinCapacity</value> or greater then <value>MaxCapacity</value></exception>
		public int Capacity
		{
			get => _buffer.Capacity;
			set
			{
				if (_buffer.Capacity == value)
					return;
				if (value is < MinCapacity or > MaxCapacity)
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
				Buffer original;
				Buffer target;
				do
				{
					original = _buffer;
					target = new Buffer(original, value);
				} while (Interlocked.CompareExchange(ref _buffer, target, original) == original);
			}
		}

		/// <summary>
		/// Get a number of items in the cache.
		/// </summary>
		public int Count => _buffer.Count;

		/// <summary>
		/// Adds or updates the <paramref name="value"/> in the cache.
		/// </summary>
		/// <param name="value"></param>
		public void Add(TValue value) => _buffer.Add(value);

		/// <summary>
		/// Returns the value from the cache corresponding to the specified <paramref name="key"/> or default value if the value is not found.
		/// </summary>
		/// <param name="key"></param>
		[return: MaybeNull]
		public TValue Get(TKey key) => _buffer.Get(key);

		/// <summary>
		/// Test if the specified <paramref name="key"/> exists in the cache.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool Contains(TKey key) => Get(key) is not null;

		/// <summary>
		/// Removes an item with the specified <paramref name="key"/> from the cache.  
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public void Remove(TKey key) => _buffer.Remove(key);
	}
}
#endif