// Lexxys Infrastructural library.
// file: RowsCollection.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Data;
using System.Globalization;

namespace Lexxys.Data;

public interface IFieldsCollection: IEnumerable<IDbField>
{
	int Count { get; }
	IDbField this[string index] { get; }
	IDbField this[int index] { get; }
}

public interface IDbField
{
	public string Name { get; }
	public object? Value { get; set; }
	public DbType Type { get; }

	public bool? GetBoolean();
	public short? GetInt16();
	public int? GetInt32();
	public long? GetInt64();
	public double? GetDouble();
	public DateTime? GetDateTime();
	public string? GetString();
	public decimal? GetDecimal();
	public byte[]? GetBytes();
	public Guid? GetGuid();
	public RowVersion? GetRowVersion();
}

public sealed class RowsCollection
{
	private readonly IFieldsCollection _fields;
	private readonly List<object?[]> _data;
	private int _currentIndex;

	public RowsCollection(IDataReader reader)
	{
		if (reader == null) throw new ArgumentNullException(nameof(reader));

		_data = [];
		while (reader.Read())
		{
			object?[] values = new object[reader.FieldCount];
			reader.GetValues(values!);
			for (int i = 0; i < values.Length; ++i)
			{
				if (values[i] is DBNull)
					values[i] = null;
			}
			_data.Add(values);
		}
		var fields = new DataTableField[reader.FieldCount];
		for (int i = 0; i < fields.Length; ++i)
		{
			fields[i] = new DataTableField(reader.GetName(i), reader.GetFieldType(i), this, i);
		}
		_fields = new FieldsCollection(fields);
	}

	private object?[] Row => !Eof ? _data[_currentIndex]: throw new InvalidOperationException();

	public bool Eof => _currentIndex >= _data.Count;

	public int Count => _data.Count;

	public IFieldsCollection Fields => _fields;

	public void MoveFirst()
	{
		_currentIndex = 0;
	}

	public bool MoveNext()
	{
		if (_currentIndex >= _data.Count)
			return false;
		++_currentIndex;
		return true;
	}

	private class FieldsCollection(DataTableField[] fields): IFieldsCollection
	{
		private readonly DataTableField[] _fields = fields ?? throw new ArgumentNullException(nameof(fields));
		private readonly Dictionary<string, DataTableField> _fieldsMap = fields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

		#region IFieldsCollection Members

		public int Count => _fields.Length;

		public IDbField this[string index] => _fieldsMap[index];

		public IDbField this[int index] => _fields[index];

		#endregion

		#region IEnumerable Members

		public IEnumerator<IDbField> GetEnumerator() => ((IEnumerable<IDbField>)_fields).GetEnumerator();

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _fields.GetEnumerator();

		#endregion
	}

	private class DataTableField: IDbField
	{
		private readonly string _name;
		private readonly RowsCollection _records;
		private readonly DbType _type;
		private readonly int _columnIndex;

		public DataTableField(string name, Type type, RowsCollection records, int columnIndex)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			_name = name ?? throw new ArgumentNullException(nameof(name));
			_records = records ?? throw new ArgumentNullException(nameof(records));

			int c = (int)System.Type.GetTypeCode(type);
			if (c > 2 && c < __typeCodeMap.Length)
				_type = __typeCodeMap[c];
			else if (type == typeof(byte[]))
				_type = DbType.Binary;
			else if (type == typeof(Guid))
				_type = DbType.Guid;
			else
				_type = DbType.Object;
			_columnIndex = columnIndex;
		}

		#region IField Members

		public string Name => _name;

		public object? Value
		{
			get => _records.Row[_columnIndex];
			set => _records.Row[_columnIndex] = value;
		}

		public DbType Type => _type;
		private static readonly DbType[] __typeCodeMap =
		[
			(DbType)(-1), (DbType)(-1), (DbType)(-1),
			DbType.Boolean, DbType.String,
			DbType.SByte, DbType.Byte,
			DbType.Int16, DbType.UInt16,
			DbType.Int32, DbType.UInt32,
			DbType.Int64, DbType.UInt64,
			DbType.Single, DbType.Double, DbType.Decimal,
			DbType.DateTime,
			(DbType)(-1),
			DbType.String
		];

		public bool? GetBoolean()
		{
			object? value = _records.Row[_columnIndex];
			return value == null ? null: Convert.ToBoolean(value, CultureInfo.InvariantCulture);
		}

		public short? GetInt16()
		{
			object? value = _records.Row[_columnIndex];
			return value == null ? null: Convert.ToInt16(value, CultureInfo.InvariantCulture);
		}

		public int? GetInt32()
		{
			object? value = _records.Row[_columnIndex];
			return value == null ? null: Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		public long? GetInt64()
		{
			object? value = _records.Row[_columnIndex];
			return value == null ? null: Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}

		public double? GetDouble()
		{
			object? value = _records.Row[_columnIndex];
			return value == null ? null: Convert.ToDouble(value);
		}

		public DateTime? GetDateTime()
		{
			object? value = _records.Row[_columnIndex];
			return value == null ? null: Convert.ToDateTime(value, CultureInfo.InvariantCulture);
		}

		public string? GetString()
		{
			object? value = _records.Row[_columnIndex];
			return value == null ? null: Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		public decimal? GetDecimal()
		{
			object? value = _records.Row[_columnIndex];
			return value == null ? null: Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		}

		public byte[]? GetBytes()
		{
			object? value = _records.Row[_columnIndex];
			return (byte[]?)value;
		}

		public Guid? GetGuid()
		{
			return _records.Row[_columnIndex] switch
			{
				null => null,
				Guid g => g,
				string s => new Guid(s),
				byte[] b => new Guid(b),
				_ => throw new InvalidOperationException(),
			};
		}

		public RowVersion? GetRowVersion()
		{
			return _records.Row[_columnIndex] switch
			{
				null => null,
				byte[] b => new RowVersion(b),
				long l => new RowVersion((ulong)l),
				ulong ul => new RowVersion(ul),
				_ => throw new InvalidOperationException()
			};
		}
		#endregion
	}
}
