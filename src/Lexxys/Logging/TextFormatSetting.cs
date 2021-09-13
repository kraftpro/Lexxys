// Lexxys Infrastructural library.
// file: TextFormatSetting.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

#nullable enable

namespace Lexxys.Logging
{
	using Xml;

	public readonly struct TextFormatSetting
	{
		public string Format { get; }
		public string Indent { get; }
		public string Section { get; }

		public TextFormatSetting(string format, string indent, string? section)
		{
			if (format == null)
				throw new ArgumentNullException(nameof(format));
			if (indent == null)
				throw new ArgumentNullException(nameof(indent));

			Format = FixIndentMark(format);
			Indent = indent;
			Section = section ?? indent;
		}

		public TextFormatSetting(TextFormatSetting other)
		{
			Format = other.Format;
			Section = other.Section;
			Indent = other.Indent;
		}

		private static string FixIndentMark(string value)
		{
			if (value.IndexOf("{IndentMark", StringComparison.OrdinalIgnoreCase) >= 0)
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
					XmlTools.GetString(config["indent"], Section),
					XmlTools.GetString(config["para"], Indent));
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
				setting.Section ?? Section,
				setting.Indent ?? Indent);
		}
	}
}


