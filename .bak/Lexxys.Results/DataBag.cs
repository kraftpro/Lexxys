using System;
using System.Collections;

namespace Lexxys;

internal class DataBag: IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
{
    private string[] _keys;
    private object?[] _values;
    private int _count;

    public DataBag()
    {
        _keys = [];
        _values = [];
    }

    public DataBag(IDictionary<string, object?> items)
    {
        _count = items.Count;
		if (_count == 0)
		{
			_keys = [];
			_values = [];
			return;
		}

        _keys = new string[_count];
        _values = new object?[_count];
        int i = 0;
        foreach (var kvp in items)
        {
            _keys[i] = kvp.Key;
            _values[i] = kvp.Value;
            ++i;
        }
        Array.Sort(_keys, _values, StringComparer.Ordinal);
    }

    public DataBag(IEnumerable<(string Key, object? Value)> items)
    {
        int count = items switch
        {
            ICollection<(string Key, object? Value)> collection => collection.Count,
            IReadOnlyCollection<(string Key, object? Value)> readOnlyCollection => readOnlyCollection.Count,
            _ => -1
        };

		if (count > 0)
		{
			_count = count;
			_keys = new string[_count];
			_values = new object?[_count];
			int i = 0;
			foreach (var (key, value) in items)
			{
				_keys[i] = key;
				_values[i] = value;
				++i;
			}
			Array.Sort(_keys, _values, StringComparer.Ordinal);
		}
		else
		{
			_keys = [];
			_values = [];
			foreach (var (key, value) in items)
			{
				Add(key, value);
			}
		}
	}

    public object? this[string key]
    {
        get => TryGetValue(key, out var value) ? value: throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");
        set => Add(key, value);
    }

    public int Count => _count;
    public bool IsReadOnly => false;
    public ICollection<string> Keys => new KeysCollection(_keys, _count);
    public ICollection<object?> Values => new ValuesCollection(_values, _count);
    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => new KeysCollection(_keys, _count);
    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => new ValuesCollection(_values, _count);

    public void Add(string key, object? value)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));

        int i, j = 0;
        string variant = key;
        while((i = Array.BinarySearch(_keys, variant, StringComparer.Ordinal)) >= 0)
        {
            if (Object.Equals(_values[i], value))
                return;
            variant = $"{key}.{++j}";
        }
        i = ~i;
        if (_keys.Length == _count)
        {
            int size = _count == 0 ? 4 : _count * 2;
            Array.Resize(ref _keys, size);
            Array.Resize(ref _values, size);
        }
        if (i < _count)
        {
            Array.Copy(_keys, i, _keys, i + 1, _count - i);
            Array.Copy(_values, i, _values, i + 1, _count - i);
        }
        _keys[i] = variant;
        _values[i] = value;
        ++_count;
    }

	public void Add(KeyValuePair<string, object?> item) => Add(item.Key, item.Value);

	public void Clear()
	{
        Array.Clear(_keys, 0, _count);
        Array.Clear(_values, 0, _count);
        _count = 0;
	}

	public bool Contains(KeyValuePair<string, object?> item)
	{
		int i = Array.BinarySearch(_keys, item.Key, StringComparer.Ordinal);
		return i >= 0 && Object.Equals(_values[i], item.Value);
	}

	public bool ContainsKey(string key) => Array.BinarySearch(_keys, key, StringComparer.Ordinal) >= 0;

	public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        int count = Math.Min(_count, array.Length - arrayIndex);
        for (int i = 0; i < count; ++i)
        {
            array[arrayIndex + i] = new KeyValuePair<string, object?>(_keys[i], _values[i]);
        }
    }

	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => Enumerable.Range(0, _count).Select(i => new KeyValuePair<string, object?>(_keys[i], _values[i])).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public bool Remove(string key)
    {
        int i = Array.BinarySearch(_keys, key, StringComparer.Ordinal);
        if (i < 0)
            return false;
        if (i < _count - 1)
        {
            Array.Copy(_keys, i + 1, _keys, i, _count - i - 1);
            Array.Copy(_values, i + 1, _values, i, _count - i - 1);
        }
        _keys[--_count] = null!;
        _values[_count] = null;
        return true;
    }

	public bool Remove(KeyValuePair<string, object?> item)
	{
		int i = Array.BinarySearch(_keys, item.Key, StringComparer.Ordinal);
		if (i >= 0 && Object.Equals(_values[i], item.Value))
		{
			if (i < _count - 1)
			{
				Array.Copy(_keys, i + 1, _keys, i, _count - i - 1);
				Array.Copy(_values, i + 1, _values, i, _count - i - 1);
			}
			_keys[--_count] = null!;
			_values[_count] = null;
			return true;
		}
		return false;
	}

	public bool TryGetValue(string key, out object? value)
    {
        int i = Array.BinarySearch(_keys, key, StringComparer.Ordinal);
        if (i >= 0)
        {
            value = _values[i];
            return true;
        }
        value = null;
        return false;
    }

    readonly struct KeysCollection(string[] keys, int count): ICollection<string>
    {
        private readonly string[] _keys = keys;
        private readonly int _count = count;

		public int Count => _count;
        public bool IsReadOnly => true;

        public void Add(string item) => throw new NotSupportedException();

        public bool Remove(string item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(string item) => Array.BinarySearch(_keys, 0, _count, item, StringComparer.Ordinal) >= 0;

        public void CopyTo(string[] array, int arrayIndex)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            int count = Math.Min(_count, array.Length - arrayIndex);
            Array.Copy(_keys, 0, array, arrayIndex, count);
        }

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < _count; ++i)
            {
                yield return _keys[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    readonly struct ValuesCollection(object?[] values, int count): ICollection<object?>
    {
        private readonly object?[] _values = values;
        private readonly int _count = count;

		public int Count => _count;
		public bool IsReadOnly => true;

		public void Add(object? item) => throw new NotSupportedException();

		public bool Remove(object? item) => throw new NotSupportedException();

		public void Clear() => throw new NotSupportedException();

		public bool Contains(object? item) => Array.IndexOf(_values, item, 0, _count) >= 0;

		public void CopyTo(object?[] array, int arrayIndex)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            int count = Math.Min(_count, array.Length - arrayIndex);
            Array.Copy(_values, 0, array, arrayIndex, count);
        }
		public IEnumerator<object?> GetEnumerator()
		{
			for (int i = 0; i < _count; ++i)
            {
                yield return _values[i];
            }
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
