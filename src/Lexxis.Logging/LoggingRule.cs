// Lexxys Infrastructural library.
// file: LoggingRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lexxys.Logging;

[Flags]
public enum LogTypeFilter
{
	None = 0,
	OutputOnly = 1,
	CriticalOnly = 1,
	ErrorOnly = 2,
	WarningOnly = 4,
	InformationOnly = 8,
	DebugOnly = 16,
	TraceOnly = 32,

	Output = 1,
	Critical = 1,
	Error = Output + ErrorOnly,
	Warning = Error + WarningOnly,
	Information = Warning + InformationOnly,
	Debug = Information + DebugOnly,
	Trace = Debug + TraceOnly,

	All = Trace,
}

internal class LoggingRule
{
	readonly Rule[] _rules;

	public static readonly LoggingRule Empty = new LoggingRule(null);

	private LoggingRule(Rule[]? rules)
	{
		_rules = rules ?? Array.Empty<Rule>();
	}

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

	public static LoggingRule Create(ICollection<LogWriterFilter>? rules, string? include, string? exclude, LogType? logLevel)
	{
		if (rules == null || rules.Count == 0)
			return Empty;

		Rule[] rr = rules
			.Select(o => Rule.TryCreate(o, include, exclude, logLevel))
			.Where(o => o != null && !o.IsEmpty)
			.ToArray()!;
		return rr.Length == 0 ? Empty: new LoggingRule(rr);
	}


	class Rule
	{
		public static readonly Rule Empty = new Rule(LogTypeFilter.None, null, null, null);

		private readonly Regex? _include;
		private readonly Regex? _exclude;
		private readonly LogTypeFilter _types;

		public Rule(LogTypeFilter? types, string? include, string? exclude, LogType? minLogLevel)
		{
			_types = types.GetValueOrDefault() | (minLogLevel == null ? 0: (LogTypeFilter)~(~1 << (int)minLogLevel.GetValueOrDefault()));
			_include = ParseRule(include, true);
			_exclude = ParseRule(exclude, false);
			if (_include == null && _exclude == __allSources)
				_types = LogTypeFilter.None;
		}

		public LogTypeFilter LogTypes => _types;

		public bool IsEmpty => _types == LogTypeFilter.None;

		public bool Contains(string? source)
			=> _types != LogTypeFilter.None && source != null && (_include?.IsMatch(source) ?? !_exclude?.IsMatch(source) ?? true);

		public bool Contains(LogType type) => (_types & (LogTypeFilter)(1 << (int)type)) != 0;

		public static Rule? TryCreate(LogWriterFilter? filter, string? include, string? exclude, LogType? minLogLevel)
		{
			return filter == null || filter.LogType == LogTypeFilter.None ? null: new Rule(filter.LogType, Join(filter.Include, include), Join(filter.Exclude, exclude), minLogLevel);
			string? Join(string? one, string? two) => one == null ? two: two == null ? one: one + "," + two;
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
					case "CRITICAL":
					case "WRITE":
						x = LogTypeFilter.OutputOnly;
						y = LogTypeFilter.Output;
						break;
					case "ERROR":
						x = LogTypeFilter.ErrorOnly;
						y = LogTypeFilter.Error;
						break;
					case "WARNING":
						x = LogTypeFilter.WarningOnly;
						y = LogTypeFilter.Warning;
						break;
					case "INFO":
					case "INFORMATION":
						x = LogTypeFilter.InformationOnly;
						y = LogTypeFilter.Information;
						break;
					case "DEBUG":
						x = LogTypeFilter.DebugOnly;
						y = LogTypeFilter.Debug;
						break;
					case "TRACE":
						x = LogTypeFilter.TraceOnly;
						y = LogTypeFilter.Trace;
						break;
					case "VERBOSE":
					case "ALL":
					case "*":
						x = LogTypeFilter.TraceOnly;
						y = LogTypeFilter.Trace;
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
		private static readonly Regex __configRex = new Regex(@"[,;\|\+\s]*(-)?\s*\b(WRITE|CRITICAL|ERROR|WARNING|INFO|INFORMATION|DEBUG|TRACE|VERBOSE|\*|ALL|ONLY)\b", RegexOptions.Compiled);

		/// <summary>
		/// Converts wildcard rule to regular expression.
		/// <code>
		/// wildcard := item [ item ]*
		/// item     := text
		///          | '(' wildcard ')' -- group
		///          | '[' wildcard ']' -- optional group
		///          | '*'              -- zero or more chars
		///          | ';' | ','        -- OR separator
		/// </code>
		/// </summary>
		/// <param name="rule">The wildcard rule to parse.</param>
		/// <param name="excludeAll">Indicates that function should return null instead on ".*" expression.</param>
		/// <returns></returns>
		private static Regex? ParseRule(string? rule, bool excludeAll)
		{
			if (rule == null)
				return null;
			string pattern = Regex.Escape(__stars.Replace(__separators.Replace(rule, ","), "*").Trim(','))
				.Replace(@"\*", ".*")
				.Replace(@"\(", "(")
				.Replace(@"\)", ")")
				.Replace(@"\[", "(")
				.Replace(@"]", ")?");
			if (pattern.Length == 0)
				return null;
			var chunks = pattern.Split(',').Distinct().ToList();
			if (chunks.Any(o => o == ".*" || o.Equals("all", StringComparison.OrdinalIgnoreCase)))
				return excludeAll ? null : __allSources;

			try
			{
				return new Regex(@"\A(" + String.Join("|", chunks) + @")\z", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(rule), rule);
				throw;
			}
		}
		private static readonly Regex __allSources = new Regex(".*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		private static readonly Regex __separators = new Regex(@"[\s,;|]+");
		private static readonly Regex __stars = new Regex(@"\*\*+");
	}
}


