// Lexxys Infrastructural library.
// file: TextFormatSetting.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using Lexxys.Xml;

namespace Lexxys.Logging
{
	public readonly struct TextFormatSetting
	{
		public string Format { get; }
		public string Next { get; }
		public string Indent { get; }

		public TextFormatSetting(string format, string next, string indent)
		{
			Format = FixIndentMark(format);
			Next = next;
			Indent = indent ?? Next;
		}

		public TextFormatSetting(TextFormatSetting other)
		{
			Format = other.Format;
			Indent = other.Indent;
			Next = other.Next;
		}

		private static string FixIndentMark(string value)
		{
			if (value == null || value.IndexOf("{IndentMark", StringComparison.OrdinalIgnoreCase) >= 0)
				return value;
			int i = value.IndexOf("{Source", StringComparison.OrdinalIgnoreCase);
			if (i < 0)
				i = value.IndexOf("{Message", StringComparison.OrdinalIgnoreCase);
			if (i < 0)
				i = 0;
			return value.Substring(0, i) + "{IndentMark}" + value.Substring(i);
		}

		/// <summary>
		/// Replace formatting setting from XmlStream <paramref name="config"/>
		/// </summary>
		/// <param name="config">Xml Stream with formatting serring</param>
		/// <returns>this object</returns>
		/// <remarks>
		/// Xml Attribes:
		///		format		- log record format <see cref="LogRecordTextFormatter.Format(System.IO.TextWriter, LogRecord)"/>
		///		indent		- the indentation string
		///		para		- new paragraph indentation string
		/// </remarks>
		public TextFormatSetting Join(XmlLiteNode config)
		{
			return config == null || config.IsEmpty ? this:
				new TextFormatSetting(
					XmlTools.GetString(config["format"], Format),
					XmlTools.GetString(config["indent"], Indent),
					XmlTools.GetString(config["para"], Next));
		}

		/// <summary>
		/// Replace non empty setting from <paramref name="setting"/>.
		/// </summary>
		/// <param name="setting">Parameters to replace</param>
		/// <returns>this object</returns>
		public TextFormatSetting Join(TextFormatSetting setting)
		{
			return new TextFormatSetting(
				setting.Format ?? Format,
				setting.Indent ?? Indent,
				setting.Next ?? Next);
		}
	}
}


