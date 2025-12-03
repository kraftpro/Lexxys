namespace Lexxys;

[Obsolete]
public interface IXmlBuilder
{
	/// <summary>
	/// Naming rules of the XML elements and attributes (default <see cref="NamingCaseRule.None"/>).
	/// </summary>
	NamingCaseRule NamingRule { get; }

	/// <summary>
	/// Indicates that XML elements are better than XML attributes while writing <see cref="System.Object"/> value (default <code>false</code>).
	/// </summary>
	bool PreferElements { get; }

	/// <summary>
	/// Indicates that the <see cref="XmlBuilder"/> is writing start element tag.
	/// </summary>
	bool InElement { get; }

	/// <summary>
	/// Indicates that the <see cref="XmlBuilder"/> is writing attribute.
	/// </summary>
	bool InAttribute { get; }

	/// <summary>
	/// Writes a start tag with specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">The name of the element</param>
	/// <returns><see cref="XmlBuilder"/></returns>
	XmlBuilder Element(string name);

	/// <summary>
	/// Writes XML element and value.
	/// </summary>
	/// <param name="name">Name of the XML element</param>
	/// <param name="value">Value of the XML element</param>
	/// <returns></returns>
	XmlBuilder Element(string name, object? value);

	/// <summary>
	/// Writes XML element and value.
	/// </summary>
	/// <param name="name">Name of the XML element</param>
	/// <param name="value">Value of the XML element</param>
	/// <param name="elements">Use XML elements instead of attributes for properties of <paramref name="value"/></param>
	/// <returns></returns>
	XmlBuilder Element(string name, object? value, bool elements);

	/// <summary>
	/// Writes XML element and value.
	/// </summary>
	/// <param name="name">Name of the XML element</param>
	/// <param name="value">Value of the XML element</param>
	/// <returns></returns>
	XmlBuilder Element(string name, IDumpXml? value);

	/// <summary>
	/// Writes XML element and collection of sub-elements.
	/// </summary>
	/// <param name="name">Name of the XML element</param>
	/// <param name="value">Value of the XML element</param>
	/// <param name="itemName">Optional name of the collection item</param>
	/// <returns></returns>
	XmlBuilder Element(string name, IEnumerable<IDumpXml>? value, string? itemName = null);

	/// <summary>
	/// Writes end tag of current element.
	/// </summary>
	/// <returns><see cref="XmlBuilder"/></returns>
	XmlBuilder End();

	/// <summary>
	/// Switches to the inner Xml elements.
	/// </summary>
	/// <returns></returns>
	XmlBuilder ToInnerElements();

	/// <summary>
	/// Writes the start of attribute with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Attrib(string name);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, string value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, bool value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, sbyte value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, byte value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, short value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, ushort value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, int value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, uint value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, long value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, ulong value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, float value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, double value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, decimal value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, Guid value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, TimeSpan value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="omitTimeZone">If true, the time zone will be omitted in the result.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, DateTime value, bool omitTimeZone = false);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="omitTimeZone">If true, the time zone will be omitted in the result.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, DateTimeOffset value, bool omitTimeZone = false);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, bool? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, sbyte? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, byte? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, short? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, ushort? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, int? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, uint? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, long? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, ulong? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, float? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, double? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, decimal? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, Guid? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, TimeSpan? value);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="omitTimeZone">If true, the time zone will be omitted in the result.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, DateTime? value, bool omitTimeZone = false);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="omitTimeZone">If true, the time zone will be omitted in the result.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, DateTimeOffset? value, bool omitTimeZone = false);

	/// <summary>
	/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	XmlBuilder Item(string name, object? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(string? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(bool value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(sbyte value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(byte value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(short value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(ushort value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(int value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(uint value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(long value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(ulong value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(float value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(double value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(decimal value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(Guid value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(TimeSpan value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(DateTime value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <param name="omitTimeZone">If true, the time zone will be omitted in the result.</param>
	/// <returns></returns>
	XmlBuilder Value(DateTime value, bool omitTimeZone);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(DateTimeOffset value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(bool? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(sbyte? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(byte? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(short? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(ushort? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(int? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(uint? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(long? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(ulong? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(float? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(double? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(decimal? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(Guid? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(TimeSpan? value);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <param name="omitTimeZone">If true, the time zone will be omitted in the result.</param>
	/// <returns></returns>
	XmlBuilder Value(DateTime? value, bool omitTimeZone = false);

	/// <summary>
	///  Writes the value of the current XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	XmlBuilder Value(DateTimeOffset? value);

	/// <summary>
	/// Writes object value
	/// </summary>
	/// <param name="value">The value to write</param>
	/// <returns></returns>
	XmlBuilder Value(IDumpXml? value);

	/// <summary>
	/// Writes collection of values
	/// </summary>
	/// <param name="items">The collection to write</param>
	/// <param name="itemName">Optional name of the element item.</param>
	/// <returns></returns>
	XmlBuilder Value(IEnumerable<IDumpXml>? items, string? itemName = null);

	/// <summary>
	/// Writes object value.
	/// </summary>
	/// <param name="value">Value to write</param>
	/// <returns><see cref="XmlBuilder"/></returns>
	XmlBuilder Value(object? value);

	/// <summary>
	/// Writes object value.
	/// </summary>
	/// <param name="value">Value to write</param>
	/// <param name="elements">Use XML elements instead of attributes for properties of <paramref name="value"/></param>
	/// <returns><see cref="XmlBuilder"/></returns>
	XmlBuilder Value(object? value, bool elements);

	/// <summary>
	/// Writes object value ignoring <see cref="IDumpXml"/> implementation.
	/// </summary>
	/// <param name="value">Value to write</param>
	/// <param name="elements">Use XML elements instead of attributes for properties of <paramref name="value"/></param>
	/// <returns><see cref="XmlBuilder"/></returns>
	XmlBuilder Object(object? value, bool elements = false);

	/// <summary>
	/// Write all ends of elements.
	/// </summary>
	void Flush();
}