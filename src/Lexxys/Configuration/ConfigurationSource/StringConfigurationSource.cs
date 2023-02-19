// Lexxys Infrastructural library.
// file: StringConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Lexxys.Configuration
{
	using Xml;

	internal class StringConfigurationSource : IXmlConfigurationSource
	{
		private const string LogSource = "Lexxys.Configuration.StringConfigurationSource";
		private List<string>? _includes;
		private IReadOnlyList<XmlLiteNode>? _content;
		private readonly Func<string, string?, IReadOnlyList<XmlLiteNode>> _converter;
		private readonly string _type;
		private readonly string _text;
		private int _version;

		private StringConfigurationSource(Uri location, IReadOnlyCollection<string> parameters)
		{
			_type = location.LocalPath.Trim('/', '[', ']');
			Name = location.Host;
			int i = location.OriginalString.IndexOf('?');
			_text = i > 0 ? location.OriginalString.Substring(i + 1): ""; // Uri.UnescapeDataString(location.Query.Substring(1) + location.Fragment);
			if (_text.Length < 120)
				Location = location;
			else
				Location = new Uri(location.OriginalString.Substring(0, i + 1) + GetHash(_text));
			_converter = XmlConfigurationProvider.GetSourceConverter(_type, OptionHandler, parameters);
			_version = 1;

			static string GetHash(string text)
			{
				using var hasher = System.Security.Cryptography.SHA256.Create();
				byte[] bytes = Encoding.Unicode.GetBytes(text);
				var hash = hasher.ComputeHash(bytes, 0, bytes.Length);
				return Strings.ToHexString(hash);
			}
		}

		#region IConfigurationSource

		public string Name { get; }

		public Uri Location { get; }

		public int Version => _version;

		public IReadOnlyList<XmlLiteNode> Content
		{
			get
			{
				IReadOnlyList<XmlLiteNode>? content = _content;
				if (content == null)
				{
					if (Interlocked.CompareExchange(ref _content, _converter(_text, null), null) == null)
						++_version;
					content = _content;
				}
				return content;
			}
		}

		public event EventHandler<ConfigurationEventArgs>? Changed;

		#endregion

		public override bool Equals(object? obj)
		{
			return obj is StringConfigurationSource x && x._type == _type && x._text == _text;
		}

		public override int GetHashCode()
		{
			return HashCode.Join(_type.GetHashCode(), _text.GetHashCode());
		}

		private void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			try
			{
				lock (Location)
				{
					_content = null;
					Changed?.Invoke(sender ?? this, e);
				}
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception flaw)
			{
				Config.LogConfigurationError(LogSource, flaw);
			}
#pragma warning restore CA1031 // Do not catch general exception types
		}

		private IEnumerable<XmlLiteNode>? OptionHandler(TextToXmlConverter converter, string option, IReadOnlyCollection<string> parameters)
		{
			if (option != "include")
			{
				Config.LogConfigurationEvent(LogSource, SR.UnknownOption(option, null));
				return null;
			}
			return ConfigurationSource.HandleInclude(LogSource, parameters, null, ref _includes, OnChanged);
		}

		// string://name/txt?configuration_text
		public static StringConfigurationSource? TryCreate(Uri? location, IReadOnlyCollection<string> parameters)
		{
			if (location == null || !location.IsAbsoluteUri || location.Scheme != "string")
				return null;
			return location.Query.Length <= 1 ? null: new StringConfigurationSource(location, parameters);
		}
	}
}
