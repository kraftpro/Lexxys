// Lexxys Infrastructural library.
// file: DataRecordExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public static class DataRecordExtensions
	{
		public static unsafe long GetRowVersion(this IDataRecord record, int position)
		{
			if (record is null)
				throw new ArgumentNullException(nameof(record));

			var buffer = new byte[8];
			record.GetBytes(position, 0, buffer, 0, 8);
			fixed (byte* p = buffer)
			{
				return *(long*)p;
			}
		}

		public static byte[] GetBytes(this IDataRecord record, int position)
		{
			if (record is null)
				throw new ArgumentNullException(nameof(record));

			var buffer = new byte[record.GetBytes(position, 0, null, 0, 0)];
			record.GetBytes(position, 0, buffer, 0, buffer.Length);
			return buffer;
		}
	}
}


