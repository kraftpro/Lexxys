#if NET5_0_OR_GREATER

using System.Buffers;
using System.Data;
using System.Runtime.CompilerServices;

using Lexxys.Testing;

namespace Lexxys.Tests.Extensions
{
	[TestClass]
	public class BufferWriterExtensionsTests
	{
		[TestMethod]
		public void Write_Int()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = Rand.Int();
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var writen = span.Read<int>();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void Write_NullableInt()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				int? value = Rand.Int();
				if (Rand.Int() % 2 == 0)
					value = null;
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var writen = span.ReadNullable<int>();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void Write_Byte()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = (byte)(Rand.Int() % 256);
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				Assert.AreEqual(sizeof(byte), span.Length, $"0. i:{i}, v:'{value}'");
				var writen = span.ReadByte();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void Write_String()
		{
			var strGen = R.Ascii(0, ushort.MaxValue);

			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				string value = strGen.NextValue();
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var writen = span.ReadString();
				Assert.AreEqual(value.Length, writen.Length, $"i:{i}, len1:{value.Length}, len2:'{writen.Length}'");
				Assert.AreEqual(value, writen, $"i:{i}, len:{value.Length}, value:'{value}'");
				Assert.AreEqual(0, span.Length, $"i:{i}, len:{value.Length}, value:'{value}'");
			}
		}

		[TestMethod]
		public void Write_Null_String()
		{
			var buffer = new ArrayBufferWriter<byte>();
			string value = null;
			buffer.Write(value);
			var span = buffer.WrittenSpan;

			Assert.AreEqual(1, span.Length);
			Assert.AreEqual((byte)0, span[0]);

			var writen = span.ReadString();
			Assert.IsNull(writen);
			Assert.AreEqual(0, span.Length);
		}

		[TestMethod]
		public void WritePacked_Uint()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = (uint)(Rand.Long() % (1L + uint.MaxValue));
				buffer.WritePacked(value);
				var span = buffer.WrittenSpan;
				var writen = span.ReadPackedUInt();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void WritePacked_Int()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = Rand.Int() - Rand.Int();
				buffer.WritePacked(value);
				var span = buffer.WrittenSpan;
				var writen = span.ReadPackedInt();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void WritePacked_Ulong()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = (ulong)Rand.Long();
				buffer.WritePacked(value);
				var span = buffer.WrittenSpan;
				var writen = span.ReadPackedULong();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void WritePacked_Long()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = Rand.Long() - Rand.Long();
				buffer.WritePacked(value);
				var span = buffer.WrittenSpan;
				var writen = span.ReadPackedLong();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void Write_Struct_Null()
		{
			// arrange
			var buffer = new ArrayBufferWriter<byte>();
			DateTime? value = null;

			// act
			buffer.Write(value);

			// assert
			var span = buffer.WrittenSpan;
			Assert.AreEqual(1, span.Length);
			Assert.AreEqual((byte)0, span[0]);

			var writen = span.ReadNullable<DateTime>();
			Assert.IsNull(writen);
			Assert.AreEqual(0, span.Length);
		}

		[TestMethod]
		public void Write_Long()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = Rand.Long() - Rand.Long();
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var writen = span.Read<long>();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void Write_Guid()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = Guid.NewGuid();
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var writen = span.Read<Guid>();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		public void Write_DateTime()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				DateTime? value = i == 0 ? null: new DateTime(Rand.Long(DateTime.MaxValue.Ticks + 1));
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var writen = span.ReadNullable<DateTime>();
				Assert.AreEqual(value, writen, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void Write_Bool(bool value)
		{
			// arrange
			var buffer = new ArrayBufferWriter<byte>();

			// act
			buffer.Write(value);

			// assert
			var span = buffer.WrittenSpan;
			Assert.AreEqual(1, span.Length);
			Assert.AreEqual((byte)(value ? 1 : 0), span[0]);
			var actual = span.Read<bool>();
			Assert.AreEqual(value, actual);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		[DataRow(null)]
		public void Write_NullableBool(bool? value)
		{
			// arrange
			var buffer = new ArrayBufferWriter<byte>();

			// act
			buffer.Write(value);

			// assert
			var span = buffer.WrittenSpan;
			Assert.AreEqual(value == null ? 1: 2, span.Length);
			Assert.AreEqual((byte)(value == null ? 0: 1), span[0]);
			if (value != null)
				Assert.AreEqual((byte)(value.Value ? 1: 0), span[1]);
			var actual = span.ReadNullable<bool>();
			Assert.AreEqual(value, actual);
		}
	}
}

#endif
