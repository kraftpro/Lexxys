using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace Lexxys.Test.Con
{
	public class Dump
	{
		public string Name { get; set; }
		public object Value { get; set; }

		public static void Test()
		{
			var d = DumpWriter.Create();
			d.Item("Hello", "Hello");
			var j = JsonParser.Parse("{ a:123, b:true, c: { d:\"\", e:3333, f:[1,2,3,4,\"a\"]}}");
			var t = j.ToString(true, 1, 2);
			Console.WriteLine(t);
		}
	}

	public static class DumpWriterExtensions
	{
		public static DumpWriter Item(this DumpWriter writer, string name, object value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, DictionaryEntry value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, IDictionary value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, IEnumerator value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, IEnumerable value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, IDump value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, BitArray value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, DateTime value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, TimeSpan value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, string value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, decimal value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, byte[] value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, ulong value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, double value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, char value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, byte value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, sbyte value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, bool value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, ushort value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, int value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, uint value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, long value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, short value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Item(this DumpWriter writer, string name, float value)
		{
			return writer.Text(name).Text('=').Dump(value);
		}

		public static DumpWriter Then(this DumpWriter writer, string name, object value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, DictionaryEntry value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, IDictionary value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, IEnumerator value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, IEnumerable value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, IDump value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, BitArray value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, DateTime value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, TimeSpan value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, string value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, decimal value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, byte[] value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, ulong value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, double value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, char value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, byte value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, sbyte value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, bool value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, ushort value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, int value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, uint value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, long value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, short value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
		public static DumpWriter Then(this DumpWriter writer, string name, float value)
		{
			return writer.Text(',').Text(name).Text('=').Dump(value);
		}
	}
}
