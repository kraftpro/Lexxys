using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Lexxys.Con;

internal static class DumpTest
{
	public static void Run(string[] args)
	{
		var ta = Factory.CreateAccessor(typeof(string), true, true);
		var opt = new DumpWriterOptions
		{
			Compact = true,
			FormatIndentation = true,
			MaxDepth = 22,
			MaxLength = 44*1024,
			StringMaxLength = 32,
			ArrayMaxLength = 4,
			EllipsisString = "...",
			FieldSeparator = ';',
			Level = ObjectDumpLevel.Debug,
		};
		var x = new XmlDumpStringWriter(new XmlDumpWriterOptions(opt) { UseAttributesForPrimitives = true }); // new JsonDumpStringWriter(opt);
		var a = x.Write("array", new object[] { 1, "<string/>", DBNull.Value }).ToString();
		Console.Clear();
		Console.WriteLine(a);
		Console.WriteLine();

		var y = new XmlDumpStringWriter(new XmlDumpWriterOptions(opt) { UseAttributesForPrimitives = true });
		var y1 = new XmlDumpStringWriter(new XmlDumpWriterOptions() { UseAttributesForPrimitives = true });

		var b = y.Write("culture", CultureInfo.CurrentCulture).ToString();
		Console.WriteLine(b);
		Console.WriteLine();

		var xx = new
		{
			Name = "Test",
			IntValue = 123,
			DateValue = DateTime.Now,
			Items = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
			MoneyValue = new Money(1234.56m, Currency.Usd),
			ComplexValue = new Complex(1.2, 3.4),
		};
		var z = new XmlDumpStringWriter(new XmlDumpWriterOptions(opt) { UseAttributesForPrimitives = true });
		var c = z.Write("culture", xx).ToString();
		Console.WriteLine(c);
		Console.WriteLine();
	}
}
