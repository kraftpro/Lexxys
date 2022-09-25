// Lexxys Infrastructural library.
// file: IDumpXml.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Text;

namespace Lexxys
{
	public interface IDumpXml
	{
		string XmlElementName { get; }
		XmlBuilder ToXmlContent(XmlBuilder builder);
	}

	public static class DumpXmlExtensions
	{
		public static XmlBuilder ToXml(this IDumpXml? obj, XmlBuilder xml)
		{
			if (xml is null)
				throw new ArgumentNullException(nameof(xml));
			obj?.ToXmlContent(xml.Element(obj.XmlElementName)).End();
			return xml;
		}

		public static StringBuilder ToXml(this IDumpXml? obj, StringBuilder text)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (obj == null)
				return text;
			obj.ToXml(new XmlStringBuilder(text));
			return text;
		}

		public static string ToXml(this IDumpXml? obj)
		{
			return obj == null ? "" : obj.ToXml(new XmlStringBuilder()).ToString()!;
		}
	}
}


