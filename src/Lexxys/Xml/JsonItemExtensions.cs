using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lexxys.Xml;

public static class JsonItemExtenstions
{
	private const string XmlItemName = "item";

	internal static IXmlReadOnlyNode ToXmlBase(JsonItem item, string name, string? value, bool ignoreCase, IEnumerable<IXmlReadOnlyNode>? properties)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		return item.Attributes.Count == 0 ?
			new XmlLiteNode(name, value, ignoreCase, null, properties):
			new XmlLiteNode(name, value, ignoreCase,
				item.Attributes.Select(o => new KeyValuePair<string, string>(o.Name, XmlTools.Convert(o.Item.Value) ?? "")),
				properties);
	}

	internal static IXmlReadOnlyNode ToXml(this JsonItem item, string name, bool ignoreCase = false, bool attributes = false)
	{
		return item switch
		{
			JsonMap map => ToXml(map, name, ignoreCase, attributes),
			JsonScalar scalar => ToXml(scalar, name, ignoreCase, attributes),
			JsonArray array => ToXml(array, name, ignoreCase, attributes),
			_ => throw new InvalidOperationException($"Invalid JSON item type: {item.GetType().Name}"),
		};
	}

	public static IXmlReadOnlyNode ToXml(this JsonScalar item, string name, bool ignoreCase = false, bool attributes = false) => ToXmlBase(item, name, XmlTools.Convert(item.Value), ignoreCase, null);

	public static IXmlReadOnlyNode ToXml(this JsonMap item, string name, bool ignoreCase = false, bool attributes = false)
	{
		var attribs = item.Attributes.Select(o => new KeyValuePair<string, string>(o.Name, XmlTools.Convert(o.Item.Value) ?? "")).ToList();
		var properties = new List<IXmlReadOnlyNode>();
		if (item.Properties.Count > 0)
		{
			foreach (var prop in item.Properties)
			{
				if (prop.IsEmpty)
					continue;
				if (prop.Item is JsonScalar scalar)
				{
					bool attrib = attributes;
					var nm = prop.Name;
					if (nm.StartsWith("@", StringComparison.Ordinal))
					{
						attrib = true;
						if (nm.Length > 1)
							nm = nm.Substring(1);
					}
					if (attrib)
					{
						attribs.Add(new KeyValuePair<string, string>(nm, XmlTools.Convert(scalar.Value) ?? ""));
						continue;
					}
				}
				properties.Add(prop.Item.ToXml(prop.Name, ignoreCase, attributes));
			}
		}
		return new XmlLiteNode(name, null, ignoreCase, attribs, properties);
	}

	public static IXmlReadOnlyNode ToXml(this JsonArray array, string name, bool ignoreCase = false, bool attributes = false)
	{
		return ToXmlBase(array, name, null, ignoreCase, array.Items.Select(o => (o ?? JsonScalar.Null).ToXml(XmlItemName, ignoreCase, attributes)));
	}
}
