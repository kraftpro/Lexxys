using Lexxys.Xml;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Lexxys.Configuration;

/// <summary>
/// A <see cref="Lexxys.Xml.TextToXmlConverter">config.txt</see> file based <see cref="ConfigurationProvider"/>.
/// </summary>
public class TextFileConfigurationProvider: FileConfigurationProvider
{
	public TextFileConfigurationProvider(FileConfigurationSource source) : base(source)
	{
	}

	public override void Load(Stream stream) => Data = TextConfigurationParser.Parse(stream, Source.Path);
}

/// <summary>
/// A <see cref="Lexxys.Xml.TextToXmlConverter">config.txt</see> file based <see cref="StreamConfigurationProvider"/>.
/// </summary>
public class TextStreamConfigurationProvider: StreamConfigurationProvider
{
	public TextStreamConfigurationProvider(StreamConfigurationSource source) : base(source)
	{
	}

	public override void Load(Stream stream) => Data = TextConfigurationParser.Parse(stream);
}

/// <summary>
/// Represents <see cref="Lexxys.Xml.TextToXmlConverter">config.txt</see> file as an <see cref="IConfigurationSource"/>.
/// </summary>
public class TextFileConfigurationSource: FileConfigurationSource
{
	public override IConfigurationProvider Build(IConfigurationBuilder builder)
	{
		EnsureDefaults(builder);
		return new TextFileConfigurationProvider(this);
	}
}

/// <summary>
/// Represents <see cref="Lexxys.Xml.TextToXmlConverter">config.txt</see> file as an <see cref="IConfigurationSource"/>.
/// </summary>
public class TextStreamConfigurationSource: StreamConfigurationSource
{
	public override IConfigurationProvider Build(IConfigurationBuilder builder) => new TextStreamConfigurationProvider(this);
}

static class TextConfigurationParser
{
	public static IDictionary<string, string?> Parse(Stream stream, string? path = null)
	{
		using var reader = new StreamReader(stream);
		var xml = TextToXmlConverter.ConvertLite(reader.ReadToEnd(), path);
		return xml.Count == 1 && String.Equals(xml[0].Name, "configuration", StringComparison.OrdinalIgnoreCase) ? Collect(xml[0].Elements): Collect(xml);
	}

	private static IDictionary<string, string?> Collect(IEnumerable<IXmlReadOnlyNode> xmlItems)
	{
		var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		foreach (var xml in xmlItems)
		{
			ScanNode(xml, xml.Name, map);
		}
		return map;
	}

	private static void ScanNode(IXmlReadOnlyNode xml, string? node, Dictionary<string, string?> map)
	{
		if (node != null)
		{
			if (!String.IsNullOrEmpty(xml.Value))
				map.Add(node, xml.Value);
			node += ConfigurationPath.KeyDelimiter;
		}

		List<string>? keys = null;

		foreach (var item in xml.Attributes)
		{
			var key = node + item.Key;
			if (!xml.Element(item.Key).IsEmpty)
			{
				(keys ??= new List<string>()).Add(item.Key);
				key += ConfigurationPath.KeyDelimiter + "0";
			}
			map.Add(key, item.Value);
		}

		foreach (var items in xml.Elements.GroupBy(o => o.Name, StringComparer.OrdinalIgnoreCase))
		{
			if (String.Equals(items.Key, "item", StringComparison.OrdinalIgnoreCase))
			{
				int index = 0;
				foreach (var item in items)
					map.Add(node + index++, item.Value);
			}
			else if (items.Count() == 1)
			{
				var item = items.First();
				var key = node + items.Key;
				if (keys != null && keys.Contains(items.Key, StringComparer.OrdinalIgnoreCase))
					key += ConfigurationPath.KeyDelimiter + "1";
				ScanNode(item, key, map);
			}
			else
			{
				int index = keys != null && keys.Contains(items.Key, StringComparer.OrdinalIgnoreCase) ? 1: 0;
				var key = node + items.Key + ConfigurationPath.KeyDelimiter;
				foreach (var item in items)
				{
					ScanNode(item, key + index++, map);
				}
			}
		}
	}
}

public static class TextFileConfigurationExtensions
{
	/// <summary>
	/// Adds the <see cref="Lexxys.Xml.TextToXmlConverter">config.txt</see> configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
	/// </summary>
	/// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
	/// <param name="provider">The <see cref="IFileProvider"/> to use to access the file.</param>
	/// <param name="path"></param>Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.
	/// <param name="optional">Whether the file is optional.</param>
	/// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IConfigurationBuilder AddTextFile(this IConfigurationBuilder builder, IFileProvider? provider, string path, bool optional = false, bool reloadOnChange = false)
	{
		if (builder is null)
			throw new ArgumentNullException(nameof(builder));
		if (path is null || path.Length <= 0)
			throw new ArgumentNullException(nameof(path));

		return builder.AddTextFile(s =>
		{
			s.FileProvider = provider;
			s.Path = path;
			s.Optional = optional;
			s.ReloadOnChange = reloadOnChange;
			s.ResolveFileProvider();
		});
	}

	/// <summary>
	/// Adds the <see cref="Lexxys.Xml.TextToXmlConverter">config.txt</see> configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
	/// </summary>
	/// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
	/// <param name="path"></param>Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.
	/// <param name="optional">Whether the file is optional.</param>
	/// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IConfigurationBuilder AddTextFile(this IConfigurationBuilder builder, string path, bool optional = false, bool reloadOnChange = false)
		=> AddTextFile(builder, null, path, optional, reloadOnChange);

	/// <summary>
	/// Adds a <see cref="Lexxys.Xml.TextToXmlConverter">config.txt</see> configuration source.
	/// </summary>
	/// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
	/// <param name="configureSource">Configures the source.</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IConfigurationBuilder AddTextFile(this IConfigurationBuilder builder, Action<TextFileConfigurationSource> configureSource)
	{
		if (builder == null)
			throw new ArgumentNullException(nameof(builder));

		return builder.Add(configureSource);
	}

	/// <summary>
	/// Adds the <see cref="Lexxys.Xml.TextToXmlConverter">config.txt</see> configuration provider at <paramref name="stream"/> to <paramref name="builder"/>.
	/// </summary>
	/// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
	/// <param name="stream">The stream containing the configuration data</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IConfigurationBuilder AddTextStream(this IConfigurationBuilder builder, Stream stream)
	{
		if (builder == null)
			throw new ArgumentNullException(nameof(builder));

		return builder.Add<TextStreamConfigurationSource>(s => s.Stream = stream);
	}
}
