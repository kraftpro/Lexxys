// Lexxys Infrastructural library.
// file: XmlLiteNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using System.Text;
using System.Xml;

namespace Lexxys;
using Xml;

public static class XmlNodeExtensions
{
	// IXmlNode
	
	public static IEnumerable<IXmlNode> Where(this IXmlNode node, string name)
	{
		var comparer = node.Comparer;
		return node.Elements.Where(o => comparer.Equals(name, o.Name));
	}

	public static IEnumerable<IXmlNode> Where(this IXmlNode node, string name, IEqualityComparer<string?> comparer)
	{
		return node.Elements.Where(o => comparer.Equals(name, o.Name));
	}

	public static IEnumerable<IXmlNode> Where(this IXmlNode node, Func<IXmlNode, bool> predicate)
	{
		return node.Elements.Where(predicate);
	}

	public static IXmlNode Element(this IXmlNode node, string name)
	{
		var comparer = node.Comparer;
		foreach (var item in node.Elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return XmlFragBuilder.Empty;
	}
	public static IXmlNode Element(this IXmlNode node, string name, IEqualityComparer<string?> comparer)
	{
		foreach (var item in node.Elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return XmlFragBuilder.Empty;
	}
	public static IXmlNode Element(this IXmlNode node, Func<IXmlNode, bool> predicate)
	{
		foreach (var item in node.Elements)
		{
			if (predicate(item))
				return item;
		}
		return XmlFragBuilder.Empty;
	}

	public static IXmlNode? FirstOrDefault(this IXmlNode node, string? name)
	{
		var comparer = node.Comparer;
		foreach (var item in node.Elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return default;
	}

	public static IXmlNode? FirstOrDefault(this IXmlNode node, string? name, IEqualityComparer<string?> comparer)
	{
		foreach (var item in node.Elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return default;
	}

	public static IXmlNode? FirstOrDefault(this IXmlNode node, Func<IXmlNode, bool> predicate)
	{
		foreach (var item in node.Elements)
		{
			if (predicate(item))
				return item;
		}
		return default;
	}

	// IXmlReadOnlyNode

	public static IEnumerable<IXmlReadOnlyNode> Where(this IXmlReadOnlyNode node, string name)
	{
		var comparer = node.Comparer;
		return node.Elements.Where(o => comparer.Equals(name, o.Name));
	}

	public static IEnumerable<IXmlReadOnlyNode> Where(this IXmlReadOnlyNode node, string name, IEqualityComparer<string?> comparer)
	{
		return node.Elements.Where(o => comparer.Equals(name, o.Name));
	}

	public static IEnumerable<IXmlReadOnlyNode> Where(this IXmlReadOnlyNode node, Func<IXmlReadOnlyNode, bool> predicate)
	{
		return node.Elements.Where(predicate);
	}


	public static IXmlReadOnlyNode Element(this IXmlReadOnlyNode node, string name)
	{
		var comparer = node.Comparer;
		foreach (var item in node.Elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return XmlFragBuilder.Empty;
	}

	public static IXmlReadOnlyNode Element(this IXmlReadOnlyNode node, string name, IEqualityComparer<string?> comparer)
	{
		foreach (var item in node.Elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return XmlFragBuilder.Empty;
	}

	public static IXmlReadOnlyNode Element(this IXmlReadOnlyNode node, Func<IXmlReadOnlyNode, bool> predicate)
	{
		foreach (var item in node.Elements)
		{
			if (predicate(item))
				return item;
		}
		return XmlFragBuilder.Empty;
	}


	public static IXmlReadOnlyNode? FirstOrDefault(this IXmlReadOnlyNode node, string? name)
	{
		var comparer = node.Comparer;
		foreach (var item in node.Elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return default;
	}

	public static IXmlReadOnlyNode? FirstOrDefault(this IXmlReadOnlyNode node, string? name, IEqualityComparer<string?> comparer)
	{
		foreach (var item in node.Elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return default;
	}

	public static IXmlReadOnlyNode? FirstOrDefault(this IXmlReadOnlyNode node, Func<IXmlReadOnlyNode, bool> predicate)
	{
		foreach (var item in node.Elements)
		{
			if (predicate(item))
				return item;
		}
		return default;
	}

	public static XmlReader ReadSubtree(this IXmlReadOnlyNode node) => XmlReader.Create(new StringReader(WriteXml(node).ToString()));

	public static StringBuilder WriteXml(this IXmlReadOnlyNode node, StringBuilder? text = null, bool innerXml = false, bool format = false)
		=> WriteXml(node, text, innerXml, format ? "": null);

	private static StringBuilder WriteXml(IXmlReadOnlyNode node, StringBuilder? text, bool innerXml, string? indent)
	{
		if (node is null) throw new ArgumentNullException(nameof(node));
		text ??= new StringBuilder();
		if (node.IsEmpty) return text;
		if (!innerXml)
		{
			text.Append(indent).Append('<').Append(XmlConvert.EncodeName(node.Name));
			foreach (var item in node.Attributes)
			{
				text.Append(' ').Append(XmlConvert.EncodeName(item.Key)).Append('=');
				XmlTools.EncodeAttribute(text, item.Value.AsSpan());
			}

			if (node.Value.Length > 0)
			{
				text.Append('>');
				if (indent != null && (node.Value.Length > 80 || node.Elements.Count > 0))
					text.AppendLine().Append(indent).Append("  ");
				XmlTools.Encode(text, node.Value.AsSpan());
			}
			else if (node.Elements.Count > 0)
			{
				text.Append('>');
			}
			else
			{
				text.Append("/>");
				return text;
			}
		}
		else if (node.Elements.Count == 0)
		{
			return text;
		}

		var indent2 = innerXml ? indent: indent == null ? null: indent + "  ";
		foreach (var item in node.Elements)
		{
			if (indent != null)
				text.AppendLine();
			WriteXml(item, text, false, indent2);
		}
		if (innerXml) return text;

		if (indent != null && (node.Value.Length > 80 || node.Elements.Count > 0))
			text.AppendLine().Append(indent);
		text.Append("</").Append(XmlConvert.EncodeName(node.Name)).Append('>');
		return text;
		
	}
}
