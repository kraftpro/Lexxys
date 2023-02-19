// Lexxys Infrastructural library.
// file: IBuildXml.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys
{
	using Xml;

	public interface IBuildXml
	{
		string XmlElementName { get; }
		XmlLiteBuilder ToXmlContent(XmlLiteBuilder builder);
	}

	public static class BuildXmlExtensions
	{
		public static XmlLiteBuilder ToXml(this IBuildXml? obj, XmlLiteBuilder xml)
		{
			if (xml is null)
				throw new ArgumentNullException(nameof(xml));

			obj?.ToXmlContent(xml.Element(obj.XmlElementName)).End();
			return xml;
		}
	}
}


