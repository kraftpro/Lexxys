// Lexxys Infrastructural library.
// file: XmlTools.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using Microsoft.Extensions.Logging;

using System.Xml;

namespace Lexxys.Xml;

public static partial class XmlTools
{
	private static ILogger? Log => __logger ??= Statics.TryGetLogger("Lexxys.XmlTools");
	private static ILogger? __logger;

	public const string OptionIgnoreCase = "opt:ignoreCase";
	public const string OptionForceAttributes = "opt:forceAttributes";


	public static IXmlReadOnlyNode Wrap(string name, StringComparer? comparer, IEnumerable<KeyValuePair<string, string>>? attributies, IEnumerable<IXmlReadOnlyNode>? elements)
		=> new XmlLiteNode(name, null, comparer, attributies, elements);

	public static IXmlReadOnlyNode FromXml(string value, bool ignoreCase)
		=> FromXml(value, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	public static IXmlReadOnlyNode FromXml(string value, StringComparer? comparer = null)
		=> XmlFragBuilder.Create<IXmlReadOnlyNode>(comparer).Xml(value).Build()[0];

	public static IXmlReadOnlyNode FromXml(XmlReader reader, bool ignoreCase)
		=> FromXml(reader, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	public static IXmlReadOnlyNode FromXml(XmlReader reader, StringComparer? comparer = null)
		=> XmlFragBuilder.Create<IXmlReadOnlyNode>(comparer).Xml(reader).Build()[0];

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from the specified JSON string.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="root"></param>
	/// <param name="ignoreCase"></param>
	/// <param name="forceAttributes"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IXmlReadOnlyNode FromJson(string value, string root, bool ignoreCase = false, bool forceAttributes = false)
		=> value.Length == 0 ? XmlFragBuilder.Empty : JsonToXmlConverter.Convert(value, root, ignoreCase, forceAttributes);
}
