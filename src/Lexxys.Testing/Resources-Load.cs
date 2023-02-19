﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lexxys.Testing;

#pragma warning disable CA1724
#pragma warning disable CA1307 // StringCompatrison parameter

public static partial class Resources
{
	private static SafeDictionary<string, RandItem<string>> LoadResources(params string[] resourceFiles)
	{
		if (resourceFiles is not { Length: >0 })
			throw new ArgumentNullException(nameof(resourceFiles));

		var asm = Assembly.GetExecutingAssembly();
		var resource = new SafeDictionary<string, RandItem<string>>(StringComparer.OrdinalIgnoreCase, RandItem<string>.Empty);

		foreach (var resourceFile in resourceFiles)
		{
			string location = asm.Location;
			if (location.Length > 0)
				location = Path.GetDirectoryName(location) ?? String.Empty;
			string path = Path.Combine(location, resourceFile);
			IEnumerable<string> files = path.Contains('?') || path.Contains('*') ? Directory.EnumerateFiles(Path.GetDirectoryName(path) ?? ".", Path.GetFileName(path)): new[] { path };
			foreach (var file in files)
			{
				var j = (JsonMap)JsonParser.Parse(File.ReadAllText(file));
				foreach (var (name, item) in j)
				{
					var i = ParseItem(item, resource);
					resource[name] = i;
				}
			}
		}
		return resource;
	}

	private static RandItem<string> ParseItem(JsonItem value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		return value switch
		{
			JsonScalar v => ParseItem(v, resources),
			JsonMap m => ParseItem(m, resources),
			JsonArray a => ParseItem(a, resources),
			_ => RandItem<string>.Empty,
		};
	}

	private static RandItem<string> ParseItem(JsonScalar scalar, IReadOnlyDictionary<string, RandItem<string>> resources) =>
		IsReference(scalar) ? CreateReference(scalar, resources): R.V(scalar.Value == null ? String.Empty: scalar.Text);

	private static bool IsReference(JsonScalar scalar) => scalar?.Text?.Length > 0 && scalar.Text[0] == '@';

	private static RandItem<string> CreateReference(JsonScalar value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		if (resources == null)
			throw new ArgumentNullException(nameof(resources));

		var key = value.Text.Length > 0 && value.Text[0] == '@' ? value.Text.Substring(1): value.Text;
		return key.Length == 0 ?
			RandItem<string>.Empty:
			new RandItem<string>(() => resources.GetValueOrDefault(key, RandItem<string>.Empty).NextValue());
	}

	private static RandItem<string> ParseItem(JsonArray value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		if (resources == null)
			throw new ArgumentNullException(nameof(resources));

		var items = new List<WeightValuePair<string>>();
		foreach (var item in value)
		{
			if (item is JsonScalar scalar)
				items.Add(ParsePair(1, scalar, resources));
			else if (item is JsonArray array)
				items.Add(R.P(1, ParseItem(array, resources)));
			else if (item is JsonMap map)
				items.Add(ParsePair(1, map, resources));
		}
		return new RandItem<string>(items);
	}

	private static RandItem<string> ParseItem(JsonMap value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		if (value["type"] is not JsonScalar type)
			throw new ArgumentOutOfRangeException(nameof(value), value, null);

		if (String.Equals(type.Text, "reference", StringComparison.OrdinalIgnoreCase))
		{
			if (value["value"] is JsonScalar v)
				return CreateReference(v, resources);
		}
		else if (String.Equals(type.Text, "choice", StringComparison.OrdinalIgnoreCase))
		{
			var items = value["items"];
			if (items is JsonArray a)
				return ParseItem(a, resources);
		}
		else if (String.Equals(type.Text, "concat", StringComparison.OrdinalIgnoreCase))
		{
			var delimiter = value["delimiter"]?.Text ?? " ";
			var items = value["items"];
			if (items is JsonArray a)
			{
				var ii = a.Select(o => ParseItem(o, resources)).ToList();
				return new RandItem<string>(() => String.Join(delimiter, ii.Select(o => o.NextValue()).Where(o => !String.IsNullOrEmpty(o))));
			}
		}

		throw new ArgumentOutOfRangeException(nameof(value), value, null);
	}

	private static WeightValuePair<string> ParsePair(double weight, JsonItem value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (weight <= 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		return value switch
		{
			JsonScalar scalar => ParsePair(weight, scalar, resources),
			JsonArray array => ParsePair(weight, array, resources),
			JsonMap map => ParsePair(weight, map, resources),
			_ => throw new InvalidOperationException(),
		};
	}

	private static WeightValuePair<string> ParsePair(double weight, JsonScalar value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (weight <= 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		return IsReference(value) ? R.P(weight, CreateReference(value, resources)): R.P(weight, value.Value == null ? String.Empty: value.Text);
	}

	private static WeightValuePair<string> ParsePair(double weight, JsonArray value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		if (value.Count == 0)
			throw new ArgumentOutOfRangeException(nameof(value), value, null);
		if (weight <= 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);

		return value.Count > 1 ? R.P(weight, ParseItem(value, resources)): ParsePair(weight, value[0]!, resources);
	}

	private static WeightValuePair<string> ParsePair(double weight, JsonMap value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (weight <= 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		if (value["weight"] is JsonScalar w)	// { wight:0.5, value:... }
		{
			var v = value["value"];
			if (v == null)
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			return ParsePair(weight * w.DoubleValue, v, resources);
		}

		return R.P(weight, ParseItem(value, resources));
	}
}
