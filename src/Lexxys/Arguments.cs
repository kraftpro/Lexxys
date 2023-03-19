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
		public Arguments(IEnumerable<string>? args)
		{
			Args = new List<string>(args ?? Array.Empty<string>());
		}

		public List<string> Args { get; }

		public void Append(params string[] args) => Args.AddRange(args);

		public bool Exists(string argument, bool missingValue = false) => Value(argument, (p, d) => p.Length == 0 || p.AsBoolean(d), true, missingValue);

		public bool? Option(string argument, bool? missingValue = null) => Value(argument, (p, _) => p.Length == 0 || p.AsBoolean(false), true, missingValue);

		public T? Value<T>(string argument, T? defaultValue = default) => Value(argument, (p, d) => p.AsValue(d), defaultValue, defaultValue);

		public T? Value<T>(string argument, T? defaultValue, T? missingValue) => Value(argument, (p, d) => p.AsValue(d), defaultValue, missingValue);

		public T? Value<T>(string argument, Func<string, T?, T?> parser, T? defaultValue) => Value(argument, parser, defaultValue, defaultValue);

		public T? Value<T>(string argument, Func<string, T?, T?> parser, T? defaultValue, T? missingValue)
		{
			if (argument is null)
				throw new ArgumentNullException(nameof(argument));

			for (int i = 0; i < Args.Count; ++i)
			{
				string arg = Args[i];
				if (arg == null || (arg = arg.Trim()).Length <= 1 || arg[0] != '/' && arg[0] != '-')
					continue;
				arg = arg.Substring(1).TrimStart();
				string? v = null;
				int k = arg.IndexOfAny(Separators);
				if (k >= 0 && arg.Length > 1)
				{
					v = arg.Substring(k + 1);
					arg = arg.Substring(0, k);
					if (v.Length == 0 && i < Args.Count - 1)
						v = Args[++i];
				}
				if (Match(arg, argument))
					return
						v == null ?
							defaultValue:
						parser == null ?
							throw new ArgumentNullException(nameof(parser)):
							parser.Invoke(v, defaultValue);
			}
			return missingValue;
		}
		private static readonly char[] Separators = { ':', '=' };

		public string? First(string? defaultValue = null)
		{
			for (int i = 0; i < Args.Count; ++i)
			{
				string item = Args[i];
				if (item == null)
					continue;
				string arg = item.Trim();
				if (arg.Length > 1 && (arg[0] == '-' || arg[0] == '/'))
				{
					int k = arg.IndexOfAny(Separators);
					if (k == arg.Length - 1 && i < Args.Count - 1)
						++i;
					continue;
				}
				return item;
			}
			return defaultValue;
		}

		public IEnumerable<string> Positional => new PositionalArguments(this);

		class PositionalArguments: IEnumerable<string>
		{
			private readonly List<string> _args;

			public PositionalArguments(Arguments args)
			{
				_args = args.Args;
			}

			public IEnumerator<string> GetEnumerator()
			{
				for (int i = 0; i < _args.Count; ++i)
				{
					string item = _args[i];
					if (item == null)
						continue;
					string arg = item.Trim();
					if (arg.Length > 1 && (arg[0] == '-' || arg[0] == '/'))
					{
						int k = arg.IndexOfAny(Separators);
						if (k > 0 && k == arg.Length - 1 && i < _args.Count - 1)
							++i;
						continue;
					}
					yield return item;
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		private static bool Match(string value, string mask)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (mask is null)
				throw new ArgumentNullException(nameof(mask));

			return MatchRest(value, 0, mask.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries), 0);
		}

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

	// public class Args
	// {
	// 	public Args(string[] args)
	// 	{
	// 	}
	//
	// 	public string? Get(string property) => null;
	// 	public bool Contains(string property) => false;
	// 	public string[] Values => Array.Empty<string>();
	//
	// 	public void Select(string space) { }
	//
	// 	public string[] ArgSets() => Array.Empty<string>();
	//
	// 	public enum ParameterType
	// 	{
	// 		None,
	// 		Positional,
	// 		Required,
	// 	}
	//
	// 	public class ArgParameter
	// 	{
	// 		public string Name { get; }
	// 		public string[] Alias { get; }
	// 		public ParameterType Type { get; }
	// 		public string? Value { get; set; }
	//
	// 		public ArgParameter(string name, ParameterType type, params string[] alias)
	// 		{
	// 			if (name is null or { Length: 0 }) throw new ArgumentNullException(nameof(name));
	//
	// 			Name = name;
	// 			Type = type;
	// 			Alias = alias;
	//
	// 		}
	// 	}
	//
	// 	public static void Test(string[] args)
	// 	{
	// 		Args aa = new Args(args)
	// 			.Parameter("", ParameterType.Positional | ParameterType.Required, out ArgParameter command)
	// 			.Parameter("name", ParameterType.Positional, new[] { "nm", "mm" });
	//
	// 	}
	//
	// 	private Args Parameter(string name, ParameterType type, params string[] alias)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	//
	// 	private Args Parameter(string name, ParameterType type, out ArgParameter parameter, params string[] alias)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	// }
}


