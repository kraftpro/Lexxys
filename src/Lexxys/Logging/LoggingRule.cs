// Lexxys Infrastructural library.
// file: LoggingRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Lexxys.Xml;
using System;
using System.Collections.Generic;
﻿using System.Linq;
﻿using System.Text.RegularExpressions;

namespace Lexxys.Logging
{
	[Flags]
	enum LogTypeMask
	{
		None = 0,
		Output = 1,
		Error = 2,
		Warning = 4,
		Information = 8,
		Trace = 16,
		Debug = 32,

		All = 63,
	}

	internal class LoggingRule
	{
		readonly Rule[] _rules;

		public static readonly LoggingRule Empty = new LoggingRule(null);

		private LoggingRule(Rule[] rules)
		{
			_rules = rules ?? EmptyArray<Rule>.Value;
		}

		public static string GlobalExclude { get; set; }

		public bool IsEmpty => _rules.Length == 0;

		public bool Contains(string source)
		{
			for (int i = 0; i < _rules.Length; ++i)
			{
				if (_rules[i].Contains(source))
					return true;
			}
			return false;
		}

		public bool Contains(LogType type)
		{
			for (int i = 0; i < _rules.Length; ++i)
			{
				if (_rules[i].Contains(type))
					return true;
			}
			return false;
		}
		public bool Contains(string source, LogType type)
		{
			for (int i = 0; i < _rules.Length; ++i)
			{
				if (_rules[i].Contains(type) && _rules[i].Contains(source))
					return true;
			}
			return false;
		}

		public LogTypeMask LogTypes(string source)
		{
			LogTypeMask logTypes = LogTypeMask.None;
			for (int i = 0; i < _rules.Length; ++i)
			{
				if (_rules[i].Contains(source))
				{
					logTypes |= _rules[i].LogTypes;
					if (logTypes == LogTypeMask.All)
						return logTypes;
				}
			}
			return logTypes;
		}

		public static LoggingRule Create(XmlLiteNode config)
		{
			if (config == null || config.IsEmpty)
				return Empty;

			var rr = config.Where("rule").Select(Rule.Create).Where(o=> !o.IsEmpty).ToList();
			return rr.Count == 0 ? Empty: new LoggingRule(rr.ToArray());
		}

		class Rule
		{
			private static readonly Regex __configRex = new Regex(@"[,;\|\+\s]*(-)?\s*\b(WRITE|ERROR|WARNING|INFO|INFORMATION|DEBUG|TRACE|VERBOSE|\*|ALL|ONLY)\b", RegexOptions.Compiled);
			private static readonly Regex __allSources = new Regex(".*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			private static readonly Regex __separators = new Regex(@"[\s,;]+");

			public static readonly Rule Empty = new Rule(LogTypeMask.None, null, null);

			private readonly Regex _include;
			private readonly Regex _exclude;
			private readonly LogTypeMask _types;

			private Rule(LogTypeMask types, string include, string exclude)
			{
				_types = types;
				_include = ParseRule(include, true);
				_exclude = ParseRule(GlobalExclude == null ? exclude: exclude == null ? GlobalExclude: exclude + "," + GlobalExclude, false);
			}

			public LogTypeMask LogTypes => _types;

			public bool IsEmpty => _types == LogTypeMask.None;

			public bool Contains(string source)
			{
				if (_types == LogTypeMask.None)
					return false;
				if (source == null)
					return false;
				if (_include != null)
					return _include.IsMatch(source);
				return _exclude == null || !_exclude.IsMatch(source);
			}

			public bool Contains(LogType type)
			{
				return (_types & (LogTypeMask)(1 << (int)type)) != 0;
			}

			private static Rule Create(string type, string include, string exclude)
			{
				LogTypeMask types = ParseLogType(type, LogTypeMask.All);
				return types == LogTypeMask.None ? Empty: new Rule(types, include, exclude);
			}

			public static Rule Create(XmlLiteNode config)
			{
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
			private static LogTypeMask ParseLogType(string configuration, LogTypeMask defaultValue)
			{
				if (configuration == null)
					return defaultValue;

				MatchCollection mc = __configRex.Matches(configuration.ToUpperInvariant());
				if (mc.Count == 0)
					return defaultValue;

				bool onlyFlag = false;
				LogTypeMask only = 0;
				LogTypeMask all = 0;
				foreach (Match m in mc)
				{
					LogTypeMask x = 0;
					LogTypeMask y = 0;
					bool exclude = m.Groups[1].Value == "-";
					switch (m.Groups[2].Value)
					{
						case "WRITE":
							x = LogTypeMask.Output;
							y = LogTypeMask.Output;
							break;
						case "ERROR":
							x = LogTypeMask.Error;
							y = LogTypeMask.Output | LogTypeMask.Error;
							break;
						case "WARNING":
							x = LogTypeMask.Warning;
							y = LogTypeMask.Output | LogTypeMask.Error | LogTypeMask.Warning;
							break;
						case "INFO":
						case "INFORMATION":
							x = LogTypeMask.Information;
							y = LogTypeMask.Output | LogTypeMask.Error | LogTypeMask.Warning | LogTypeMask.Information;
							break;
						case "TRACE":
						case "VERBOSE":
							x = LogTypeMask.Trace;
							y = LogTypeMask.Output | LogTypeMask.Error | LogTypeMask.Warning | LogTypeMask.Information | LogTypeMask.Trace;
							break;
						case "DEBUG":
						case "ALL":
						case "*":
							x = LogTypeMask.Debug;
							y = LogTypeMask.Output | LogTypeMask.Error | LogTypeMask.Warning | LogTypeMask.Information | LogTypeMask.Trace | LogTypeMask.Debug;
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

			private static Regex ParseRule(string rule, bool excludeAll)
			{
				if (rule == null)
					return null;
				string pattern = Regex.Escape(__separators.Replace(rule, ",")).Replace("\\*", ".*").Replace(',', '|').Trim('|');
				if (pattern.Length == 0)
					return null;
				if (pattern == ".*" || pattern.Equals("all", StringComparison.OrdinalIgnoreCase))
					return excludeAll ? null: __allSources;

				return new Regex("\\A" + pattern + "\\z", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			}
		}
	}
}


