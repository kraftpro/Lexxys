// Lexxys Infrastructural library.
// file: JsonToXmlConverter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

#nullable enable

namespace Lexxys.Xml
{
	public static class JsonToXmlConverter
	{
		public static XmlLiteNode Convert(string text, string rootName, bool ignoreCase = false, bool forceAttributes = false, string? sourceName = null)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (rootName is null || rootName.Length <= 0)
				throw new ArgumentNullException(nameof(rootName));

			return JsonParser.Parse(text, sourceName)
				.ToXml(rootName, ignoreCase, forceAttributes);
		}

		public static IReadOnlyList<XmlLiteNode> Convert(string text, bool ignoreCase = false, bool forceAttributes = false, string? sourceName = null)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			return JsonParser.Parse(text, sourceName)
				.ToXml("x", ignoreCase, forceAttributes).Elements;
		}
	}
}


