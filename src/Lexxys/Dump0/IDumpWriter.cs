using System.Collections;

namespace Lexxys;

public interface IDumpWriter
{
	/// <summary>
	/// Maximum capacity in characters of the dumping stream.
	/// </summary>
	int MaxCapacity { get; }

	/// <summary>
	/// Maximum depth of dumping objects.
	/// </summary>
	int MaxDepth { get; }

	/// <summary>
	/// Current depth of dumping objects.
	/// </summary>
	int Depth { get; }

	/// <summary>
	/// Maximum length of string to dump
	/// </summary>
	int StringLimit { get; }

	/// <summary>
	/// Maximum length of byte array to dump
	/// </summary>
	int BlobLimit { get; }

	/// <summary>
	/// >Maximum number of array elements to dump
	/// </summary>
	int ArrayLimit { get; }

	/// <summary>
	/// Indicates whether to format dumped objects values.
	/// </summary>
	bool Format { get; set; }

	/// <summary>
	/// String used for indenting.
	/// </summary>
	string? Tab { get; set; }

	/// <summary>
	/// Writes <see cref="String"/> value to the stream.
	/// </summary>
	/// <param name="text">The value to write.</param>
	/// <returns></returns>
	DumpWriter Text(string? text);

	/// <summary>
	/// Writes <see cref="Char"/> value to the stream.
	/// </summary>
	/// <param name="text">The value to write.</param>
	DumpWriter Text(char text);

	/// <summary>
	/// Sets whether to format dumped objects values.
	/// </summary>
	/// <param name="formatted"></param>
	/// <param name="tab"></param>
	/// <returns></returns>
	DumpWriter Pretty(bool formatted = true, string? tab = null);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(bool value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(char value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(byte value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(sbyte value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(short value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(ushort value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(int value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(uint value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(long value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(ulong value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(float value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(double value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(decimal value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(string? value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(IEnumerable<byte>? value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(TimeSpan value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(DateTime value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(BitArray? value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <returns></returns>
	DumpWriter Dump(IDump? value);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <param name="ignoreToString">Don't use ToString method for dump</param>
	/// <returns></returns>
	DumpWriter Dump(IEnumerable? value, bool ignoreToString = false);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <param name="ignoreToString">Don't use ToString method for dump</param>
	/// <returns></returns>
	DumpWriter Dump(IEnumerator? value, bool ignoreToString = false);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <param name="ignoreToString">Don't use ToString method for dump</param>
	/// <returns></returns>
	DumpWriter Dump(IDictionary? value, bool ignoreToString = false);

	/// <summary>
	/// Dumps the specified <paramref name="value"/> to the stream.
	/// </summary>
	/// <param name="value">The value to dump.</param>
	/// <param name="ignoreToString">Don't use ToString method for dump</param>
	/// <returns></returns>
	DumpWriter Dump(DictionaryEntry value, bool ignoreToString = false);

	/// <summary>
	/// Dumps the object value using reflection or the <see cref="IDump"/> interface if it is implemented by the object.
	/// </summary>
	/// <param name="value">The value to dump</param>
	/// <param name="ignoreToString">Don't use ToString method for dump</param>
	/// <returns></returns>
	DumpWriter Dump(object? value, bool ignoreToString = false);

	/// <summary>
	/// Dumps the object value using reflection only.
	/// </summary>
	/// <param name="value">The value to dump</param>
	/// <param name="ignoreToString">Don't use ToString method for dump</param>
	/// <returns></returns>
	DumpWriter DumpObject(object? value, bool ignoreToString = false);

	/// <summary>
	/// Dumps only content (exclude header and footer) of the object value using reflection only.
	/// </summary>
	/// <param name="value">The value to dump</param>
	/// <param name="ignoreToString">Don't use ToString method for dump</param>
	/// <returns></returns>
	DumpWriter DumpObjectContent(object? value, bool ignoreToString = false);

	/// <summary>
	/// Dumps only content (exclude header and footer) of the object value using reflection or the <see cref="IDump"/> interface if it is implemented by the object.
	/// </summary>
	/// <param name="value">The value to dump</param>
	/// <param name="ignoreToString">Don't use ToString method for dump</param>
	/// <returns></returns>
	DumpWriter DumpContent(object? value, bool ignoreToString = false);

	/// <summary>
	/// Dumps only content (exclude header and footer) of the object.
	/// </summary>
	/// <param name="value">The value to dump</param>
	/// <returns></returns>
	DumpWriter DumpContent(IDump? value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, object? value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, DictionaryEntry value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, IDictionary value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, IEnumerator value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, IEnumerable value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, IDump value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, BitArray value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, DateTime value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, TimeSpan value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, string? value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, decimal value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, IEnumerable<byte> value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, ulong value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, double value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, char value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, byte value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, sbyte value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, bool value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, ushort value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, int value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, uint value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, long value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, short value);

	/// <summary>
	/// Dumps item in form Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Item(string name, float value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, object? value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, DictionaryEntry value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, IDictionary? value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, IEnumerator? value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, IEnumerable? value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, IDump? value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, BitArray? value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, DateTime value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, TimeSpan value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, string value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, decimal value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, IEnumerable<byte>? value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, ulong value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, double value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, char value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, byte value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, sbyte value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, bool value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, ushort value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, int value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, uint value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, long value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, short value);

	/// <summary>
	/// Dumps item in form: ,Name=Value
	/// </summary>
	/// <param name="name">Name of the item</param>
	/// <param name="value">Value of the item</param>
	/// <returns></returns>
	DumpWriter Then(string name, float value);

	DumpWriter NewLine();
}