#if false
// Lexxys Infrastructural library.
// file: LexicalContext.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Lexxys.Tokenizer2
{
	[Flags]
	public enum LexicalContextModes
	{
		None = 0,
		KeepWhiteSpace = 1,
		HandleIndent = 2,
		NotHandleWhiteSpace = 4,
		KeepNewLine = 8,
	}

	public class LexicalContext
	{
		private const int LowAscii = 0;
		private const int HighAscii = 127;

		private readonly List<LexicalTokenRule> _rules;
		private readonly List<LexicalTokenRule>[] _asciiRules;
		private readonly List<LexicalTokenRule> _extraRules;
		private LexicalContextModes _mode;
		private bool _readonly;

		public LexicalContext()
		{
			_rules = new List<LexicalTokenRule>();
			_extraRules = new List<LexicalTokenRule>();
			_asciiRules = new List<LexicalTokenRule>[HighAscii - LowAscii + 1];
		}

		public LexicalContext(LexicalContext context)
		{
			_rules = new List<LexicalTokenRule>(context._rules);
			_extraRules = new List<LexicalTokenRule>();
			_asciiRules = new List<LexicalTokenRule>[HighAscii - LowAscii + 1];
			for (int i = 0; i < _asciiRules.Length; ++i)
			{
				if (context._asciiRules[i] != null)
					_asciiRules[i] = new List<LexicalTokenRule>(context._asciiRules[i]);
			}
		}

		public LexicalContext(LexicalContextModes mode): this()
		{
			_mode = mode;
		}

		public LexicalContext(params LexicalTokenRule[] rules): this()
		{
			Add(rules);
		}

		public LexicalContext(LexicalContextModes mode, params LexicalTokenRule[] rules): this(mode)
		{
			Add(rules);
		}

		public void Add(IEnumerable<LexicalTokenRule> rules)
		{
			if (rules == null)
				throw EX.ArgumentNull("rules");

			foreach (var rule in rules)
			{
				if (rule != null)
					Add(rule);
			}
		}

		public void Add(LexicalTokenRule rule)
		{
			if (rule == null)
				throw EX.ArgumentNull("rule");
			if (_readonly)
				throw EX.NotSupported("the object is readonly");

			if (!_rules.Contains(rule))
			{
				_rules.Add(rule);

				if (rule.BeginningChars != null)
				{
					foreach (char ch in rule.BeginningChars)
					{
						if (IsAscii(ch))
						{
							List<LexicalTokenRule> r = _asciiRules[ch];
							if (r == null)
							{
								r = new List<LexicalTokenRule>();
								_asciiRules[ch] = r;
							}
							if (!r.Contains(rule))
								r.Add(rule);
						}
					}
				}
				else
				{
					for (int i = LowAscii; i <= HighAscii; ++i)
					{
						if (rule.TestBeginning((char)i))
						{
							List<LexicalTokenRule> r = _asciiRules[i];
							if (r == null)
							{
								r = new List<LexicalTokenRule>();
								_asciiRules[i] = r;
							}
							if (!r.Contains(rule))
								r.Add(rule);
						}
					}
				}
				if (rule.HasExtraBeginning)
					_extraRules.Add(rule);
			}
		}

		public IEnumerable<LexicalTokenRule> GetRules(char startingWith)
		{
			if (!IsAscii(startingWith))
				return _extraRules;

			IEnumerable<LexicalTokenRule> rr = _asciiRules[startingWith];
			return rr ?? EmptyArray<LexicalTokenRule>.Value;
		}

		public LexicalContextModes Mode
		{
			get { return _mode; }
			set
			{
				if (_readonly)
					throw EX.NotSupported("the object is readonly");
				_mode = value;
			}
		}

		public LexicalContext Clone()
		{
			return new LexicalContext(this);
		}

		public void ReadOnly()
		{
			_readonly = true;
		}

		public bool IsReadOnly => _readonly;

		private static bool IsAscii(char ch)
		{
			return ch <= '\x007F';
		}

		public override string ToString()
		{
			return String.Format(CultureInfo.InvariantCulture, "{0} rules, ({1}{2})", _rules.Count, _mode, _readonly ? " (ro)": "");
		}
	}
}
#endif