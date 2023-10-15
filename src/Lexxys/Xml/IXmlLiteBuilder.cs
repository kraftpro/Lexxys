// Lexxys Infrastructural library.
// file: IXmlNodeBuilder<T>.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Xml;

namespace Lexxys.Xml;

public interface IXmlNodeBuilder<TNode>
{
	/// <summary>
	/// Writes an attribute <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	IXmlNodeBuilder<TNode>	Attrib(string name, string? value);

	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	IXmlNodeBuilder<TNode>	Value(string? value);

	/// <summary>
	/// Writes the specified <paramref name="value"/> and the <paramref name="value"/> properties as an XML element or attribute.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="elements">Indicates whether to write properties of the <paramref name="value"/> as an XML elements or attributes.</param>
	IXmlNodeBuilder<TNode> Value(object? value, bool elements = false);

	/// <summary>
	/// Starts a new XML node with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of the node.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	IXmlNodeBuilder<TNode>	Begin(string name);

	/// <summary>
	/// Ends the current XML node.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	IXmlNodeBuilder<TNode>	End();

	/// <summary>
	/// Adds an XML node with the specified <paramref name="name"/> and the <paramref name="value"/>.
	/// </summary>
	/// <param name="name">Name of the created XML element</param>
	/// <param name="value"></param>
	/// <param name="elements">Indicates whether to write properties of the <paramref name="value"/> as an XML elements or attributes.</param>
	IXmlNodeBuilder<TNode> Element(string name, object? value, bool elements = false);

	/// <summary>
	/// Adds a collection of sub-nodes to the current node.
	/// </summary>
	/// <param name="nodes"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
	IXmlNodeBuilder<TNode>	Elements(IEnumerable<TNode> nodes);

	/// <summary>
	/// Adds a collection <typeparamref name="T"/>s with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="items">Collection of items to be added.</param>
	/// <param name="name">Name of the nodes.</param>
	/// <param name="action">Action to be applied to each item.</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	IXmlNodeBuilder<TNode>	Elements<T>(IEnumerable<T> items, string name, Action<IXmlNodeBuilder<TNode>, T> action);

	/// <summary>
	/// Returns an actual list of constructed <typeparamref name="TNode"/> trees.
	/// </summary>
	/// <returns></returns>
	IReadOnlyList<TNode> GetNodes();

	/// <summary>
	/// Completes the building process and returns a list of constructed <typeparamref name="TNode"/> trees.
	/// </summary>
	/// <returns></returns>
	List<TNode> Build();
}

public static class XmlNodeBuilderExtensions
{
	/// <summary>
	/// Adds a sub-node to the current node.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="node"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static IXmlNodeBuilder<T> Elements<T>(this IXmlNodeBuilder<T> builder, T node) => builder.Elements((IEnumerable<T>)new[] { node });

	/// <summary>
	/// Adds a collection of sub-nodes to the current node.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="nodes"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
	public static IXmlNodeBuilder<T> Elements<T>(this IXmlNodeBuilder<T> builder, params T[] nodes) => builder.Elements((IEnumerable<T>)nodes);

	/// <summary>
	/// Adds an XML fragment to the current node.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="builder"></param>
	/// <param name="reader"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IXmlNodeBuilder<T> Xml<T>(this IXmlNodeBuilder<T> builder, XmlReader reader)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));
		if (reader is null) throw new ArgumentNullException(nameof(reader));
		if (reader.ReadState == ReadState.EndOfFile) return builder;

		while (reader.NodeType != XmlNodeType.Element)
			if (!reader.Read()) return builder;

		builder.Begin(reader.Name);

		if (reader.MoveToFirstAttribute())
		{
			do
			{
				builder.Attrib(reader.Name, reader.Value);
			} while (reader.MoveToNextAttribute());
			reader.MoveToElement();
		}

		if (reader.IsEmptyElement) return builder.End();

		string? value = null;
		while (reader.Read())
		{
			switch (reader.NodeType)
			{
				case XmlNodeType.Element:
					Xml(builder, reader);
					break;

				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
					value = ConcatValue(value, reader.Value);
					break;

				case XmlNodeType.EndElement:
					return builder.Value(value).End();
			}
		}
		throw new InvalidOperationException();

		static string? ConcatValue(string? value, string node)
		{
			int n = TrimRight(node);
			if (n == 0)
				return value;
			if (value is not { Length: > 0 })
				return n >= node.Length ? node : node.Substring(0, n);

			if (!IsCrLf(node[0]) || !IsCrLf(value[value.Length - 1]))
				return value + (n >= node.Length ? node : node.Substring(0, n));
			else
				return value + (n > 1 && (node[0] ^ node[1]) == ('\r' ^ '\n') ? node.Substring(2, n - 2) : node.Substring(1, n - 1));
		}

		static int TrimRight(string value)
		{
			var span = value.AsSpan();
			int i = span.Length;
			do
			{
				if (--i < 0) return 0;
			} while (IsSpace(span[i]));

			if (IsCrLf(span[i]) && --i > 0)
				if ((span[i] ^ span[i - 1]) == ('\r' ^ '\n'))
					--i;
			return i + 1;
		}

		static bool IsSpace(char c) => c is ' ' or '\t';
		static bool IsCrLf(char c) => c is '\r' or '\n';
	}

	/// <summary>
	/// Adds an XML fragment to the current node.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="builder"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IXmlNodeBuilder<T> Xml<T>(this IXmlNodeBuilder<T> builder, string value)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));
		if (value is null) throw new ArgumentNullException(nameof(value));
		if (value.Length == 0) return builder;
		using var reader = XmlReader.Create(new StringReader(value));
		return builder.Xml(reader);
	}

	#region Attrib

	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, bool value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, sbyte value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, byte value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, short value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, ushort value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, int value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, uint value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, long value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, ulong value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, float value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, double value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, decimal value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, Guid value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, TimeSpan value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTime value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="dateTimeOption">How to treat the <paramref name="value"/>.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTime value, XmlDateTimeSerializationMode dateTimeOption) => builder.Attrib(name, XmlTools.Convert(value, dateTimeOption));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTime value, string format) => builder.Attrib(name, XmlTools.Convert(value, format));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTimeOffset value) => builder.Attrib(name, XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTimeOffset value, string format) => builder.Attrib(name, XmlTools.Convert(value, format));

	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, bool? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, sbyte? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, byte? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, short? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, ushort? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, int? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, uint? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, long? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, ulong? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, float? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, double? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, decimal? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, Guid? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, TimeSpan? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTime? value) => value == null ? builder.Attrib(name, (string?)null) : builder.Attrib(name, value.GetValueOrDefault());
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="dateTimeOption">How to treat the <paramref name="value"/>.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTime? value, XmlDateTimeSerializationMode dateTimeOption) => value == null ? builder.Attrib(name, (string?)null) : builder.Attrib(name, value.GetValueOrDefault(), dateTimeOption);
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTime? value, string format) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault(), format));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTimeOffset? value) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Attrib<T>(this IXmlNodeBuilder<T> builder, string name, DateTimeOffset? value, string format) => builder.Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault(), format));

	#endregion

	#region Value

	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, bool value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, sbyte value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, byte value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, short value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, ushort value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, int value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, uint value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, long value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, ulong value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, float value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, double value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, decimal value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, Guid value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, TimeSpan value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTime value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <param name="dateTimeOption"></param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTime value, XmlDateTimeSerializationMode dateTimeOption) => builder.Value(XmlTools.Convert(value, dateTimeOption));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTime value, string format) => builder.Value(XmlTools.Convert(value, format));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTimeOffset value) => builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTimeOffset value, string format) => builder.Value(XmlTools.Convert(value, format));

	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, bool? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, sbyte? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, byte? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, short? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, ushort? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, int? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, uint? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, long? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, ulong? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, float? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, double? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, decimal? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, Guid? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, TimeSpan? value) => value == null ? builder: builder.Value(XmlTools.Convert(value));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTime? value) => value == null ? builder: builder.Value(value.GetValueOrDefault());
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <param name="dateTimeOption"></param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTime? value, XmlDateTimeSerializationMode dateTimeOption) => value == null ? builder: builder.Value(value.GetValueOrDefault(), dateTimeOption);
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTime? value, string format) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault(), format));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTimeOffset? value) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault()));
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value">The value to write.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted</param>
	/// <returns></returns>
	public static IXmlNodeBuilder<T> Value<T>(this IXmlNodeBuilder<T> builder, DateTimeOffset? value, string format) => value == null ? builder: builder.Value(XmlTools.Convert(value.GetValueOrDefault(), format));

	#endregion
}

