// Lexxys Infrastructural library.
// file: XmlLiteBuilder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys.Xml;

public static partial class XmlNodeBuilder
{
	[Serializable]
	private class EmptyXmlNode: IXmlNode
	{
		public static readonly IXmlNode Instance = new EmptyXmlNode();

		private EmptyXmlNode() { }

		public string? this[string name] { get => null; set => throw new NotImplementedException(); }
		public string Name { get => String.Empty; set => throw new NotImplementedException(); }
		public string Value { get => String.Empty; set => throw new NotImplementedException(); }

		public IList<KeyValuePair<string, string>> Attributes => Array.Empty<KeyValuePair<string, string>>();
		public IList<IXmlNode> Elements => Array.Empty<IXmlNode>();
		public StringComparer Comparer => StringComparer.Ordinal;
		public bool IsEmpty => true;

		public IXmlReadOnlyNode AsReadOnly() => this;

		IReadOnlyList<KeyValuePair<string, string>> IXmlReadOnlyNode.Attributes => Array.Empty<KeyValuePair<string, string>>();

		IReadOnlyList<IXmlReadOnlyNode> IXmlReadOnlyNode.Elements => Array.Empty<IXmlReadOnlyNode>();
	}
}
