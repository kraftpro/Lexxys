// Lexxys Infrastructural library.
// file: Arguments.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

/// <summary>
/// Command line arguments parser.
/// </summary>
public class Arguments
{
	private readonly List<string> _args;

	/// <summary>
	/// Initializes a new instance of the <see cref="Arguments"/> class.
	/// </summary>
	public Arguments(IEnumerable<string>? args)
	{
		_args = new List<string>(args ?? Array.Empty<string>());
	}

	/// <summary>
	/// Command line arguments
	/// </summary>
	public IList<string> Args => _args;

	/// <summary>
	/// Checks if the specified flag is provided. (i.e. -flag or -flag:true ... )
	/// </summary>
	/// <param name="argument">Name of the parameter</param>
	/// <returns></returns>
	public bool Option(string argument) => Value(argument, (p, d) => p.Length == 0 || p.AsBoolean(d), true, false);

	/// <summary>
	/// Checks if the specified flag is provided. (i.e. -flag or -flag:true or -flag:false ... )
	/// </summary>
	/// <param name="argument">Name of the parameter</param>
	/// <param name="missingValue">The value returned when the specified <paramref name="argument"/> not found in the arguments list</param>
	/// <returns></returns>
	public bool? Option(string argument, bool? missingValue) => Value(argument, (p, _) => p.Length == 0 || p.AsBoolean(false), true, missingValue);

	/// <summary>
	/// Returns value for the specified <paramref name="argument"/>.
	/// </summary>
	/// <typeparam name="T">Type of the parameter value.</typeparam>
	/// <param name="argument">Name of the parameter</param>
	/// <returns></returns>
	public T? Value<T>(string argument) => Value<T?>(argument, (p, d) => p.AsValue(d), default, default);

	/// <summary>
	/// Returns value for the specified <paramref name="argument"/>.
	/// </summary>
	/// <typeparam name="T">Type of the parameter value.</typeparam>
	/// <param name="argument">Name of the parameter</param>
	/// <param name="defaultValue">The value returned if the system cannot find the parameter of parse the parameter value</param>
	/// <returns></returns>
	public T Value<T>(string argument, T defaultValue) => Value(argument, (p, d) => p.AsValue(d), defaultValue, defaultValue);

	/// <summary>
	/// Returns value for the specified <paramref name="argument"/>.
	/// </summary>
	/// <typeparam name="T">Type of the parameter value.</typeparam>
	/// <param name="argument">Name of the parameter</param>
	/// <param name="defaultValue">The value returned the parameter value is not provided or cannot be parsed.</param>
	/// <param name="missingValue">The value returned if the parameter is absent.</param>
	/// <returns></returns>
	public T Value<T>(string argument, T defaultValue, T missingValue) => Value(argument, (p, d) => p.AsValue(d), defaultValue, missingValue);

	/// <summary>
	/// Returns value for the specified <paramref name="argument"/>.
	/// </summary>
	/// <typeparam name="T">Type of the parameter value.</typeparam>
	/// <param name="argument">Name of the parameter</param>
	/// <param name="parser">The parameter value parser</param>
	/// <param name="defaultValue">The value returned if the system cannot find the parameter of parse the parameter value</param>
	/// <returns></returns>
	public T Value<T>(string argument, Func<string, T, T> parser, T defaultValue) => Value(argument, parser, defaultValue, defaultValue);

	/// <summary>
	/// Returns value for the specified <paramref name="argument"/>.
	/// </summary>
	/// <typeparam name="T">Type of the parameter value.</typeparam>
	/// <param name="argument">Name of the parameter</param>
	/// <param name="parser">The parameter value parser</param>
	/// <param name="defaultValue">The value returned the parameter value is not provided or cannot be parsed.</param>
	/// <param name="missingValue">The value returned if the parameter is absent.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public T Value<T>(string argument, Func<string, T, T> parser, T defaultValue, T missingValue)
	{
		if (argument is null)
			throw new ArgumentNullException(nameof(argument));

		for (int i = 0; i < _args.Count; ++i)
		{
			string arg = _args[i];
			if (arg == null || (arg = arg.Trim()).Length <= 1 || arg[0] != '/' && arg[0] != '-')
				continue;
			arg = arg.Substring(1).TrimStart();
			string? v = null;
			int k = arg.IndexOfAny(Separators);
			if (k >= 0 && arg.Length > 1)
			{
				v = arg.Substring(k + 1);
				arg = arg.Substring(0, k);
				if (v.Length == 0 && i < _args.Count - 1)
					v = _args[++i];
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

	/// <summary>
	/// Returns first position argument or the <paramref name="defaultValue"/>.
	/// </summary>
	/// <param name="defaultValue">Default argument value.</param>
	/// <returns></returns>
	public string? First(string? defaultValue = null)
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
				if (k == arg.Length - 1 && i < _args.Count - 1)
					++i;
				continue;
			}
			return item;
		}
		return defaultValue;
	}

	/// <summary>
	/// Enumerates the positional arguments.
	/// </summary>
	public IEnumerable<string> Positional => new PositionalArguments(this);

	class PositionalArguments: IEnumerable<string>
	{
		private readonly List<string> _args;

		public PositionalArguments(Arguments args)
		{
			_args = args._args;
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


