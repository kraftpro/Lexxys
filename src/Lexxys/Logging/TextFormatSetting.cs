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
	public class TextFormatSetting
	{
		public string Format { get; set; }
		public string Indent { get; set; }
		public string Para { get; set; }

		public TextFormatSetting()
		{
		}

		public TextFormatSetting(string para, string indent, string format)
		{
			Format = format;
			Indent = indent;
			Para = para;
		}

		public TextFormatSetting(TextFormatSetting other)
		{
			Format = other.Format;
			Indent = other.Indent;
			Para = other.Para;
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
			if (config != null && !config.IsEmpty)
			{
				Format = XmlTools.GetString(config["format"], Format);
				Indent = XmlTools.GetString(config["indent"], Indent);
				Para = XmlTools.GetString(config["para"], Para);
			}
			return this;
		}

		/// <summary>
		/// Replace non empty setting from <paramref name="setting"/>.
		/// </summary>
		/// <param name="setting">Parameters to replace</param>
		/// <returns>this object</returns>
		public TextFormatSetting Join(TextFormatSetting setting)
		{
			if (setting != null)
			{
				if (setting.Format != null)
					Format = setting.Format;
				if (setting.Indent != null)
					Indent = setting.Indent;
				if (setting.Para != null)
					Para = setting.Para;
			}
			return this;
		}
	}
}


