// Lexxys Infrastructural library.
// file: IniToXmlConverter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Lexxys.Xml
{
	public static class IniToXmlConverter
	{
		/*
		 * [Group]
		 * Item = Value
		 * ;Comments
		 * 
		 * 
		 * 
		 * 
		 */

		public static string Convert(string source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			XmlStringBuilder builder = new XmlStringBuilder();
			Convert(builder, source);
			return builder.ToString();
		}

		public static List<XmlLiteNode> ConvertLite(string source, bool ignoreCase = false)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			var builder = new XmlLiteBuilder(ignoreCase);
			Convert(builder, source);
			return builder.Nodes;
		}

		private abstract class Builder
		{
			public abstract void Element(string name);

			public abstract void Attribute(string name, string value);

			public abstract void Finish();
		}

		private class XmlStringBuilder: Builder
		{
			private readonly StringBuilder _text;
			private readonly HashSet<string> _attr;

			public XmlStringBuilder()
			{
				_text = new StringBuilder();
				_attr = new HashSet<string>();
			}

			public override void Element(string name)
			{
				_attr.Clear();
				_text.Append(_text.Length > 0 ? "/><": "<").Append(Name(name, "default"));
			}

			public override void Attribute(string name, string value)
			{
				_text.Append(' ').Append(Attr(name, "@")).Append('=').Append(XmlTools.EncodeAttribute(value));
			}

			public override void Finish()
			{
				if (_text.Length > 0)
					_text.Append("/>");
			}

			private static string Name(string name, string defaultName)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					name = defaultName;
				return XmlConvert.EncodeName(name);
			}

			private string Attr(string name, string defaultName)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					name = defaultName;
				if (_attr.Contains(name))
				{
					int i = 2;
					while (_attr.Contains(name + i.ToString()))
					{
						++i;
					}
					name += i.ToString();
				}
				_attr.Add(name);
				return XmlConvert.EncodeName(name);
			}

			public override string ToString()
			{
				return _text.ToString();
			}
		}

		private class XmlLiteBuilder: Builder
		{
			private string _name;
			private readonly List<XmlLiteNode> _nodes;
			private readonly Dictionary<string, string> _attr;
			private readonly bool _ignoreCase;

			public XmlLiteBuilder(bool ignoreCase)
			{
				_nodes = new List<XmlLiteNode>();
				_attr = new Dictionary<string, string>();
				_ignoreCase = ignoreCase;
			}

			public override void Element(string name)
			{
				if (_name != null)
					_nodes.Add(new XmlLiteNode(_name, null, _ignoreCase, _attr, null));
				_name = Name(name, "default");
				_attr.Clear();
			}

			public override void Attribute(string name, string value)
			{
				_attr.Add(Attr(name, "@"), value);
			}

			public override void Finish()
			{
				if (_name != null)
					_nodes.Add(new XmlLiteNode(_name, null, _ignoreCase, _attr, null));
				_name = null;
				_attr.Clear();
			}

			private static string Name(string name, string defaultName)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					name = defaultName;
				return name;
			}

			private string Attr(string name, string defaultName)
			{
				if (name == null || (name = name.Trim()).Length == 0)
					name = defaultName;
				if (_attr.ContainsKey(name))
				{
					int i = 2;
					while (_attr.ContainsKey(name + i.ToString()))
					{
						++i;
					}
					name += i.ToString();
				}
				return name;
			}

			public List<XmlLiteNode> Nodes
			{
				get { return _nodes; }
			}
		}

		private static void Convert(Builder builder, string source)
		{
			int index = 0;
			for (; ; )
			{
				if (index < 0)
					break;

				while (index < source.Length && Char.IsWhiteSpace(source[index]))
					++index;
				if (index == source.Length)
					break;

				if (source[index] == ';')
				{
					index = source.IndexOfAny(__eol, index);
					if (index < 0)
						break;
				}
				else if (source[index] == '[')
				{
					int i = index + 1;
					index = source.IndexOfAny(__eob, i);
					if (index < 0)
						break;

					string name = source.Substring(i, index - i).Trim();
					builder.Element(name);
					index = source.IndexOfAny(__eol);
				}
				else
				{
					int i = index;
					index = source.IndexOfAny(__eoe, i);
					string name = (index < 0 ? source.Substring(i): source.Substring(i, index - i)).Trim();
					if (name.Length == 0)
						name = null;
					string value;
					if (index > 0 && source[index] == '=')
					{
						i = index + 1;
						index = source.IndexOfAny(new[] { '\n', '\r' }, i);
						value = source.Substring(i, index - i).Trim();
					}
					else
					{
						value = name;
						name = null;
					}
					builder.Attribute(name, value);
				}
			}
			builder.Finish();
		}
		private static readonly char[] __eol = { '\n', '\r' };
		private static readonly char[] __eob = { '\n', '\r', ']' };
		private static readonly char[] __eoe = { '\n', '\r', '=' };
	}
}


