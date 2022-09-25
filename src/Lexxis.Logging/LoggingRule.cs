// Lexxys Infrastructural library.
// file: LoggingRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
﻿using System.Linq;
﻿using System.Text.RegularExpressions;

namespace Lexxys.Logging;
using Xml;

[Flags]
public enum LogTypeFilter
{
	None = 0,
	OutputOnly = 1,
	CriticalOnly = 1,
	ErrorOnly = 2,
	WarningOnly = 4,
	InformationOnly = 8,
	TraceOnly = 16,
	DebugOnly = 32,

	Output = 1,
	Critical = 1,
	Error = Output + ErrorOnly,
	Warning = Error + WarningOnly,
	Information = Warning + InformationOnly,
	Trace = Information + TraceOnly,
	Debug = Trace + DebugOnly,

	All = Debug,
}

internal class LoggingRule
{
	readonly Rule[] _rules;

	public static readonly LoggingRule Empty = new LoggingRule(null);

	private LoggingRule(Rule[]? rules)
	{
		_rules = rules ?? Array.Empty<Rule>();
	}

	public static string? GlobalExclude { get; set; }

	public bool IsEmpty => _rules.Length == 0;

	public bool Contains(string? source, LogType type)
	{
		for (int i = 0; i < _rules.Length; ++i)
		{
			if (_rules[i].Contains(type) && _rules[i].Contains(source))
				return true;
		}
		return false;
	}

	public LogTypeFilter LogTypes(string? source)
	{
		LogTypeFilter logTypes = LogTypeFilter.None;
		for (int i = 0; i < _rules.Length; ++i)
		{
			if (_rules[i].Contains(source))
			{
				logTypes |= _rules[i].LogTypes;
				if (logTypes == LogTypeFilter.All)
					return logTypes;
			}
		}
		return logTypes;
	}

	public static LoggingRule Create(XmlLiteNode? config)
	{
		if (config == null || config.IsEmpty)
			return Empty;

		var rr = config.Where("rule").Select(Rule.Create).Where(o=> !o.IsEmpty).ToList();
		return rr.Count == 0 ? Empty: new LoggingRule(rr.ToArray());
	}

	public static LoggingRule Create(LogWriterRuleConfig[]? rules)
	{
		if (rules == null || rules.Length == 0)
			return Empty;
		return new LoggingRule(Array.ConvertAll(rules, r => Rule.Create(r.LogLevel, r.Include, r.Exclude)));
	}

	public static LoggingRule Create(ICollection<LogWriterFilter>? rules)
	{
		if (rules == null || rules.Count == 0)
			return Empty;

		Rule[] rr = rules
			.Select(Rule.TryCreate)
			.Where(o => o != null)
			.ToArray()!;
		return rr.Length == 0 ? Empty: new LoggingRule(rr);
	}


	class Rule
	{
		public static readonly Rule Empty = new Rule(LogTypeFilter.None, null, null);

		private readonly Regex? _include;
		private readonly Regex? _exclude;
		private readonly LogTypeFilter _types;

		public Rule(LogTypeFilter types, string? include, string? exclude)
		{
			_types = types;
			_include = ParseRule(include, true);
			_exclude = ParseRule(GlobalExclude == null ? exclude: exclude == null ? GlobalExclude: exclude + "," + GlobalExclude, false);
		}

		public LogTypeFilter LogTypes => _types;

		public bool IsEmpty => _types == LogTypeFilter.None;

		public bool Contains(string? source)
		{
			if (_types == LogTypeFilter.None)
				return false;
			if (source == null)
				return false;
			if (_include != null)
				return _include.IsMatch(source);
			return _exclude == null || !_exclude.IsMatch(source);
		}

		public bool Contains(LogType type) => (_types & (LogTypeFilter)(1 << (int)type)) != 0;

		public static Rule Create(string? type, string? include, string? exclude)
		{
			LogTypeFilter types = ParseLogType(type, LogTypeFilter.All);
			return types == LogTypeFilter.None ? Empty: new Rule(types, include, exclude);
		}

		public static Rule? TryCreate(LogWriterFilter? filter)
		{
			return filter == null || filter.LogType == LogTypeFilter.None ? null: new Rule(filter.LogType, filter.Include, filter.Exclude);
		}

		public static Rule Create(XmlLiteNode config)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			return Create(config["level"], config["include"], config["exclude"]);
		}

		/// <summary>
		/// Parse logging type from configuration string
		/// </summary>
		/// <param name="configuration">The configuration string</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		/// <remarks>
		///	<code>
		///	config :=	item *
		///	item :=		WRITE | ERROR | WARNING | INFO | TRACE | DEBUG | '*'
		///				(WRITE | ERROR | WARNING | INFO | TRACE | DEBUG | '*')+ ONLY
		///	</code>
		/// 
		/// </remarks>
		private static LogTypeFilter ParseLogType(string? configuration, LogTypeFilter defaultValue)
		{
			if (configuration == null)
				return defaultValue;

			MatchCollection mc = __configRex.Matches(configuration.ToUpperInvariant());
			if (mc.Count == 0)
				return defaultValue;

			bool onlyFlag = false;
			LogTypeFilter only = 0;
			LogTypeFilter all = 0;
			foreach (Match m in mc)
			{
				LogTypeFilter x = 0;
				LogTypeFilter y = 0;
				bool exclude = m.Groups[1].Value == "-";
				switch (m.Groups[2].Value)
				{
					case "WRITE":
						x = LogTypeFilter.OutputOnly;
						y = LogTypeFilter.OutputOnly;
						break;
					case "ERROR":
						x = LogTypeFilter.ErrorOnly;
						y = LogTypeFilter.OutputOnly | LogTypeFilter.ErrorOnly;
						break;
					case "WARNING":
						x = LogTypeFilter.WarningOnly;
						y = LogTypeFilter.OutputOnly | LogTypeFilter.ErrorOnly | LogTypeFilter.WarningOnly;
						break;
					case "INFO":
					case "INFORMATION":
						x = LogTypeFilter.InformationOnly;
						y = LogTypeFilter.OutputOnly | LogTypeFilter.ErrorOnly | LogTypeFilter.WarningOnly | LogTypeFilter.InformationOnly;
						break;
					case "TRACE":
						x = LogTypeFilter.TraceOnly;
						y = LogTypeFilter.OutputOnly | LogTypeFilter.ErrorOnly | LogTypeFilter.WarningOnly | LogTypeFilter.InformationOnly | LogTypeFilter.TraceOnly;
						break;
					case "VERBOSE":
					case "DEBUG":
					case "ALL":
					case "*":
						x = LogTypeFilter.DebugOnly;
						y = LogTypeFilter.OutputOnly | LogTypeFilter.ErrorOnly | LogTypeFilter.WarningOnly | LogTypeFilter.InformationOnly | LogTypeFilter.TraceOnly | LogTypeFilter.DebugOnly;
						break;
					case "ONLY":
						onlyFlag = true;
						break;
				}
				if (exclude)
				{
					all &= ~x;
				}
				else
				{
					only |= x;
					all |= y;
				}
			}
			return all == 0 ? defaultValue: onlyFlag ? only: all;
		}
		private static readonly Regex __configRex = new Regex(@"[,;\|\+\s]*(-)?\s*\b(WRITE|ERROR|WARNING|INFO|INFORMATION|DEBUG|TRACE|VERBOSE|\*|ALL|ONLY)\b", RegexOptions.Compiled);

		private static Regex? ParseRule(string? rule, bool excludeAll)
		{
			if (rule == null)
				return null;
			string pattern = Regex.Escape(__stars.Replace(__separators.Replace(rule, "|"), "*")).Replace("\\*", ".*").Trim('|');
			if (pattern.Length == 0)
				return null;
			if (pattern == ".*" || pattern.Equals("all", StringComparison.OrdinalIgnoreCase))
				return excludeAll ? null: __allSources;

			return new Regex("\\A(" + pattern + ")\\z", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		}
		private static readonly Regex __allSources = new Regex(".*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		private static readonly Regex __separators = new Regex(@"[\s,;]+");
		private static readonly Regex __stars = new Regex(@"\*\*+");
	}
}


