using System.Collections;

namespace Lexxys;

[Obsolete]
public interface IJsonBuilder
{
	/// <summary>
	/// Naming rules of the JSON items
	/// </summary>
	NamingCaseRule NamingRule { get; set; }

	/// <summary>
	/// Indicates whether the current position is inside an object.
	/// </summary>
	bool InObject { get; }

	/// <summary>
	/// Indicates whether the current position is inside an array.
	/// </summary>
	bool InArray { get; }

	/// <summary>
	/// Sets the <see cref="JsonBuilder.NamingRule"/> for the JSON items.
	/// </summary>
	/// <param name="namingRule">Items naming rule</param>
	/// <returns></returns>
	JsonBuilder WithNamingRule(NamingCaseRule namingRule);

	/// <summary>
	/// Writes the start of a JSON object.
	/// </summary>
	/// <returns></returns>
	JsonBuilder Obj();

	/// <summary>
	/// Writes the start of an JSON array.
	/// </summary>
	/// <returns></returns>
	JsonBuilder Arr();

	/// <summary>
	/// Writes the end of an array or an object.
	/// </summary>
	/// <returns></returns>
	JsonBuilder End();

	/// <summary>
	/// Writes a name of an JSON attribute.
	/// </summary>
	/// <param name="name">Name of the attribute</param>
	/// <returns></returns>
	JsonBuilder Item(string name);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, IDictionary? value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, string? value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, char value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, bool value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, byte value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, sbyte value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, short value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, ushort value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, int value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, uint value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, long value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, ulong value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, decimal value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, float value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, double value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, DateTime value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, DateTimeOffset value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, TimeSpan value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, Guid value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, byte[]? value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, IEnumerable? value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, IDumpJson? value);

	/// <summary>
	/// Writes item and value pair using <see cref="IDumpJson"/> implementation if it is present.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder Item(string name, object? value);

	/// <summary>
	/// Writes a <see cref="IDictionary"/> value as a JSON object
	/// </summary>
	/// <param name="value">The <see cref="IDictionary"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(IDictionary? value);

	/// <summary>
	/// Writes a <see cref="String"/> value.
	/// </summary>
	/// <param name="value">The <see cref="String"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(string? value);

	/// <summary>
	/// Writes a <see cref="Char"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Char"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(char value);

	/// <summary>
	/// Writes an <see cref="Boolean"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Boolean"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(bool value);

	/// <summary>
	/// Writes a <see cref="Byte"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Byte"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(byte value);

	/// <summary>
	/// Writes a <see cref="SByte"/> value.
	/// </summary>
	/// <param name="value">The <see cref="SByte"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(sbyte value);

	/// <summary>
	/// Writes an <see cref="Int16"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Int16"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(short value);

	/// <summary>
	/// Writes an <see cref="UInt16"/> value.
	/// </summary>
	/// <param name="value">The <see cref="UInt16"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(ushort value);

	/// <summary>
	/// Writes an <see cref="Int32"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Int32"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(int value);

	/// <summary>
	/// Writes an <see cref="UInt32"/> value.
	/// </summary>
	/// <param name="value">The <see cref="UInt32"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(uint value);

	/// <summary>
	/// Writes an <see cref="Int64"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Int64"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(long value);

	/// <summary>
	/// Writes an <see cref="UInt64"/> value.
	/// </summary>
	/// <param name="value">The <see cref="UInt64"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(ulong value);

	/// <summary>
	/// Writes a <see cref="Decimal"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Decimal"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(decimal value);

	/// <summary>
	/// Writes a <see cref="Single"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Single"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(float value);

	/// <summary>
	/// Writes a <see cref="Double"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Double"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(double value);

	/// <summary>
	/// Writes a <see cref="DateTime"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(DateTime value);

	/// <summary>
	/// Writes a <see cref="DateTimeOffset"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTimeOffset"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(DateTimeOffset value);

	/// <summary>
	/// Writes a <see cref="TimeSpan"/> value.
	/// </summary>
	/// <param name="value">The <see cref="TimeSpan"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(TimeSpan value);

	/// <summary>
	/// Writes a <see cref="Guid"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Guid"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(Guid value);

	/// <summary>
	/// Writes the value of the bytes array as the base64 string.
	/// </summary>
	/// <param name="value">The <see cref="Guid"/> value to write</param>
	/// <returns></returns>
	JsonBuilder Val(byte[]? value);

	/// <summary>
	/// Writes a <see cref="IEnumerable"/> value as an JSON array.
	/// </summary>
	/// <param name="value">The <see cref="IEnumerable"/> value to write.</param>
	/// <returns></returns>
	JsonBuilder Val(IEnumerable? value);

	/// <summary>
	/// Writes an object <paramref name="value"/> using <see cref="IDumpJson"/> implementation.
	/// </summary>
	/// <param name="value">The <see cref="Object"/> value to write.</param>
	/// <returns></returns>
	JsonBuilder Val(IDumpJson? value);

	/// <summary>
	/// Writes an object <paramref name="value"/> using <see cref="IDumpJson"/> implementation if it is present.
	/// </summary>
	/// <param name="value">The <see cref="Object"/> value to write.</param>
	/// <returns></returns>
	JsonBuilder Val(object? value);

	/// <summary>
	/// Writes an object <paramref name="value"/> ignoring <see cref="IDumpJson"/> implementation.
	/// </summary>
	/// <param name="value">The <see cref="Object"/> value to write.</param>
	/// <returns></returns>
	JsonBuilder ValObj(object? value);

	/// <summary>
	/// Writes item and value pair ignoring <see cref="IDumpJson"/> implementation.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	JsonBuilder ItemObj(string name, object? value);

	/// <summary>
	/// Writes value using <see cref="IDumpJson"/> implementation.
	/// </summary>
	/// <param name="value">The value to write</param>
	/// <returns></returns>
	JsonBuilder Content(IDumpJson? value);

	/// <summary>
	/// Closes all end tags.
	/// </summary>
	void Flush();
}