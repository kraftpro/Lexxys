using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys.Configuration
{
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
					lock (this)
					{
						if (_content == null)
						{
							_content = _converter(GetText(Location), Name);
						}
					}
				}
				return _content;
			}
		}

		private static string GetText(Uri location)
		{
#if NETCOREAPP
			return new System.Net.Http.HttpClient().GetStringAsync(location).GetAwaiter().GetResult();
#else
			using (var c = new System.Net.WebClient())
			{
				return c.DownloadString(location);
			}
#endif
		}

#if NETCOREAPP
		private static async Task<(string Type, string? Text)> GetContentTypeAsync(Uri location)
		{
			var type = TypeByUrl(location.ToString());
			if (type != null)
				return (type, null);

			var client = new System.Net.Http.HttpClient();
			var message = await client.GetAsync(location, System.Net.Http.HttpCompletionOption.ResponseContentRead);
			if (!message.IsSuccessStatusCode)
				throw new InvalidOperationException($"GET {location} returns status code {((int)message.StatusCode)}.");
			if (!message.Headers.TryGetValues("Content-Type", out var contentType)) // || (contentType = contentTypes.FirstOrDefault(o => o.IndexOf('/') > 0)) == null)
				throw new InvalidOperationException($"GET {location} doesn't have content type specified.");

			type = TypeByContentType(contentType, location);
			var content = await message.Content.ReadAsStringAsync();
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

			type = TypeByContentType(rp.ContentType?.Split(';'), location);

			string content;
			using (var reader = new StreamReader(rp.GetResponseStream()))
			{
				content = reader.ReadToEnd();
			}
			return (type, content);
		}
#endif

		private static string? TypeByUrl(string value)
		{
			return
				value.IndexOf("type=txt") > 0 ? "txt":
				value.IndexOf("type=xml") > 0 ? "xml":
				value.IndexOf("type=ini") > 0 ? "ini":
				value.IndexOf("type=json") > 0 ? "json":
				value.IndexOf(".txt") > 0 ? "txt":
				value.IndexOf(".xml") > 0 ? "xml":
				value.IndexOf(".ini") > 0 ? "ini":
				value.IndexOf(".json") > 0 ? "json": null;
		}

		private static string TypeByContentType(IEnumerable<string>? contentType, Uri location)
		{
			if (contentType == null)
				throw new InvalidOperationException($"GET {location} doesn't have content type specified.");

			var type  = contentType.FirstOrDefault(o => o.IndexOf('/') > 0)?.Trim() ?? "";
			if (type.EndsWith("/json"))
				return "json";
			if (type.EndsWith("/xml"))
				return "xml";
			if (type.EndsWith("/ini"))
				return "xml";
			if (type == "text/plain")
				return "txt";
			throw new InvalidOperationException($"GET {location} returns unsupported content type \"{String.Join(";", contentType)}\".");
		}

		public event EventHandler<ConfigurationEventArgs>? Changed;

		private void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			try
			{
				lock (this)
				{
					_content = null;
					Changed?.Invoke(sender ?? this, e);
					++_version;
				}
			}
			catch (Exception flaw)
			{
				Config.LogConfigurationError(LogSource, flaw.Add(nameof(Location), Location));
			}
		}

		private IEnumerable<XmlLiteNode>? OptionHandler(string option, IReadOnlyCollection<string> parameters)
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
			if (!dir.EndsWith("/"))
			{
				var i = dir.LastIndexOf('/');
				if (i >= 0)
					dir = dir.Substring(0, i);
			}
			return dir;
		}

		public static HttpConfigurationSource? Create(Uri location, IReadOnlyCollection<string> parameters)
		{
			if (location == null || !location.IsAbsoluteUri)
				return null;
			if (!(location.Scheme == Uri.UriSchemeHttp || location.Scheme == Uri.UriSchemeHttps))
				return null;
			return new HttpConfigurationSource(location, parameters);
		}
	}
}
