// Lexxys Infrastructural library.
// file: TextFormatSetting.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys.Logging;

using Xml;

public readonly struct TextFormatSetting
{
	public string Format { get; }
	public string Indent { get; }
	public string Section { get; }

	public TextFormatSetting(string format, string indent, string? section = default)
	{
		if (format == null)
			throw new ArgumentNullException(nameof(format));
		if (indent == null)
			throw new ArgumentNullException(nameof(indent));

		Format = FixIndentMark(format);
		Indent = indent;
		Section = section ?? (
			indent.Length == 0 ? indent:
			indent[0] == '\t' ? "." + indent:
			indent.Length == 1 ? indent: "." + indent.Substring(1, indent.Length - 1));

		static string FixIndentMark(string value)
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
	}

	public TextFormatSetting(TextFormatSetting other)
	{
		Format = other.Format;
		Section = other.Section;
		Indent = other.Indent;
	}

	/// <summary>
	/// Replace formatting setting from XmlStream <paramref name="config"/>
	/// <code>
	/// Xml attributes:
	///		format		- log record format <see cref="LogRecordTextFormatter.Format(System.IO.TextWriter, LogRecord)"/>
	///		indent		- the indentation string
	///		para		- new paragraph indentation string
	/// </code>
	/// </summary>
	/// <param name="config">Xml Stream with formatting settings</param>
	/// <returns>this object</returns>
	public TextFormatSetting Join(XmlLiteNode? config)
	{
		return config == null || config.IsEmpty ? this:
			new TextFormatSetting(
				Strings.GetString(config["format"], Format)!,
				Strings.GetString(config["indent"], Section)!,
				Strings.GetString(config["para"], Indent));
	}
}


