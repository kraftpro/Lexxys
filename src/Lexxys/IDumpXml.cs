// Lexxys Infrastructural library.
// file: IDumpXml.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public interface IDumpXml
	{
		string XmlElementName { get; }
		XmlBuilder ToXmlContent(XmlBuilder builder);
	}

	public static class DumpXmlExtensions
	{
		public static XmlBuilder ToXml(this IDumpXml obj, XmlBuilder xml)
		{
			obj?.ToXmlContent(xml.Element(obj.XmlElementName)).End();
			return xml;
		}

		public static StringBuilder ToXml(this IDumpXml obj, StringBuilder text)
		{
			Contract.Ensures(Contract.Result<StringBuilder>() != null);
			text ??= new StringBuilder();
			if (obj == null)
				return text;
			obj.ToXml(new XmlStringBuilder(text));
			return text;
		}

		public static string ToXml(this IDumpXml obj)
		{
			Contract.Ensures(Contract.Result<string>() != null);
			return obj == null ? "" : obj.ToXml(new XmlStringBuilder()).ToString();
		}
	}

	public sealed class XmlElementAttribute: Attribute
	{
		public XmlElementAttribute()
		{
		}

		public XmlElementAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}
}


