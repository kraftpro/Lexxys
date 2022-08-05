// Lexxys Infrastructural library.
// file: XmlToTextConverter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Lexxys.Xml
{
	public class XmlToTextConverter
	{
		private readonly XmlReader _reader;
		private readonly TextWriter _writer;
		private Element _element;


		private XmlToTextConverter(XmlReader reader, TextWriter writer)
		{
			_reader = reader;
			_writer = writer;
		}


		public static string Convert(string text)
		{
			using var reader = XmlReader.Create(new StringReader(text));
			return Convert(reader, false);
		}

		public static string Convert(XmlReader reader)
		{
			return Convert(reader, false);
		}

		public static string Convert(XmlReader reader, bool closeAtExit)
		{
			var sb = new StringBuilder(1024);
			using (TextWriter writer = new StringWriter(sb, CultureInfo.InvariantCulture))
			{
				Convert(reader, writer, closeAtExit);
			}
			return sb.ToString();
		}

		public static void Convert(XmlReader reader, TextWriter writer, bool closeReaderAtExit)
		{
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));

			var converter = new XmlToTextConverter(reader, writer);
			converter.ConvertFragment("");
			if (closeReaderAtExit)
				reader.Close();
			writer.Flush();
		}


		private void ConvertFragment(string indentation)
		{
			while (_reader.Read())
			{
				switch (_reader.NodeType)
				{
					case XmlNodeType.EndElement:
						FlushElement();
						return;

					case XmlNodeType.ProcessingInstruction:
					case XmlNodeType.Whitespace:
					case XmlNodeType.DocumentType:
						break;

					case XmlNodeType.Document:
					case XmlNodeType.DocumentFragment:
					case XmlNodeType.Entity:
					case XmlNodeType.EndEntity:
					case XmlNodeType.EntityReference:
					case XmlNodeType.Notation:
					case XmlNodeType.None:
					case XmlNodeType.Attribute:
					case XmlNodeType.XmlDeclaration:
						//throw EX.Unexpected("Node type " + _reader.NodeType.ToString() + " is not supported.");
						break;

					case XmlNodeType.Comment:
						FlushElement();
						WriteComments(_reader.Value, indentation);
						break;

					case XmlNodeType.Element:
						FlushElement();
						ConvertElement(indentation);
						break;

					case XmlNodeType.SignificantWhitespace:
					case XmlNodeType.Text:
					case XmlNodeType.CDATA:
						if (_element == null)
							WriteText(_reader.Value.Trim(), indentation);
						else
							FlushElement(_reader.Value.Trim());
						break;

					default:
						throw new InvalidOperationException(SR.UndefinedNodeType(_reader.NodeType));
				}
			}
			FlushElement();
		}

		private void ConvertElement(string indentation)
		{
			Debug.Assert(_reader.NodeType == XmlNodeType.Element);

			bool isEmpty = _reader.IsEmptyElement;
			_element = new Element(_reader.Name, indentation);
			if (_reader.MoveToFirstAttribute())
			{
				do
				{
					_element.Attrib.Add(new NameValue(_reader.Name, _reader.ReadContentAsString()));
				} while (_reader.MoveToNextAttribute());
			}

			if (isEmpty)
			{
				FlushElement();
				return;
			}

			ConvertFragment(indentation + "\t");
		}

		private void FlushElement()
		{
			FlushElement(null);
		}

		private void FlushElement(string value)
		{
			if (_element == null)
				return;
			string indentation = _element.Indent;
			_writer.Write(indentation);
			_writer.Write(_element.Name);
			if (value != null)
				WriteValue(value, indentation + "\t");
			else
				_writer.WriteLine();
			indentation += "\t";
			foreach (NameValue attr in _element.Attrib)
			{
				_writer.Write(indentation);
				_writer.Write("=");
				_writer.Write(attr.Name);
				WriteValue(attr.Value, indentation + "\t");
			}
			_element = null;
		}

		private void WriteComments(string comments, string indentation)
		{
			foreach (string s in SplitLine(comments))
			{
				_writer.Write(indentation);
				_writer.Write("//\t");
				_writer.Write(s.Trim());
				_writer.WriteLine();
			}
		}

		private void WriteValue(string text, string indentation)
		{
			if (text == null)
				return;
			string[] ss = SplitLine(text);
			if (ss.Length == 1)
			{
				_writer.Write("\t");
				_writer.WriteLine(ss[0]);
			}
			else
			{
				_writer.WriteLine();
				_writer.Write(indentation);
				_writer.WriteLine("<<");
				foreach (string s in ss)
				{
					_writer.Write(indentation);
					_writer.WriteLine(TextToValueString(s));
				}
				_writer.Write(indentation);
				_writer.WriteLine(">>");
			}
		}

		private void WriteText(string text, string indentation)
		{
			if (text == null)
				return;
			string[] ss = SplitLine(text);
			if (ss.Length == 1)
			{
				_writer.Write(indentation);
				_writer.Write("< ");
				_writer.WriteLine(ss[0]);
			}
			else
			{
				_writer.WriteLine();
				_writer.Write(indentation);
				_writer.WriteLine("<<");
				foreach (string s in ss)
				{
					_writer.Write(indentation);
					_writer.WriteLine(TextToValueString(s));
				}
				_writer.Write(indentation);
				_writer.WriteLine("<<");
			}
		}

		struct NameValue
		{
			public readonly string Name;
			public readonly string Value;

			public NameValue(string name, string value)
			{
				Name = name;
				Value = value;
			}
		}

		class Element
		{
			public readonly string Name;
			public readonly string Indent;
			public readonly List<NameValue> Attrib;
			//public bool IsFlushed;

			public Element(string name, string indent)
			{
				Name = name;
				Indent = indent;
				Attrib = new List<NameValue>();
			}
		}

		private static string[] SplitLine(string value)
		{
			return value == null ? Array.Empty<string>() : Regex.Replace(value, @"(?<!\n)\r\n|(?<!\r)\n\r|\r", "\n").Split('\n');
		}

		private static string TextToValueString(string text)
		{
			if (text == null)
				return "";
			if (text.Length > 0)
			{
				if (text[text.Length - 1] <= ' ')
				{
#pragma warning disable CA1307 // Specify StringComparison for clarity
					if (text.IndexOf('\'') < 0)
						return "'" + text + "'";
					if (text.IndexOf('"') < 0)
						return "\"" + text + "\"";
					return "'" + text.Replace("'", "''") + "'";
#pragma warning restore CA1307 // Specify StringComparison for clarity
				}
				if (text[0] <= ' ')
				{
					return '`' + text;
				}
			}
			return text;
		}
	}
}


