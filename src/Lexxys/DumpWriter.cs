// Lexxys Infrastructural library.
// file: DumpWriter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys
{
	/// <summary>
	/// Represents a writer to create a text dump of objects.
	/// </summary>
	public abstract class DumpWriter
	{
		/// <summary>
		/// The string which represents null value.
		/// </summary>
		public const string NullValue = "(null)";
		/// <summary>
		/// Maximum stream capacity by default.
		/// </summary>
		public const int DefaultMaxCapacity = Int32.MaxValue;
		/// <summary>
		/// Maximum depth of traversing of the dumping objects by default.
		/// </summary>
		public const int DefaultMaxDepth = 5;
		/// <summary>
		/// Maximum supported depth of traversing of the dumping objects.
		/// </summary>
		public const int MaxMaxDepth = 100;

		public const int DefaultStringLimit = 512;
		public const int DefaultBlobLimit = 64;
		public const int DefaultArrayLimit = 64;

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpWriter"/> class.
		/// </summary>
		/// <param name="maxCapacity">Maximum numer of characters to write.</param>
		/// <param name="maxDepth">Maximum depth of traversing of the dumping objects</param>
		/// <param name="stringLimit">Maximum length of string to dump</param>
		/// <param name="blobLimit">Maximum length of byte array to dump</param>
		/// <param name="arrayLimit">Maximum number of array elements to dump</param>
		protected DumpWriter(int maxCapacity, int maxDepth, int stringLimit, int blobLimit, int arrayLimit)
		{
			MaxCapacity = maxCapacity <= 0 ? DefaultMaxCapacity : maxCapacity;
			MaxDepth = maxDepth <= 0 ? DefaultMaxDepth : Math.Min(maxDepth, MaxMaxDepth);
			Left = MaxCapacity;
			Observed = new List<object>();
			StringLimit = stringLimit <= 0 ? DefaultStringLimit: stringLimit;
			BlobLimit = blobLimit <= 0 ? DefaultBlobLimit: blobLimit;
			ArrayLimit = arrayLimit <= 0 ? DefaultArrayLimit: arrayLimit;
		}

		private List<object> Observed { get; }
		/// <summary>
		/// Maximum capacity in characters of the dumping stream.
		/// </summary>
		public int MaxCapacity { get; }
		/// <summary>
		/// Maximum depth of dumping objects.
		/// </summary>
		public int MaxDepth { get; private set; }
		/// <summary>
		/// Current depth of dumping objects.
		/// </summary>
		public int Depth { get; private set; }
		/// <summary>
		/// Maximum length of string to dump
		/// </summary>
		public int StringLimit { get; }
		/// <summary>
		/// >Maximum length of byte array to dump
		/// </summary>
		public int BlobLimit { get; }
		/// <summary>
		/// >Maximum number of array elements to dump
		/// </summary>
		public int ArrayLimit { get; }
		/// <summary>
		/// The remaining capacity of the dumping stream.
		/// </summary>
		public int Left { get; protected set; }
		/// <summary>
		/// Indicates whether to format dumped objects values.
		/// </summary>
		public bool Format { get; set; }
		/// <summary>
		/// String used for indenting.
		/// </summary>
		public string Tab { get; set; }

		/// <summary>
		/// Writes <see cref="String"/> value to the stream.
		/// </summary>
		/// <param name="text">The value to write.</param>
		/// <returns></returns>
		public abstract DumpWriter Text(string text);
		/// <summary>
		/// Writes <see cref="Char"/> value to the stream.
		/// </summary>
		/// <param name="text">The value to write.</param>
		public abstract DumpWriter Text(char text);

		/// <summary>
		/// Creates a new <see cref="DumpWriter"/> using the specified <see cref="TextWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> to write as dump.</param>
		/// <param name="maxCapacity">Maximum numer of characters to write.</param>
		/// <param name="maxDepth">Maximum depth of traversing of the dumping objects</param>
		/// <param name="stringLimit">Maximum length of string portion to dump</param>
		/// <param name="blobLimit">Maximum length of byte array portion to dump</param>
		/// <param name="arrayLimit">Maximum number of array elements dump</param>
		/// <returns></returns>
		public static DumpWriter Create(TextWriter writer, int maxCapacity = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0)
		{
			return new DumpStreamWriter(writer, maxCapacity, maxDepth, stringLimit, blobLimit, arrayLimit);
		}

		/// <summary>
		/// Creates a new <see cref="DumpWriter"/> using the specified <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="writer">The <see cref="StringBuilder"/> to write as dump.</param>
		/// <param name="maxCapacity">Maximum numer of characters to write.</param>
		/// <param name="maxDepth">Maximum depth of traversing of the dumping objects</param>
		/// <param name="stringLimit">Maximum length of string portion to dump</param>
		/// <param name="blobLimit">Maximum length of byte array portion to dump</param>
		/// <param name="arrayLimit">Maximum number of array elements dump</param>
		/// <returns></returns>
		public static DumpWriter Create(StringBuilder writer, int maxCapacity = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0)
		{
			return new DumpStringWriter(writer, maxCapacity, maxDepth, stringLimit, blobLimit, arrayLimit);
		}

		/// <summary>
		/// Creates a new <see cref="DumpWriter"/> using a new <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="maxCapacity">Maximum numer of characters to write.</param>
		/// <param name="maxDepth">Maximum depth of traversing of the dumping objects</param>
		/// <param name="stringLimit">Maximum length of string portion to dump</param>
		/// <param name="blobLimit">Maximum length of byte array portion to dump</param>
		/// <param name="arrayLimit">Maximum number of array elements dump</param>
		/// <returns></returns>
		public static DumpWriter Create(int maxCapacity = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0)
		{
			return new DumpStringWriter(new StringBuilder(), maxCapacity, maxDepth, stringLimit, blobLimit, arrayLimit);
		}

		public static string ToString(object value, int maxCapacity = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0, bool pretty = false)
		{
			return Create(maxCapacity, maxDepth, stringLimit, blobLimit, arrayLimit).Pretty(pretty, "  ").DumpIt(value, ignoreToString: true).ToString();
		}

		/// <summary>
		/// Sets whether to format dumped objects values.
		/// </summary>
		/// <param name="formatted"></param>
		/// <param name="tab"></param>
		/// <returns></returns>
		public DumpWriter Pretty(bool formatted = true, string tab = null)
		{
			Format = formatted;
			Tab = tab;
			return this;
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(bool value)
		{
			return Text(value ? "true": "false");
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(char value)
		{
			Text("'");
			if (Char.IsControl(value))
			{
				var s = EscapeChar(value);
				if (s.Length > Left)
					return this;
				Text(s);
			}
			else if (value == '\'' || value == '\\')
			{
				if (Left < 2)
					return this;
				Text("\\" + value.ToString());
			}
			else
			{
				Text(value);
			}
			return Text('\'');
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(byte value)
		{
			Text("0x");
			Text(__hex[value >> 4]);
			Text(__hex[value & 15]);
			return this;
		}
		private static readonly char[] __hex = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(sbyte value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(short value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(ushort value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(int value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(uint value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(long value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(ulong value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(float value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(double value)
		{
			return Text(value.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(decimal value)
		{
			return Text(value.ToString($"F{value.GetScale()}", CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(string value)
		{
			if (value == null)
				return Text(NullValue);
			if (value.Length > StringLimit)
				value = value.Substring(0, StringLimit);
			Text('"');
			foreach (char c in value)
			{
				if (Left == 0)
					return this;
				if (Char.IsControl(c))
				{
					var s = EscapeChar(c);
					if (s.Length > Left)
						return this;
					Text(s);
				}
				else if (c == '"' || c == '\\')
				{
					if (Left < 2)
						return this;
					Text("\\" + c.ToString());
				}
				else
				{
					Text(c);
				}
			}
			return Left > 0 ? Text('"'): this;
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(IEnumerable<byte> value)
		{
			if (value == null)
				return Text(NullValue);

			Text("0x");
			int i = 0;
			foreach (var v in value)
			{
				if (Left <= 0 || i >= BlobLimit)
					return this;
				Text(__hex[v >> 4]);
				Text(__hex[v & 15]);
			}
			return this;
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(TimeSpan value)
		{
			return Text(value.ToString());
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(DateTime value)
		{
			string v = value.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
			return Text(v.EndsWith(" 00:00:00.0000000", StringComparison.Ordinal) ? v.Substring(0, 10): v);
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(BitArray value)
		{
			if (value == null)
				return Text(NullValue);
			Text('[');
			int rest = Math.Min(value.Length, Left - 1);
			for (int i = 0; i < rest; ++i)
			{
				Text(value[i] ? '1' : '0');
			}
			return Text(']');
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <returns></returns>
		public DumpWriter Dump(IDump value)
		{
			return Dump(value, false);
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <param name="ignoreToString">Don't use ToString method for dump</param>
		/// <returns></returns>
		public DumpWriter Dump(IEnumerable value, bool ignoreToString = false)
		{
			if (value == null)
				return Text(NullValue);
			if (value is string s)
				return Dump(s);
			if (value is IDictionary d)
				return Dump(d, ignoreToString);
			if (value is BitArray b)
				return Dump(b);
			if (value is IEnumerable<byte> y)
				return Dump(y);

			if (Observed.Contains(value))
				return Text($"^{Observed.IndexOf(value) + 1}");

			Observed.Add(value);
			var depth = Depth++;
			try
			{
				int i = 0;
				var pad = '[';
				foreach (var item in value)
				{
					if (Left <= 0)
						return this;
					if (++i > ArrayLimit)
						break;
					Text(pad);
					Dump(item, ignoreToString);
					pad = ',';
				}
				return Text(pad == '[' ? "[]": "]");
			}
			finally
			{
				Depth = depth;
			}
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <param name="ignoreToString">Don't use ToString method for dump</param>
		/// <returns></returns>
		public DumpWriter Dump(IEnumerator value, bool ignoreToString = false)
		{
			if (value == null)
				return Text(NullValue);

			if (Observed.Contains(value))
				return Text($"^{Observed.IndexOf(value) + 1}");

			Observed.Add(value);
			var depth = Depth++;
			try
			{
				int i = 0;
				var pad = '[';
				while (value.MoveNext())
				{
					if (Left <= 0)
						return this;
					if (++i > ArrayLimit)
						break;
					Text(pad);
					Dump(value.Current, ignoreToString);
					pad = ',';
				}
				return Text(pad == '[' ? "[]" : "]");
			}
			finally
			{
				Depth = depth;
			}
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <param name="ignoreToString">Don't use ToString method for dump</param>
		/// <returns></returns>
		public DumpWriter Dump(IDictionary value, bool ignoreToString = false)
		{
			if (value == null)
				return Text(NullValue);

			if (Observed.Contains(value))
				return Text($"^{Observed.IndexOf(value) + 1}");
			Observed.Add(value);
			var depth = Depth++;
			try
			{
				int i = 0;
				var pad = '[';
				IDictionaryEnumerator enumerator = value.GetEnumerator();
				while (enumerator.MoveNext() && Left > 0)
				{
					if (Left <= 0)
						return this;
					if (++i > ArrayLimit)
						break;
					Text(pad);
					Dump(enumerator.Key, ignoreToString);
					Text('=');
					Dump(enumerator.Value, ignoreToString);
					pad = ',';
				}
				return Text(pad == '[' ? "[]": "]");
			}
			finally
			{
				Depth = depth;
			}
		}

		/// <summary>
		/// Dumps the specified <paramref name="value"/> to the stream.
		/// </summary>
		/// <param name="value">The value to dump.</param>
		/// <param name="ignoreToString">Don't use ToString method for dump</param>
		/// <returns></returns>
		public DumpWriter Dump(DictionaryEntry value, bool ignoreToString = false)
		{
			return Text("{Key=").Dump(value.Key, ignoreToString).Text(",Value=").Dump(value.Value, ignoreToString).Text('}');
		}

		/// <summary>
		/// Dumps the object value using reflection or the <see cref="IDump"/> interface if it is implemented by the object.
		/// </summary>
		/// <param name="value">The value to dump</param>
		/// <param name="ignoreToString">Don't use ToString method for dump</param>
		/// <returns></returns>
		public DumpWriter Dump(object value, bool ignoreToString = false)
		{
			return DumpIt(value, ignoreToString: ignoreToString);
		}

		/// <summary>
		/// Dumps the object value using reflection only.
		/// </summary>
		/// <param name="value">The value to dump</param>
		/// <param name="ignoreToString">Don't use ToString method for dump</param>
		/// <returns></returns>
		public DumpWriter DumpObject(object value, bool ignoreToString = false)
		{
			return DumpIt(value, skipIDump: true, ignoreToString: ignoreToString);
		}

		/// <summary>
		/// Dumps only content (exclude header and footer) of the object value using reflection only.
		/// </summary>
		/// <param name="value">The value to dump</param>
		/// <param name="ignoreToString">Don't use ToString method for dump</param>
		/// <returns></returns>
		public DumpWriter DumpObjectContent(object value, bool ignoreToString = false)
		{
			return DumpIt(value, skipIDump: true, contentOnly: true, ignoreToString: ignoreToString);
		}

		/// <summary>
		/// Dumps only content (exclude header and footer) of the object value using reflection or the <see cref="IDump"/> interface if it is implemented by the object.
		/// </summary>
		/// <param name="value">The value to dump</param>
		/// <param name="ignoreToString">Don't use ToString method for dump</param>
		/// <returns></returns>
		public DumpWriter DumpContent(object value, bool ignoreToString = false)
		{
			return DumpIt(value, contentOnly: true, ignoreToString: ignoreToString);
		}

		/// <summary>
		/// Dumps only content (exclude header and footer) of the object.
		/// </summary>
		/// <param name="value">The value to dump</param>
		/// <returns></returns>
		public DumpWriter DumpContent(IDump value)
		{
			return Dump(value, true);
		}

		#region Item, Then

		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, object value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, DictionaryEntry value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, IDictionary value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, IEnumerator value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, IEnumerable value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, IDump value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, BitArray value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, DateTime value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, TimeSpan value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, string value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, decimal value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, IEnumerable<byte> value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, ulong value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, double value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, char value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, byte value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, sbyte value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, bool value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, ushort value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, int value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, uint value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, long value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, short value) => Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Item(string name, float value) => Text(name).Text('=').Dump(value);

		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, object value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, DictionaryEntry value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, IDictionary value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, IEnumerator value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, IEnumerable value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, IDump value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, BitArray value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, DateTime value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, TimeSpan value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, string value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, decimal value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, IEnumerable<byte> value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, ulong value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, double value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, char value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, byte value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, sbyte value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, bool value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, ushort value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, int value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, uint value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, long value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, short value) => Text(',').Text(name).Text('=').Dump(value);
		/// <summary>
		/// Dumps item in form: ,Name=Value
		/// </summary>
		/// <param name="name">Name of the item</param>
		/// <param name="value">Value of the item</param>
		/// <returns></returns>
		public DumpWriter Then(string name, float value) => Text(',').Text(name).Text('=').Dump(value);

		#endregion

		private DumpWriter Dump(IDump value, bool contentOnly)
		{
			if (value == null)
				return Text(NullValue);
			var byref = value.GetType().IsClass;
			if (byref && Observed.Contains(value))
				return Text($"^{Observed.IndexOf(value) + 1}");
			if (Depth > MaxDepth)
				return Text("...");
			if (byref)
				Observed.Add(value);
			int depth = Depth++;
			try
			{
				return contentOnly ? value.DumpContent(this): value.Dump(this);
			}
			finally
			{
				Depth = depth;
			}
		}

		private DumpWriter DumpIt(object value, bool skipIDump = false, bool contentOnly = false, bool ignoreToString = false)
		{
			if (Depth > MaxDepth)
				return Text("...");
			if (value == null)
				return Text(NullValue);

			if (!skipIDump && value is IDump u)
				return Dump(u, contentOnly);

			if (value is IConvertible ic)
			{
				if (value is Enum)
					return Text(value.ToString());
				return ic.GetTypeCode() switch
				{
					TypeCode.DBNull or TypeCode.Empty => Text(NullValue),
					TypeCode.Boolean => Dump((bool)value),
					TypeCode.Char => Dump((char)value),
					TypeCode.SByte => Dump((sbyte)value),
					TypeCode.Byte => Dump((byte)value),
					TypeCode.Int16 => Dump((short)value),
					TypeCode.UInt16 => Dump((ushort)value),
					TypeCode.Int32 => Dump((int)value),
					TypeCode.UInt32 => Dump((uint)value),
					TypeCode.Int64 => Dump((long)value),
					TypeCode.UInt64 => Dump((ulong)value),
					TypeCode.Single => Dump((float)value),
					TypeCode.Double => Dump((double)value),
					TypeCode.Decimal => Dump((Decimal)value),
					TypeCode.DateTime => Dump((DateTime)value),
					TypeCode.String => Dump((string)value),
					_ => Text(ic.ToString(CultureInfo.InvariantCulture)),
				};
			}

			if (value is IEnumerable e)
				return Dump(e, ignoreToString);
			if (value is IEnumerator r)
				return Dump(r, ignoreToString);
			if (value is TimeSpan span)
				return Dump(span);
			if (value is DictionaryEntry entry)
				return Dump(entry, ignoreToString);

			int depth = Depth++;

			try
			{
				Type type = value.GetType();
				if (type.IsClass)
				{
					if (Observed.Contains(value))
						return Text($"^{Observed.IndexOf(value) + 1}");
					Observed.Add(value);
				}

				char pad;
				if (contentOnly)
				{
					pad = '\0';
				}
				else
				{
					var typeName = type.ToString();
					string shortName = ShortName(typeName);
					if (!ignoreToString)
					{
						var svalue = value.ToString();
						if (shortName.StartsWith("KeyValuePair<", StringComparison.Ordinal))
							typeName = "<>";
						else if (svalue != typeName)
							return Text(shortName).Text(':').Text(svalue);
					}
					if (typeName.Contains("<>"))
					{
						pad = '{';
					}
					else
					{
						pad = ':';
						Text("{" + shortName);
					}
				}
				foreach (var item in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField))
				{
					if (Left == 0)
						return this;
					if (pad != '\0')
						Text(pad);
					NewLine();
					Text(item.Name);
					Text('=');
					DumpIt(item.GetValue(value), skipIDump, false, ignoreToString);
					pad = ',';
				}
				foreach (var item in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
				{
					if (item.CanRead &&
						item.GetIndexParameters().Length == 0 &&
						!item.PropertyType.IsGenericTypeDefinition &&
						!item.PropertyType.IsGenericParameter)
					{
						if (Left == 0)
							return this;
						try
						{
							object v = item.GetValue(value);
							if (pad != '\0')
								Text(pad);
							NewLine();
							Text(item.Name);
							Text('=');
							DumpIt(v, skipIDump, false, ignoreToString);
							pad = ',';
						}
						#pragma warning disable CA1031 // Do not catch general exception types
						catch
						{
							// ignore all internal exceptions
						}
						#pragma warning restore CA1031 // Do not catch general exception types
					}
				}

				if (pad != ',')
					Text($"{value}");
				if (!contentOnly && pad != '{')
				{
					NewLine();
					return Text('}');
				}
				return this;
			}
			finally
			{
				Depth = depth;
			}
		}

		public DumpWriter NewLine()
		{
			if (Format)
				Text(Environment.NewLine + Repeat(Tab, Depth));
			Console.WriteLine();
			return this;
		}

		private static string Repeat(string value, int count)
		{
			if (count == 0)
				return "";
			if (String.IsNullOrEmpty(value))
				return new string('\t', count);
			if (count == 1)
				return value;
			if (value.Length == 1)
				return new string(value[0], count);
			var xx = new StringBuilder(value);
			for (int i = 1; i < count; ++i)
			{
				xx.Append(value);
			}
			return xx.ToString();
		}

		private static string ShortName(string typeName)
		{
			return Regex.Replace(typeName.Replace('[', '<').Replace(']', '>'), @"([a-zA-Z]+\.)+|`[1-9]", "");
		}

		private static string EscapeChar(char value)
		{
			return value switch
			{
				'\0' => "\\0",
				'\a' => "\\a",
				'\b' => "\\b",
				'\f' => "\\f",
				'\n' => "\\n",
				'\r' => "\\r",
				'\t' => "\\t",
				'\v' => "\\v",
				_ => value < '\x100'
					? "\\x" + ((int)value).ToString("X2", CultureInfo.InvariantCulture)
					: "\\u" + ((int)value).ToString("X4", CultureInfo.InvariantCulture),
			};
		}
	}

	/// <summary>
	/// Represents <see cref="DumpWriter"/> that uses <see cref="TextWriter"/> to write a dump.
	/// </summary>
	public class DumpStreamWriter: DumpWriter
	{
		private readonly TextWriter _w;

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpStreamWriter"/> class.
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> to write a dump.</param>
		/// <param name="maxCapacity">Maximum numer of characters to write.</param>
		/// <param name="maxDepth">Maximum depth of traversing of the dumping objects</param>
		/// <param name="stringLimit">Maximum length of string portion to dump</param>
		/// <param name="blobLimit">Maximum length of byte array portion to dump</param>
		/// <param name="arrayLimit">Maximum number of array elements dump</param>
		public DumpStreamWriter(TextWriter writer, int maxCapacity = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0)
			: base(maxCapacity, maxDepth, stringLimit, blobLimit, arrayLimit)
		{
			_w = writer ?? throw new ArgumentNullException(nameof(writer));
		}

		/// <inheritdoc />
		public override DumpWriter Text(string text)
		{
			if (text == null)
				text = NullValue;
			int length = text.Length;
			if (Left < length)
			{
				if (Left <= 0)
					return this;
				_w.Write(text.ToCharArray(0, Left));
				Left = 0;
			}
			else
			{
				_w.Write(text);
				Left -= length;
			}
			return this;
		}

		/// <inheritdoc />
		public override DumpWriter Text(char text)
		{
			if (Left <= 0)
				return this;
			_w.Write(text);
			--Left;
			return this;
		}
	}

	/// <summary>
	/// Represents <see cref="DumpWriter"/> that uses <see cref="StringBuilder"/> to write a dump.
	/// </summary>
	public class DumpStringWriter: DumpWriter
	{
		private readonly StringBuilder _w;

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpStringWriter"/> class.
		/// </summary>
		/// <param name="maxCapacity">Maximum numer of characters to write.</param>
		/// <param name="maxDepth">Maximum depth of traversing of the dumping objects</param>
		/// <param name="stringLimit">Maximum length of string portion to dump</param>
		/// <param name="blobLimit">Maximum length of byte array portion to dump</param>
		/// <param name="arrayLimit">Maximum number of array elements dump</param>
		public DumpStringWriter(int maxCapacity = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0)
			: base(maxCapacity, maxDepth, stringLimit, blobLimit, arrayLimit)
		{
			_w = new StringBuilder();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpStreamWriter"/> class.
		/// </summary>
		/// <param name="writer">The <see cref="StringWriter"/> to write a dump.</param>
		/// <param name="maxCapacity">Maximum numer of characters to write.</param>
		/// <param name="maxDepth">Maximum depth of traversing of the dumping objects</param>
		/// <param name="stringLimit">Maximum length of string portion to dump</param>
		/// <param name="blobLimit">Maximum length of byte array portion to dump</param>
		/// <param name="arrayLimit">Maximum number of array elements dump</param>
		public DumpStringWriter(StringBuilder writer, int maxCapacity = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0)
			: base(maxCapacity, maxDepth, stringLimit, blobLimit, arrayLimit)
		{
			_w = writer ?? new StringBuilder();
		}

		/// <inheritdoc />
		public override DumpWriter Text(string text)
		{
			if (text == null)
				text = NullValue;
			int length = text.Length;
			if (Left < length)
			{
				if (Left <= 0)
					return this;
				_w.Append(text.ToCharArray(0, Left));
				Left = 0;
			}
			else
			{
				_w.Append(text);
				Left -= length;
			}
			return this;
		}

		/// <inheritdoc />
		public override DumpWriter Text(char text)
		{
			if (Left <= 0)
				return this;
			_w.Append(text);
			--Left;
			return this;
		}

		/// <summary>
		/// Converts the value of this instance to a <see cref="String"/>.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _w.ToString();
		}
	}
}
