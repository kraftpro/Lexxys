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
				var written = span.Read<int>();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
			}
		}

		[TestMethod]
		public void Write_NullableInt()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				int? value = i switch
				{
					0 => null,
					1 => int.MaxValue,
					2 => int.MinValue,
					_ => Rand.Item(1, -1) * Rand.Int()
				};
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var written = span.ReadNullable<int>();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
			}
		}

		[TestMethod]
		public void Write_Byte()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				byte value = i switch
				{
					0 => byte.MinValue,
					1 => byte.MaxValue,
					_ => (byte)Rand.Int(byte.MinValue, byte.MaxValue + 1)
				};
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				Assert.AreEqual(sizeof(byte), span.Length, $"0. i:{i}, v:'{value}'");
				var written = span.ReadByte();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
			}
		}

		[TestMethod]
		public void Write_String()
		{
			var str = R.Str(R.Chr(char.MinValue, char.MaxValue), 0, 999);

			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				string value = str.NextValue();
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var written = span.ReadString();
				Assert.IsNotNull(written);
				Assert.AreEqual(value.Length, written.Length, $"i:{i}, len1:{value.Length}, len2:'{written.Length}'");
				Assert.AreEqual(value, written, $"i:{i}, len:{value.Length}, value:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"i:{i}, len:{value.Length}, value:'{value}' w:'{written}'");
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

			var written = span.ReadString();
			Assert.IsNull(written);
			Assert.AreEqual(0, span.Length);
		}

		[TestMethod]
		public void WritePacked_Uint()
		{
			for (int i = 0; i < 1000; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = i switch
				{
					0 => 0u,
					1 => uint.MaxValue - 1,
					2 => uint.MaxValue,
					_ => (uint)((ulong)Rand.Long() & Mask(sizeof(uint)))
				};
				buffer.WritePacked(value);
				var span = buffer.WrittenSpan;
				var written = span.ReadPackedUInt();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}'");
			}
		}
		
		private static ulong Mask(int size) => ~0ul >> 8 * (sizeof(ulong) - size + Rand.Int(size));

		[TestMethod]
		public void WritePacked_Int()
		{
			for (int i = 0; i < 1000; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = i switch
				{
					0 => 0,
					1 => int.MaxValue - 1,
					2 => int.MaxValue,
					3 => int.MinValue + 1,
					4 => int.MinValue,
					_ => Rand.Item(-1, 1) * (Rand.Int() & (int)Mask(sizeof(int)))
				};
				buffer.WritePacked(value);
				var span = buffer.WrittenSpan;
				var written = span.ReadPackedInt();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
			}
		}

		[TestMethod]
		public void WritePacked_Ulong()
		{
			for (int i = 0; i < 1000; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = i switch
				{
					0 => 0ul,
					1 => ulong.MaxValue - 1,
					2 => ulong.MaxValue,
					_ => (ulong)Rand.Long() & Mask(sizeof(ulong))
				};
				buffer.WritePacked(value);
				var span = buffer.WrittenSpan;
				var written = span.ReadPackedULong();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
			}
		}

		[TestMethod]
		public void WritePacked_Long()
		{
			for (int i = 0; i < 1000; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				var value = i switch
				{
					0 => 0,
					1 => long.MaxValue - 1,
					2 => long.MaxValue,
					3 => long.MinValue + 1,
					4 => long.MinValue,
					_ => Rand.Item(-1, 1) * (Rand.Long() & (long)Mask(sizeof(long)))
				};
				buffer.WritePacked(value);
				var span = buffer.WrittenSpan;
				var written = span.ReadPackedLong();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
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

			var written = span.ReadNullable<DateTime>();
			Assert.IsNull(written);
			Assert.AreEqual(0, span.Length);
		}

		[TestMethod]
		public void Write_Long()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				long value = i switch
				{
					0 => 0,
					1 => long.MaxValue,
					2 => long.MinValue,
					_ => Rand.Item(-1, 1) * Rand.Long()
				};
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var written = span.Read<long>();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
			}
		}

		[TestMethod]
		public void Write_Guid()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				Guid value = i switch
				{
					0 => Guid.Empty,
					1 => new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
					_ => Guid.NewGuid()
				};
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var written = span.Read<Guid>();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
			}
		}

		[TestMethod]
		public void Write_DateTime()
		{
			for (int i = 0; i < 100; ++i)
			{
				var buffer = new ArrayBufferWriter<byte>();
				DateTime? value = i switch
				{
					0 => null,
					1 => DateTime.MinValue,
					2 => DateTime.MaxValue,
					_ => new DateTime(Rand.Long(DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks + 1))
				};
				buffer.Write(value);
				var span = buffer.WrittenSpan;
				var written = span.ReadNullable<DateTime>();
				Assert.AreEqual(value, written, $"1. i:{i}, v:'{value}' w:'{written}'");
				Assert.AreEqual(0, span.Length, $"2. i:{i}, v:'{value}' w:'{written}'");
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
			Assert.AreEqual((byte)(value ? 1: 0), span[0]);
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
