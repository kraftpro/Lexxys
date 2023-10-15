using System.Reflection;

namespace Lexxys.Testing;

#pragma warning disable CA1724

/// <summary>
/// Provides access to the random generators loaded from resource files.
/// </summary>
public static partial class Resources
{
	/// <summary>
	/// Loads the random generators from the specified resource files.
	/// </summary>
	/// <param name="resourceFiles">Collection of the resource files to load.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IDictionary<string, RandItem<string>> LoadResources(params string[] resourceFiles)
	{
		if (resourceFiles is not { Length: >0 }) throw new ArgumentNullException(nameof(resourceFiles));

		var dd = new List<string>();
		var d1 = Directory.GetCurrentDirectory();
		var d2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		var d3 = Path.GetDirectoryName(typeof(Resources).Assembly.Location);
		if (d1 != null && d1.Length > 0)
			dd.Add(d1);
		if (d2 != null && d2.Length > 0 && d2 != d1)
			dd.Add(d2);
		if (d3 != null && d3.Length > 0 && d3 != d1 && d3 != d2)
			dd.Add(d3);

		var resource = new SafeDictionary<string, RandItem<string>>(StringComparer.OrdinalIgnoreCase, RandItem<string>.Empty);

		foreach (var resourceFile in resourceFiles)
		{
			foreach (var location in dd)
			{
				string path = Path.Combine(location, resourceFile);
				IEnumerable<string> files = path.Contains('?') || path.Contains('*') ? Directory.EnumerateFiles(Path.GetDirectoryName(path) ?? ".", Path.GetFileName(path)): new[] { path };
				bool found = false;
				foreach (var file in files)
				{
					if (!File.Exists(file)) continue;
					var j = (JsonMap)JsonParser.Parse(File.ReadAllText(file));
					foreach (var (name, item) in j)
					{
						var i = ParseItem(item, resource);
						resource[name] = i;
					}
					found = true;
				}
				if (found) break;
			}
		}
		return resource;
	}

	private static RandItem<string> ParseItem(JsonItem value, IReadOnlyDictionary<string, RandItem<string>> resources) =>
		value switch
		{
			JsonScalar v => ParseItem(v, resources),
			JsonMap m => ParseItem(m, resources),
			JsonArray a => ParseItem(a, resources),
			_ => RandItem<string>.Empty,
		};

	private static RandItem<string> ParseItem(JsonScalar scalar, IReadOnlyDictionary<string, RandItem<string>> resources) =>
		IsReference(scalar) ? CreateReference(scalar, resources): R.I(scalar.Value == null ? String.Empty: scalar.Text);

	private static bool IsReference(JsonScalar scalar) => scalar.Text.Length > 0 && scalar.Text[0] == '@';

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

		var items = new List<RandItem<string>>();
		foreach (var item in value)
		{
			if (item is JsonScalar scalar)
				items.Add(ParsePair(1, scalar, resources));
			else if (item is JsonArray array)
				items.Add(R.I(1, ParseItem(array, resources)));
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

	private static RandItem<string> ParsePair(double weight, JsonItem value, IReadOnlyDictionary<string, RandItem<string>> resources)
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

	private static RandItem<string> ParsePair(double weight, JsonScalar value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (weight <= 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		return IsReference(value) ? R.I(weight, CreateReference(value, resources)): R.I(weight, value.Value == null ? String.Empty: value.Text);
	}

	private static RandItem<string> ParsePair(double weight, JsonArray value, IReadOnlyDictionary<string, RandItem<string>> resources)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		if (value.Count == 0)
			throw new ArgumentOutOfRangeException(nameof(value), value, null);
		if (weight <= 0)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, null);

		return value.Count > 1 ? R.I(weight, ParseItem(value, resources)): ParsePair(weight, value[0]!, resources);
	}

	private static RandItem<string> ParsePair(double weight, JsonMap value, IReadOnlyDictionary<string, RandItem<string>> resources)
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

		return R.I(weight, ParseItem(value, resources));
	}
}
