// Lexxys Infrastructural library.
// file: LogWriter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

namespace Lexxys.Logging;
using Xml;

public abstract class LogWriter: ILogWriter
{
	public static readonly TextFormatSetting TextFormatDefaults = new TextFormatSetting(
		"{MachineName}:{ProcessID:X4}{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyyMMddTHH:mm:ss.fffff} {IndentMark}{Source}: {Message}",
		"  ",
		". ");

	private readonly LoggingRule _rule;

	public LogWriter(string name, XmlLiteNode config, ILogRecordFormatter? formatter = null)
	{
		if (config == null)
			config = XmlLiteNode.Empty;

		Name = name ?? this.GetType().Name;
		Formatter = CreateFormatter(config["formatter"], config) ?? formatter ?? new LogRecordTextFormatter(TextFormatDefaults.Join(config));
		_rule = LoggingRule.Create(config);
	}

	/// <summary>
	/// Get name of the <see cref="LogWriter"/>
	/// </summary>
	public string Name { get; }

	public abstract string Target { get; }

	protected internal ILogRecordFormatter Formatter { get; }

	public static ILogWriter? FromXml(XmlLiteNode node)
	{
		if (node == null || node.IsEmpty)
			return null;

		string? name = node["name"].AsString(null);
		string? className = node["class"].AsString(null);
		if (className != null)
			return CreateLogWriter(className, name, node);

		SystemLog.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name, className));
		return null;
	}

	private static LogWriter? CreateLogWriter(string className, string? name, XmlLiteNode node)
	{
		if (String.IsNullOrEmpty(className))
			return null;
		LogWriter? writer = null;
		try
		{
			Type? type = Factory.GetType(className) ??
				(className.IndexOf('.') < 0 ? Factory.GetType("Lexxys.Logging." + className) : null);
			if (type != null && type.IsSubclassOf(typeof(LogWriter)))
				writer = Factory.TryGetConstructor(type, false, new[] { typeof(string), typeof(XmlLiteNode) })?
					.Invoke(new object?[] { name, node }) as LogWriter;
			if (writer == null)
				SystemLog.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name, className));
		}
		catch (Exception e)
		{
			SystemLog.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name, className, e));
		}
		return writer;
	}

	private static ILogRecordFormatter? CreateFormatter(string className, XmlLiteNode node)
	{
		if (String.IsNullOrEmpty(className))
			return null;
		ILogRecordFormatter? formatter = null;
		try
		{
			Type? type = Factory.GetType(className) ??
				(className.IndexOf('.') < 0 ? Factory.GetType("Lexxys.Logging." + className) : null);
			if (type != null)
				formatter = Factory.TryGetConstructor(type, false, new[] { typeof(XmlLiteNode) })?
					.Invoke(new object[] { node }) as ILogRecordFormatter;
			if (formatter == null)
				SystemLog.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogFormatter(className));
		}
		catch (Exception e)
		{
			SystemLog.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogFormatter(className, e));
		}
		return formatter;
	}

	/// <summary>
	/// Open the <see cref="LogWriter"/> object for logging
	/// </summary>
	public virtual void Open()
	{
	}

	/// <summary>
	/// Close <see cref="LogWriter"/> and free all resources used.
	/// </summary>
	public virtual void Close()
	{
	}

	public bool Accepts(string? source, LogType type) => _rule.Contains(source, type);

	/// <summary>
	/// Log the <paramref name="records"/> to the log
	/// </summary>
	/// <param name="records">The <see cref="LogRecord"/>s to be logged.</param>
	public abstract void Write(IEnumerable<LogRecord> records);
}
