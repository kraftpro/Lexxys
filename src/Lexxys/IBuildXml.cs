// Lexxys Infrastructural library.
// file: IBuildXml.cs
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
	using Xml;

	public interface IBuildXml
	{
		string XmlElementName { get; }
		XmlLiteBuilder ToXmlContent(XmlLiteBuilder builder);
	}

	public static class BuildXmlExtensions
	{
		public static XmlLiteBuilder ToXml(this IBuildXml obj, XmlLiteBuilder xml)
		{
			obj?.ToXmlContent(xml.Element(obj.XmlElementName)).End();
			return xml;
		}
	}
}


