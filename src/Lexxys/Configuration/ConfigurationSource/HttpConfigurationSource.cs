namespace Lexxys.Configuration;

using Xml;

public class HttpConfigurationSource: IXmlConfigurationSource
{
	private const string LogSource = "Lexxys.Configuration.HttpConfigurationSource";
	private readonly Func<string, string?, IReadOnlyList<XmlLiteNode>> _converter;
	private List<string>? _includes;
	private IReadOnlyList<XmlLiteNode>? _content;
	private int _version;

	public HttpConfigurationSource(Uri location, IReadOnlyCollection<string> parameters)
	{
		if (location == null)
			throw new ArgumentNullException(nameof(location));
		if (!(location.IsAbsoluteUri && (location.Scheme == Uri.UriSchemeHttp || location.Scheme == Uri.UriSchemeHttps)))
			throw new ArgumentOutOfRangeException(nameof(location), location, null);

		Name = location.ToString();
		Location = location;
#if NETCOREAPP
		var (type, text) = GetContentTypeAsync(location).GetAwaiter().GetResult();
#else
		var (type, text) = GetContentType(location);
#endif
		_converter = XmlConfigurationProvider.GetSourceConverter(type, OptionHandler, parameters);
		if (text != null)
			_content = _converter(text, Name);
		_version = 1;
	}

	public string Name { get; }

	public Uri Location { get; }

	public int Version => _version;

	public IReadOnlyList<XmlLiteNode> Content
	{
		get
		{
			if (_content == null)
			{
				lock (Location)
				{
					_content ??= _converter(GetText(Location), Name);
				}
			}
			return _content;
		}
	}

	private static string GetText(Uri location)
	{
#if NETCOREAPP
		using var client = new System.Net.Http.HttpClient();
		return client.GetStringAsync(location).GetAwaiter().GetResult();
#else
		using var c = new System.Net.WebClient();
		return c.DownloadString(location);
#endif
	}

#if NETCOREAPP
	private static async Task<(string Type, string? Text)> GetContentTypeAsync(Uri location)
	{
		var type = TypeByUrl(location.ToString());
		if (type != null)
			return (type, null);

		using var client = new System.Net.Http.HttpClient();
		var message = await client.GetAsync(location, System.Net.Http.HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
		if (!message.IsSuccessStatusCode)
			throw new InvalidOperationException($"GET {location} returns status code {((int)message.StatusCode)}.");
		if (!message.Headers.TryGetValues("Content-Type", out var contentType)) // || (contentType = contentTypes.FirstOrDefault(o => o.IndexOf('/') > 0)) == null)
			throw new InvalidOperationException($"GET {location} doesn't have content type specified.");

		type = TypeByContentType(contentType, location);
		var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
		return (type, content);
	}
#else
	private static (string Type, string? Text) GetContentType(Uri location)
	{
		var type = TypeByUrl(location.ToString());
		if (type != null)
			return (type, null);
		var rq = System.Net.WebRequest.CreateHttp(location);

		using var rp = (System.Net.HttpWebResponse)rq.GetResponse();

		if ((int)rp.StatusCode / 100 != 2)
			throw new InvalidOperationException($"GET {location} returns status code {((int)rp.StatusCode)}.");

		type = TypeByContentType(rp.ContentType.Split(';'), location);

		var stream = rp.GetResponseStream();
		if (stream == null)
			return (type, null);

		using var reader = new StreamReader(stream);
		var content = reader.ReadToEnd();
		return (type, content);
	}
#endif

	private static string? TypeByUrl(string value)
	{
		return
			value.IndexOf("type=txt", StringComparison.Ordinal) > 0 ? "txt":
			value.IndexOf("type=xml", StringComparison.Ordinal) > 0 ? "xml":
			value.IndexOf("type=ini", StringComparison.Ordinal) > 0 ? "ini":
			value.IndexOf("type=json", StringComparison.Ordinal) > 0 ? "json":
			value.IndexOf(".txt", StringComparison.Ordinal) > 0 ? "txt":
			value.IndexOf(".xml", StringComparison.Ordinal) > 0 ? "xml":
			value.IndexOf(".ini", StringComparison.Ordinal) > 0 ? "ini":
			value.IndexOf(".json", StringComparison.Ordinal) > 0 ? "json": null;
	}

	private static string TypeByContentType(IEnumerable<string>? contentType, Uri location)
	{
		if (contentType == null)
			throw new InvalidOperationException($"GET {location} doesn't have content type specified.");

		// ReSharper disable once PossibleMultipleEnumeration
		var type  = contentType.FirstOrDefault(o => o.IndexOf('/') > 0)?.Trim() ?? "";
		if (type.EndsWith("/json", StringComparison.Ordinal))
			return "json";
		if (type.EndsWith("/xml", StringComparison.Ordinal))
			return "xml";
		if (type.EndsWith("/ini", StringComparison.Ordinal))
			return "xml";
		if (type == "text/plain")
			return "txt";
		// ReSharper disable once PossibleMultipleEnumeration
		throw new InvalidOperationException($"GET {location} returns unsupported content type \"{String.Join(";", contentType)}\".");
	}

	public event EventHandler<ConfigurationEventArgs>? Changed;

	private void OnChanged(object? sender, ConfigurationEventArgs e)
	{
		try
		{
			lock (Location)
			{
				_content = null;
				Changed?.Invoke(sender ?? this, e);
				++_version;
			}
		}
		#pragma warning disable CA1031 // Ignore all the errors
		catch (Exception flaw)
		{
			Config.LogConfigurationError(LogSource, flaw.Add(nameof(Location), Location));
		}
	}

	private IEnumerable<XmlLiteNode>? OptionHandler(ref TextToXmlConverter converter, string option, IReadOnlyCollection<string> parameters)
	{
		if (option != "include")
		{
			Config.LogConfigurationEvent(LogSource, SR.UnknownOption(option, Name));
			return null;
		}
		return ConfigurationSource.HandleInclude(LogSource, parameters, GetDirectory(Location), ref _includes, OnChanged);
	}

	private static string GetDirectory(Uri location)
	{
		var dir = location.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped);
		if (!dir.EndsWith("/", StringComparison.Ordinal))
		{
			var i = dir.LastIndexOf('/');
			if (i >= 0)
				dir = dir.Substring(0, i);
		}
		return dir;
	}

	public static HttpConfigurationSource? TryCreate(Uri? location, IReadOnlyCollection<string> parameters)
	{
		if (location == null || !location.IsAbsoluteUri)
			return null;
		if (!(location.Scheme == Uri.UriSchemeHttp || location.Scheme == Uri.UriSchemeHttps))
			return null;
		return new HttpConfigurationSource(location, parameters);
	}
}
