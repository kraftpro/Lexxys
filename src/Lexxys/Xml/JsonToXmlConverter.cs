// Lexxys Infrastructural library.
// file: JsonToXmlConverter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using Lexxys;
using Lexxys.Tokenizer;

namespace Lexxys.Xml
{
	public static class JsonToXmlConverter
	{
		public static XmlLiteNode Convert(string text, string rootName, bool ignoreCase = false, bool forceAttributes = false, string sourceName = null)
		{
			var node = JsonParser.Parse(text, sourceName);
			return node == null ? XmlLiteNode.Empty: node.ToXml(rootName, ignoreCase, forceAttributes);
		}

		public static IReadOnlyList<XmlLiteNode> Convert(string text, bool ignoreCase = false, bool forceAttributes = false, string sourceName = null)
		{
			var node = JsonParser.Parse(text, sourceName);
			return node == null ? Array.Empty<XmlLiteNode>(): (IReadOnlyList<XmlLiteNode>)node.ToXml("x", ignoreCase, forceAttributes).Elements;
		}
	}
}


