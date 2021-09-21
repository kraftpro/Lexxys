// Lexxys Infrastructural library.
// file: ConfigurationLocator.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Configuration
{
	public enum ConfigLocatorSchema
	{
		Undefined,
		File,
		Http,
		Https,
		Ftp,
		Ftps,
		Database,
		DatabaseEnums,
		String
	}

	//public enum ConfigSourceType
	//{
	//    Undefined,
	//    Xml,
	//    Txt,
	//    Ini,
	//}

	public class ConfigurationLocator: IEquatable<ConfigurationLocator>
	{
		private readonly bool _sourceTypeFixed;

		/// <summary>
		///	Create Configuration Locator
		/// </summary>
		/// <param name="value"></param>
		/// <param name="directory">True - the location is directory, so don't need to parse extension</param>
		/// <remarks>
		///		grammar:
		///			location		:=	[ schema schemaSeparator ] [ type ] [host] ['/' path ] ['?' query] [newLine text]
		///			type			:=	'[' (ini | xml | txt | ...) ']'
		///			schemaSeparator	:=	':' '?'{0..2}
		///		
		/// </remarks>
		public ConfigurationLocator(string value, bool directory = false)
		{
			if (value == null)
				throw EX.ArgumentNull(nameof(value));

			value = Config.ExpandParameters(value);
			Match m = __crlRex.Match(value);
			Schema = m.Groups["schema"].Value.Trim().ToLowerInvariant();
			Host = m.Groups["host"].Value.Trim();
			Path = m.Groups["path"].Value.Trim();
			QueryString = m.Groups["query"].Value.Trim();
			SourceType = m.Groups["type"].Value.Trim().ToUpperInvariant();
			Text = m.Groups["text"].Value.Trim();
			if (!directory)
				if (SourceType.Length == 0)
					SourceType = GetExtension(Path).ToUpperInvariant();
				else
					_sourceTypeFixed = true;

			SchemaType = ConfigLocatorSchema.Undefined;
			switch (Schema)
			{
				case "file":
					SchemaType = ConfigLocatorSchema.File;
					Path = Path?.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar).TrimEnd(System.IO.Path.DirectorySeparatorChar);
					break;

				case "http":
					SchemaType = ConfigLocatorSchema.Http;
					break;

				case "https":
					SchemaType = ConfigLocatorSchema.Https;
					break;

				case "ftp":
					SchemaType = ConfigLocatorSchema.Ftp;
					break;

				case "ftps":
					SchemaType = ConfigLocatorSchema.Ftps;
					break;

				case "database":
					SchemaType = ConfigLocatorSchema.Database;
					break;

				case "dbenums":
				case "db-enums":
				case "databaseenums":
				case "database-enums":
					SchemaType = ConfigLocatorSchema.DatabaseEnums;
					break;

				case "string":
					SchemaType = ConfigLocatorSchema.String;
					break;

				case "":
					if (String.Equals(Host, "DBENUMS", StringComparison.OrdinalIgnoreCase) ||
						String.Equals(Host, "DB-ENUMS", StringComparison.OrdinalIgnoreCase) ||
						String.Equals(Host, "DATABASEENUMS", StringComparison.OrdinalIgnoreCase) ||
						String.Equals(Host, "DATABASE-ENUMS", StringComparison.OrdinalIgnoreCase))
					{
						SchemaType = ConfigLocatorSchema.DatabaseEnums;
						Host = String.Empty;
					}
					break;
			}
			QueryParameters = ParseQueryString(QueryString);
		}

		private ConfigurationLocator(string sourceType, ConfigLocatorSchema schema, string host, string path, string queryString)
		{
			SourceType = sourceType?.ToUpperInvariant() ?? "";
			SchemaType = schema;
			Host = host;
			Path = path;
			QueryString = queryString;
			IsLocated = true;
			_sourceTypeFixed = true;
			QueryParameters = ParseQueryString(QueryString);
		}

		private static IReadOnlyList<KeyValuePair<string, string>> ParseQueryString(string value)
		{
			if (value == null || value.Length == 0)
				return ReadOnly.Empty<KeyValuePair<string, string>>();

			var parameters = new List<KeyValuePair<string, string>>();
			foreach (string item in value.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string name = item.Trim();
				if (name.Length == 0 || name == "=")
					continue;
				int i = name.IndexOf('=');
				string val = "";
				if (i >= 0)
				{
					val = name.Substring(i + 1).Trim();
					name = name.Substring(0, i).Trim();
				}
				parameters.Add(new KeyValuePair<string, string>(name, val));
			}
			return ReadOnly.Wrap(parameters);
		}

		public string SourceType { get; }
		public ConfigLocatorSchema SchemaType { get; }
		public string Schema { get; }
		public string Host { get; }
		public string Path { get; }
		public IReadOnlyList<KeyValuePair<string, string>> QueryParameters { get; }
		public string QueryString { get; }
		public string Text { get; }
		public bool IsLocated { get; }

		public string DirectoryName => !IsLocated ? null: System.IO.Path.GetDirectoryName(Path);
		public bool IsValid => SchemaType != ConfigLocatorSchema.Undefined || Host.Length > 0 || Path.Length > 0;

		//public string Value
		//{
		//    get { return _schema.ToString().ToLowerInvariant() + "://" + _host + _path + (_queryString.Length > 0 ? "?" + _queryString: ""); }
		//}
		//public bool IsLocal
		//{
		//    get { return _schema == ConfigLocatorSchema.File; }
		//}
		//public bool IsRemote
		//{
		//    get
		//    {
		//        return _schema == ConfigLocatorSchema.Http || _schema == ConfigLocatorSchema.Https ||
		//                _schema == ConfigLocatorSchema.Ftp || _schema == ConfigLocatorSchema.Ftps;
		//    }
		//}
		//public bool IsDatabase
		//{
		//    get { return _schema == ConfigLocatorSchema.Database || _schema == ConfigLocatorSchema.DatabaseEnums; }
		//}
		//public bool IsRelative
		//{
		//    get
		//    {
		//        return _schema == ConfigLocatorSchema.File &&
		//                _path.Length > 0 && _path[0] != '/' && _path[0] != '\\' &&
		//                (_path.Length < 2 || _path[1] != ':' || !Char.IsLetter(_path[0]));
		//    }
		//}

		public ConfigurationLocator Locate(IEnumerable<string> directory, ICollection<string> extention)
		{
			if (IsLocated)
				return this;
			if (!IsValid || (SchemaType != ConfigLocatorSchema.Undefined && SchemaType != ConfigLocatorSchema.File))
				return this;

			FileInfo fi = FindLocalFile(Host + Path, directory, extention);
			return fi == null ? this: new ConfigurationLocator(_sourceTypeFixed ? SourceType: GetExtension(fi.Name), ConfigLocatorSchema.File, "", fi.FullName, QueryString);
		}

		//public static ConfigurationLocator Combine(ConfigurationLocator left, ConfigurationLocator right)
		//{
		//	if (right == null && left == null)
		//		throw EX.ArgumentNull("left and right");
		//	if (right == null)
		//		return left;
		//	if (left == null)
		//		return right;
		//	return null;
		//	//if (right.IsAbsolute)
		//	//	return right;
		//	//return new ConfigurationLocator(System.IO.Path.Combine(left.ToString(), right));
		//}

		public override bool Equals(object obj)
		{
			return Equals(obj as ConfigurationLocator);
		}

		public bool Equals(ConfigurationLocator that)
		{
			if (that == null)
				return false;
			return SchemaType == that.SchemaType &&
				String.Equals(SourceType, that.SourceType, StringComparison.OrdinalIgnoreCase) &&
				String.Equals(Path, that.Path, StringComparison.OrdinalIgnoreCase) &&
				String.Equals(Host, that.Host, StringComparison.OrdinalIgnoreCase) &&
				String.Equals(QueryString, that.QueryString, StringComparison.OrdinalIgnoreCase) &&
				String.Equals(Text, that.Text, StringComparison.Ordinal);
		}

		public override int GetHashCode()
		{
			return HashCode.Join(SchemaType.GetHashCode(),
				QueryString.ToUpperInvariant().GetHashCode(),
				Path.ToUpperInvariant().GetHashCode(),
				Host.ToUpperInvariant().GetHashCode());
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			if (SourceType.Length > 0)
				text.Append('[').Append(SourceType).Append("] ");
			if (SchemaType != ConfigLocatorSchema.Undefined && SchemaType != ConfigLocatorSchema.File)
				text.Append(SchemaType == ConfigLocatorSchema.DatabaseEnums ? "database-enums": SchemaType.ToString().ToLowerInvariant()).Append("://");

			if (!String.IsNullOrEmpty(Host))
				text.Append(Host);
			if (!String.IsNullOrEmpty(Path))
				text.Append(Path);
			if (!String.IsNullOrEmpty(QueryString))
				text.Append('?').Append(QueryString);
			return text.ToString();
		}

		#region Implementation
		private static FileInfo FindLocalFile(string filePath, IEnumerable<string> directory, ICollection<string> extention)
		{
			if (filePath == null)
				return null;

			filePath = Config.ExpandParameters(filePath);
			if (File.Exists(filePath))
				return new FileInfo(filePath);

			if (extention != null)
			{
				foreach (string ext in extention)
				{
					int i = ext.LastIndexOf('.');
					string e = (i > 0) ? ext.Substring(i): ext;
					if (filePath.EndsWith(e, StringComparison.OrdinalIgnoreCase))
					{
						extention = null;
						break;
					}
				}
			}

			if (filePath[0] == System.IO.Path.DirectorySeparatorChar || filePath[0] == System.IO.Path.AltDirectorySeparatorChar)
				directory = null;
			else if (ContainsVolume(filePath))
				directory = null;

			if (directory == null)
				return extention?
					.Where(ext => File.Exists(filePath + ext))
					.Select(ext => new FileInfo(filePath + ext))
					.FirstOrDefault();

			if (extention == null)
				return directory
					.Select(dir => System.IO.Path.Combine(dir, filePath))
					.Where(File.Exists)
					.Select(file => new FileInfo(file))
					.FirstOrDefault();

			return directory
				.SelectMany(_ => extention, (dir, ext) => System.IO.Path.Combine(dir, filePath + ext))
				.Where(File.Exists)
				.Select(file => new FileInfo(file))
				.FirstOrDefault();

			static bool ContainsVolume(string path)
			{
				if (System.IO.Path.DirectorySeparatorChar == System.IO.Path.VolumeSeparatorChar)
					return false;
				var i = path.IndexOf(System.IO.Path.VolumeSeparatorChar);
				return i > 0 && path.Substring(0, i).All(c => Char.IsLetter(c));
			}
		}

		private static string GetExtension(string fileName)
		{
			if (fileName == null)
				return "";
			var ext = System.IO.Path.GetExtension(fileName);
			return ext.Length > 0 ? ext.Substring(1) : ext;
		}

		private static readonly Regex __crlRex = new Regex(@"\A\s*" +
			@"((?<schema>file|https?|ftps?|database|enums|database-?enums|string):/?/?/?)?" +
			@"(\[(?<type>[a-zA-Z]*)\])?" +
			@"(?<res>" +
				@"(?<host>[^/\r\n\?\\]*)" +
				@"(?<path>[^\r\n]*?)" +
				@")" +
			@"(\?(?<query>[^\r\n]*?))?" +
			@"([\r\n](?<text>.*))?\z",
			RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
		#endregion
	}
}
