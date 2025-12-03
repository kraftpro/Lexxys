// Lexxys Infrastructural library.
// file: IDumpXml.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;

namespace Lexxys;

[Obsolete("Use IDump intead.")]
public interface IDumpXml
{
	string XmlElementName { get; }
	XmlBuilder ToXmlContent(XmlBuilder builder);
}

public static class DumpXmlExtensions
{
	extension(IDumpXml? obj)
	{
		public XmlBuilder ToXml(XmlBuilder xml)
		{
			if (xml is null) throw new ArgumentNullException(nameof(xml));
			obj?.ToXmlContent(xml.Element(obj.XmlElementName)).End();
			return xml;
		}

		public StringBuilder ToXml(StringBuilder text)
		{
			if (text is null) throw new ArgumentNullException(nameof(text));
			if (obj == null) return text;
			obj.ToXml(new XmlStringBuilder(text));
			return text;
		}

		public string ToXml()
		{
			return obj == null ? String.Empty: obj.ToXml(new XmlStringBuilder()).ToString()!;
		}
	}
}


