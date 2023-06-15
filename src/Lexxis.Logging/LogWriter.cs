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
		"{MachineName}:{ProcessID:X4}{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyyMMddTHH:mm:ss.fffff}[{Type:3}] {IndentMark}{Source}: {Message}",
		"  ",
		". ");
	private const string LogSource = "Lexxys.Logging.LogWriter";

	private readonly LoggingRule _rule;

	public LogWriter(ILogWriterParameters parameters)
	{
		Name = parameters.Name ?? this.GetType().Name;
		Formatter = (parameters.Formatter ?? new LogRecordTextParameters()).CreateFormatter();
		_rule = LoggingRule.Create(parameters.Rules, parameters.Include, parameters.Exclude, parameters.LogLevel);
	}

	public void Configure(LogWriterParameters? parameters)
	{
		if (parameters is null)
			return;
		if (parameters.Name is not null)
			Name = parameters.Name;
	}

	/// <summary>
	/// Get name of the <see cref="LogWriter"/>
	/// </summary>
	public string Name { get; private set; }

	public abstract string Target { get; }

	protected internal ILogRecordFormatter Formatter { get; private set; }

	public static ILogWriter? FromXml(XmlLiteNode? node)
	{
		if (node == null || node.IsEmpty)
			return null;

		string? name = node["name"].AsString(null);
		string? className = node["class"].AsString(null);
		if (className is { Length: >0 })
			return CreateLogWriter(className, name, node);

		SystemLog.WriteErrorMessage(LogSource, SR.LOG_CannotCreateLogWriter(name, className));
		return null;
	}

	private static LogWriter? CreateLogWriter(string className, string? name, XmlLiteNode node)
	{
		LogWriter? writer = null;
		try
		{
			Type? type = Factory.GetType(className) ??
				(className.IndexOf('.') < 0 ? Factory.GetType("Lexxys.Logging." + className) : null);
			if (type != null && typeof(LogWriter).IsAssignableFrom(type))
				writer = Factory.TryGetConstructor(type, new[] { typeof(string), typeof(XmlLiteNode) })?
					.Invoke(new object?[] { name, node }) as LogWriter;
			if (writer == null)
				SystemLog.WriteErrorMessage(LogSource, SR.LOG_CannotCreateLogWriter(name, className));
		}
		catch (Exception e)
		{
			SystemLog.WriteErrorMessage(LogSource, SR.LOG_CannotCreateLogWriter(name, className, e));
		}
		return writer;
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
