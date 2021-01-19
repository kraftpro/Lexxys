// Lexxys Infrastructural library.
// file: RowsCollection.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Lexxys.Data
{
	public interface IFieldsCollection: IEnumerable<AField>
	{
		int Count { get; }
		AField this[string index] { get; }
		AField this[int index] { get; }
	}

	public abstract class AField
	{
		public abstract string Name { get; }
		public abstract object Value { get; set; }
		public abstract DbType Type { get; }

		public abstract bool? GetBoolean();
		public abstract short? GetInt16();
		public abstract int? GetInt32();
		public abstract long? GetInt64();
		public abstract double? GetDouble();
		public abstract DateTime? GetDateTime();
		public abstract string GetString();
		public abstract decimal? GetDecimal();
		public abstract byte[] GetBytes();
		public abstract Guid? GetGuid();
		public abstract RowVersion? GetRowVersion();

		public static explicit operator bool(AField value)
		{
			return value.GetBoolean().Value;
		}
		public static explicit operator bool?(AField value)
		{
			return value.GetBoolean();
		}
		public static explicit operator short(AField value)
		{
			return value.GetInt16().Value;
		}
		public static explicit operator short?(AField value)
		{
			return value.GetInt16();
		}
		public static explicit operator int(AField value)
		{
			return value.GetInt32().Value;
		}
		public static explicit operator int?(AField value)
		{
			return value.GetInt32();
		}
		public static explicit operator long(AField value)
		{
			return value.GetInt64().Value;
		}
		public static explicit operator long?(AField value)
		{
			return value.GetInt64();
		}
		public static explicit operator decimal(AField value)
		{
			return value.GetDecimal().Value;
		}
		public static explicit operator decimal?(AField value)
		{
			return value.GetDecimal();
		}
		public static explicit operator double(AField value)
		{
			return value.GetDouble().Value;
		}
		public static explicit operator double?(AField value)
		{
			return value.GetDouble();
		}
		public static explicit operator DateTime(AField value)
		{
			return value.GetDateTime().Value;
		}
		public static explicit operator DateTime?(AField value)
		{
			return value.GetDateTime();
		}
		public static explicit operator byte[](AField value)
		{
			return value.GetBytes();
		}
		public static explicit operator string(AField value)
		{
			return value.GetString();
		}
		public static explicit operator Guid(AField value)
		{
			return value.GetGuid().Value;
		}
		public static explicit operator Guid?(AField value)
		{
			return value.GetGuid();
		}
		public static explicit operator RowVersion(AField value)
		{
			return value.GetRowVersion().Value;
		}
		public static explicit operator RowVersion?(AField value)
		{
			return value.GetRowVersion();
		}
	}

	public sealed class RowsCollection
	{
		private readonly IFieldsCollection _fields;
		private readonly object[][] _data;
		private int _currentIndex;

		public RowsCollection(DataTable table)
		{
			if (table == null)
				throw EX.ArgumentNull(nameof(table));
			if (table == null)
				throw EX.ArgumentNull(nameof(table));

			_data = new object[table.Rows.Count][];
			for (int i = 0; i < _data.Length; ++i)
			{
				var row = table.Rows[i].ItemArray;
				for (int j = 0; j < row.Length; j++)
				{
					if (row[j] == Convert.DBNull)
						row[j] = null;
				}
				_data[i] = row;
			}
			_fields = new FieldsCollection(table.Columns.OfType<DataColumn>().Select(col => new DataTableField(col.ColumnName, col.DataType, this, col.Ordinal)).ToList());
		}

		private object[] Row
		{
			get
			{
				if (Eof)
					throw EX.InvalidOperation();
				//return _table.Rows[_currentIndex];
				return _data[_currentIndex];
			}
		}

		public bool Eof
		{
			get { return _currentIndex >= _data.Length; }
		}

		public int Count
		{
			get { return _data.Length; }
		}

		public IFieldsCollection Fields
		{
			get { return _fields; }
		}

		public void MoveFirst()
		{
			_currentIndex = 0;
		}

		public void MoveNext()
		{
			if (Eof)
				throw EX.InvalidOperation();
			++_currentIndex;
		}

		private class FieldsCollection: IFieldsCollection
		{
			private readonly IList<DataTableField> _fields;
			private readonly IDictionary<string, DataTableField> _fildsDict;

			public FieldsCollection(IList<DataTableField> fields)
			{
				_fields = fields ?? throw EX.ArgumentNull(nameof(fields));
				_fildsDict = _fields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
			}

			#region IFieldsCollection Members

			public int Count
			{
				get { return _fields.Count; }
			}

			public AField this[string index]
			{
				get { return _fildsDict[index]; }
			}

			public AField this[int index]
			{
				get { return _fields[index]; }
			}

			#endregion

			#region IEnumerable<IField> Members

			public IEnumerator<AField> GetEnumerator()
			{
				return _fields.GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return _fields.GetEnumerator();
			}

			#endregion
		}

		private class DataTableField: AField
		{
			private readonly string _name;
			private readonly RowsCollection _records;
			private readonly DbType _type;
			private readonly int _columnIndex;

			public DataTableField(string name, Type type, RowsCollection records, int columnIndex)
			{
				if (type == null)
					throw EX.ArgumentNull(nameof(type));
				
				_name = name ?? throw EX.ArgumentNull(nameof(name));
				_records = records ?? throw EX.ArgumentNull(nameof(records));

				int c = (int)System.Type.GetTypeCode(type);
				if (c > 2 && c < _typeCodeMap.Length)
					_type = _typeCodeMap[c];
				else if (type == typeof(byte[]))
					_type = DbType.Binary;
				else if (type == typeof(Guid))
					_type = DbType.Guid;
				else
					_type = DbType.Object;
				_columnIndex = columnIndex;
			}

			#region IField Members

			public override string Name
			{
				get { return _name; }
			}

			public override object Value
			{
				get { return _records.Row[_columnIndex]; }
				set { _records.Row[_columnIndex] = value; }
			}

			public override DbType Type
			{
				get { return _type; }
			}
			private static readonly DbType[] _typeCodeMap =
			{
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
			};

			public override bool? GetBoolean()
			{
				object value = _records.Row[_columnIndex];
				return value == null ? (bool?)null: Convert.ToBoolean(value, CultureInfo.InvariantCulture);
			}

			public override short? GetInt16()
			{
				object value = _records.Row[_columnIndex];
				return value == null ? (short?)null: Convert.ToInt16(value, CultureInfo.InvariantCulture);
			}

			public override int? GetInt32()
			{
				object value = _records.Row[_columnIndex];
				return value == null ? (int?)null: Convert.ToInt32(value, CultureInfo.InvariantCulture);
			}

			public override long? GetInt64()
			{
				object value = _records.Row[_columnIndex];
				return value == null ? (long?)null: Convert.ToInt64(value, CultureInfo.InvariantCulture);
			}

			public override double? GetDouble()
			{
				object value = _records.Row[_columnIndex];
				return value == null ? (double?)null: Convert.ToDouble(value);
			}

			public override DateTime? GetDateTime()
			{
				object value = _records.Row[_columnIndex];
				return value == null ? (DateTime?)null: Convert.ToDateTime(value, CultureInfo.InvariantCulture);
			}

			public override string GetString()
			{
				return Convert.ToString(_records.Row[_columnIndex]);
			}

			public override decimal? GetDecimal()
			{
				object value = _records.Row[_columnIndex];
				return value == null ? (decimal?)null: Convert.ToDecimal(value, CultureInfo.InvariantCulture);
			}

			public override byte[] GetBytes()
			{
				object value = _records.Row[_columnIndex];
				return (byte[])value;
			}

			public override Guid? GetGuid()
			{
				return _records.Row[_columnIndex] switch
				{
					null => null,
					Guid g => g,
					string s => new Guid(s),
					byte[] b => new Guid(b),
					_ => throw EX.InvalidOperation(),
				};
			}

			public override RowVersion? GetRowVersion()
			{
				return _records.Row[_columnIndex] switch
				{
					null => null,
					byte[] b => new RowVersion(b),
					long l => new RowVersion(l),
					_ => throw EX.InvalidOperation()
				};
			}
			#endregion
		}
	}
}
