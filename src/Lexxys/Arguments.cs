// Lexxys Infrastructural library.
// file: Arguments.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

namespace Lexxys
{
	public class Arguments
	{
		public Arguments(IEnumerable<string> args)
		{
			Args = new List<string>(args);
		}

		public List<string> Args { get; }

		public void Append(params string[] args) => Args.AddRange(args);

		public bool Exists(string argument, bool missingValue = false) => Value(argument, (p, d) => p.Length == 0 || p.AsBoolean(d), true, missingValue);

		public string Value(string argument, string defaultValue) => Value(argument, (o, d) => o.TrimToNull() ?? d, defaultValue, defaultValue);

		public string Value(string argument, string defaultValue, string missingValue) => Value(argument, (o, d) => o.TrimToNull() ?? d, defaultValue, missingValue);

		public int Value(string argument, int defaultValue) => Value(argument, (o, d) => o.AsInt32(d), defaultValue, defaultValue);

		public int Value(string argument, int defaultValue, int missingValue) => Value(argument, (o, d) => o.AsInt32(d), defaultValue, missingValue);

		public decimal Value(string argument, decimal defaultValue) => Value(argument, (o, d) => o.AsDecimal(d), defaultValue, defaultValue);

		public decimal Value(string argument, decimal defaultValue, decimal missingValue) => Value(argument, (o, d) => o.AsDecimal(d), defaultValue, missingValue);

		public double Value(string argument, double defaultValue) => Value(argument, (o, d) => o.AsDouble(d), defaultValue, defaultValue);

		public double Value(string argument, double defaultValue, double missingValue) => Value(argument, (o, d) => o.AsDouble(d), defaultValue, missingValue);

		public DateTime Value(string argument, DateTime defaultValue) => Value(argument, (o, d) => o.AsDateTime(d), defaultValue, defaultValue);

		public DateTime Value(string argument, DateTime defaultValue, DateTime missingValue) => Value(argument, (o, d) => o.AsDateTime(d), defaultValue, missingValue);

		public TimeSpan Value(string argument, TimeSpan defaultValue) => Value(argument, (o, d) => o.AsTimeSpan(d), defaultValue, defaultValue);

		public TimeSpan Value(string argument, TimeSpan defaultValue, TimeSpan missingValue) => Value(argument, (o, d) => o.AsTimeSpan(d), defaultValue, missingValue);

		public T Value<T>(string argument, T defaultValue) => Value(argument, (p, d) => p.AsValue(d), defaultValue, defaultValue);

		public T Value<T>(string argument, T defaultValue, T missingValue) => Value(argument, (p, d) => p.AsValue(d), defaultValue, missingValue);

		public T Value<T>(string argument, Func<string, T, T> parser, T defaultValue) => Value(argument, parser, defaultValue, defaultValue);

		public T Value<T>(string argument, Func<string, T, T> parser, T defaultValue, T missingValue)
		{
			foreach (var item in Args)
			{
				string arg = item;
				if (arg != null && (arg = arg.Trim()).Length > 0 && (arg[0] == '/' || arg[0] == '-'))
				{
					arg = arg.Substring(1).TrimStart();
					if (arg.Length == 0)
						arg = "-";
					int i = arg.IndexOfAny(Separators);
					string v = null;
					if (i >= 0)
					{
						v = arg.Substring(i + 1);
						arg = i == 0 ? ":": arg.Substring(0, i);
					}
					if (Match(arg, argument))
						return v == null ? defaultValue: parser(v, defaultValue);
				}
			}
			return missingValue;
		}
		private static readonly char[] Separators = { ':', '=' };

		public IEnumerable<string> Positional => new PositionalArguments(this);

		class PositionalArguments: IEnumerable<string>
		{
			private readonly Arguments _args;

			public PositionalArguments(Arguments args)
			{
				_args = args;
			}

			public IEnumerator<string> GetEnumerator()
			{
				foreach (var item in _args.Args)
				{
					if (item == null)
						continue;
					string arg = item.TrimStart();
					if (arg.Length > 0 && (arg[0] == '-' || arg[0] == '/'))
						continue;
					yield return item;
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public string First(string defaultValue = null)
		{
			foreach (var item in Args)
			{
				if (item == null)
					continue;
				string arg = item.TrimStart();
				if (arg.Length > 0 && (arg[0] == '-' || arg[0] == '/'))
					continue;
				return item;
			}
			return defaultValue;
		}

		//public static string Parameter(string value, params string[] variants)
		//{
		//	if (value == null || (value = value.Trim()).Length == 0 || !(value[0] == '/' || value[0] == '-'))
		//		return null;

		//	value = value.Substring(1).TrimStart();
		//	if (value.Length == 0)
		//		value = "-";

		//	return Select(value, variants);
		//}

		//public static string[] Parameter(string value, char separator, params string[] variants)
		//{
		//	if (value == null || (value = value.Trim()).Length == 0 || !(value[0] == '/' || value[0] == '-'))
		//		return new string[2];

		//	value = value.Substring(1).TrimStart();
		//	if (value.Length == 0)
		//		value = "-";

		//	var result = Split(value, separator);
		//	result[0] = Select(result[0], variants);
		//	return result;
		//}


		public static string Select(string value, params string[] variants)
		{
			if (value == null || (value = value.Trim()).Length == 0)
				return null;

			string match = null;
			foreach (var arg in variants)
			{
				if (Match(value, arg))
				{
					if (match != null)
						return null;
					match = arg;
				}
			}
			return match;
		}

		public static string[] Split(string arg, char separator)
		{
			int i = arg.IndexOf(separator);
			return i < 0 ?
				new[] { NullIfEmpty(arg), null }:
				new[] { NullIfEmpty(arg.Substring(0, i)), NullIfEmpty(arg.Substring(i + 1)) };
		}

		private static string NullIfEmpty(string value) => String.IsNullOrWhiteSpace(value) ? null: value;

		public static bool Match(string value, string mask) => MatchRest(value, 0, mask.Split((char[])null, StringSplitOptions.RemoveEmptyEntries), 0);

		private static bool MatchRest(string value, int valueIndex, string[] parts, int maskIndex)
		{
			if (maskIndex == parts.Length)
				return valueIndex == value.Length;
			if (valueIndex == value.Length)
				return false;
			string mask = parts[maskIndex];
			if (maskIndex == parts.Length - 1)
				return mask.StartsWith(value.Substring(valueIndex), StringComparison.OrdinalIgnoreCase);

			for (int i = 1; i <= mask.Length; ++i)
			{
				if (valueIndex + i > value.Length)
					return false;
				if (!mask.StartsWith(value.Substring(valueIndex, i), StringComparison.OrdinalIgnoreCase))
					return false;
				if (MatchRest(value, valueIndex + i, parts, maskIndex + 1))
					return true;
			}
			return false;
		}
	}
}


