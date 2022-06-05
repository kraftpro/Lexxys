// Lexxys Infrastructural library.
// file: Curl.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.RL
{
	public interface ISchemeParser
	{
		IEnumerable<string> SupportedSchemas { get; }
		CurlBuilder Parse(string value, Vote<string> vote);
		Vote<string> Analyze(string value);
	}

	/// <summary>
	/// Concrete implementation of Uniform Resource Locator for local use.
	/// </summary>
	/// <remarks>
	/// <code>
	/// URL ::= scheme ':' user ':' password '@' host ':' port '/' path '?' query '#' fragment
	/// 
	/// URI				:=	scheme [fullpath] ['?' query] ['#' fragment]
	/// scheme			:=	name ':'
	/// fullpath		:=	authority ['/' path] |
	/// authority		:=	inet_authority | mail_authority
	/// inet_authority	:=	'//' [user [':' password] '@'] hostname [ ':' port ]
	/// mail_authority	:=	user [':' password] '@' hostname
	/// file_authority	:=	'///' drive ':'
	/// </code>
	/// </remarks>
	public class Curl
	{
		public static readonly Curl Empty = new Curl();

		public const int DefaultPort = 0;
		public const int MaxPort = 65535;

		public string Scheme { get; }
		public string Host { get; }
		public int Port { get; }
		public string Path { get; }
		/// <summary>
		/// scheme:host:port/path?query#fragment
		/// </summary>
		public string FullPath { get; }
		public string Query { get; }
		public string Fragment { get; }
		public string UserId { get; }
		public string Password { get; }
		public bool IsAbsolute { get; }
		public IDictionary<string, string> QueryParameters { get; }

		private static readonly List<ISchemeParser> SchemeParsers;
		private static List<string> _supportedSchemas;

		static Curl()
		{
			SchemeParsers = Factory.Classes(typeof(ISchemeParser))
				.Select(o => Factory.TryConstruct(o, false) as ISchemeParser)
				.Where(o => o != null).ToList();
			Factory.AssemblyLoad += Factory_AssemblyLoad;
		}

		public static void Register(ISchemeParser parser)
		{
			if (parser != null)
				SchemeParsers.Add(parser);
		}

		private Curl()
		{
		}

		public Curl(string value): this(Parse(value))
		{
		}

		public Curl(CurlBuilder builder)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			Scheme = builder.Scheme.TrimToNull()?.ToLowerInvariant();
			Host = builder.Host.TrimToNull();
			Port = builder.Port;
			Path = builder.Path.TrimToNull();
			Fragment = builder.Fragment.TrimToNull();

			var query = builder.Query.TrimToNull();
			Dictionary<string, string> parms = builder.QueryParameters == null ?
				new Dictionary<string, string>():
				new Dictionary<string, string>(builder.QueryParameters);
			if (query != null)
			{
				foreach (string pair in query.Split('&'))
				{
					int i = pair.IndexOf('=');
					string key = i < 0 ? pair.Trim(): pair.Substring(0, i).Trim();
					string value = i < 0 ? null: pair.Substring(i + 1).Trim();
					parms[key] = value;
				}
			}

			var text = new StringBuilder();
			foreach (var item in parms)
			{
				if (text.Length > 0)
					text.Append('&');
				text.Append(EncodeUrl(item.Key));
				if (item.Value != null)
					text.Append('=').Append(EncodeUrl(item.Value));
			}
			Query = text.ToString();
			QueryParameters = ReadOnly.Wrap(parms);

			if (builder.FullPath != null)
				FullPath = builder.FullPath.TrimToNull();

			if (FullPath == null)
			{
				if (Scheme != null && Host != null)
				{
					/*
					 * 
					 * 
					 */

					text.Clear();
					text.Append(Scheme).Append(':').Append(Host);
					if (Port > 0)
						text.Append(':').Append(Port);
					if (Path != null)
					{
						if (!Path.StartsWith("/"))
							text.Append('/');
						text.Append(Path);
					}
					if (Query != null)
						text.Append('?').Append(Query);
					if (Fragment != null)
						text.Append('#').Append(Fragment);
				}
			}
			UserId = builder.UserId.TrimToNull();
			Password = builder.Password.TrimToNull();
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			if (Scheme != null)
				text.Append(Scheme).Append(':');
			if (UserId != null)
			{
				text.Append(UserId);
				if (Password != null)
					text.Append(':').Append(Password);
				text.Append('@');
			}
			if (Host != null)
			{
				text.Append(Host);
				if (Port > 0)
					text.Append(':').Append(Port);
			}
			if (Path != null)
			{
				if (!(Host == null || Path.StartsWith("/")))
					text.Append('/');
				text.Append(Path);
			}
			if (Query != null)
				text.Append('?').Append(Query);
			if (Fragment != null)
				text.Append('#').Append(Fragment);
			return text.ToString();
		}

		public static Curl Create(string value)
		{
			if (value == null)
				return null;

			CurlBuilder builder = Parse(value);
			return builder == null ? null: new Curl(builder);
		}

		private static CurlBuilder Parse(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			string scheme = ExtractScheme(value, out string valuePart);
			if (scheme != null)
			{
				var yes = Vote.Yes(scheme);
				foreach (var parser in GetParser(scheme))
				{
					CurlBuilder builder = parser?.Parse(valuePart, yes);
					if (builder != null)
						return builder;
				}
				return null;
			}

			foreach (var item in VoteForSchemas(value, VoteScore.Maybe))
			{
				foreach (var parser in GetParser(item.Value))
				{
					CurlBuilder builder = parser?.Parse(value, item);
					if (builder != null)
						return builder;
				}
			}
			return null;
		}

		private static IEnumerable<ISchemeParser> GetParser(string scheme)
		{
			return SchemeParsers.Where(o => o.SupportedSchemas.Contains(scheme));
		}

		private static IEnumerable<Vote<string>> VoteForSchemas(string value, VoteScore minimalVote)
		{
			return SchemeParsers.Select(o => o.Analyze(value))
				.Where(o => o.Score >= minimalVote)
				.OrderByDescending(o => o.Score);
		}

		private static string ExtractScheme(string value, out string locatorValue)
		{
			value = value.TrimStart();
			foreach (string scheme in SupportedSchemas())
			{
				if (value.StartsWith(scheme + ":", StringComparison.OrdinalIgnoreCase))
				{
					locatorValue = value.Substring(scheme.Length + 1).TrimStart();
					return scheme;
				}
			}
			locatorValue = value;
			return null;
		}

		private static IList<string> SupportedSchemas()
		{
			return _supportedSchemas ??= SchemeParsers.SelectMany(o => o.SupportedSchemas).ToList();
		}

		private static void Factory_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			SchemeParsers.AddRange(Factory.Classes(typeof(ISchemeParser), args.LoadedAssembly)
				.Select(o => Factory.TryConstruct(o, false) as ISchemeParser)
				.Where(o => o != null));
			_supportedSchemas = null;
		}

		public static unsafe string EncodeUrl(string value)
		{
			if (value == null)
				return null;

			fixed (char* str = value)
			{
				char* s = str;
				char* e = s + value.Length;
				bool space = false;
				int count5 = 0;
				int count2 = 0;
				while (s != e)
				{
					char c = *s;
					if (c > 0x007F)
					{
						if (c > 0x07FF)
							count5 += 2;
						else
							count5 += 1;
					}
					else if (!IsSafeUrlChar(c))
					{
						if (c == ' ')
							space = true;
						else
							++count2;
					}
					++s;
				}
				if (count2 == 0 && count5 == 0 && !space)
					return value;

				char[] buffer = new char[value.Length + count2 * 2 + count5 * 5];
				byte* temp = stackalloc byte[3];
				int i = 0;

				for (s = str; s != e; ++s)
				{
					char c = *s;
					if (IsSafeUrlChar(c))
					{
						buffer[i++] = c;
					}
					else if (c > 127)
					{
						int count = Encoding.UTF8.GetBytes(s, 1, temp, 3);
						for (int j = 0; j < count; ++j)
						{
							buffer[i++] = '%';
							buffer[i++] = __hexDigits[temp[j] >> 4];
							buffer[i++] = __hexDigits[temp[j] & 15];
						}
					}
					else if (c != ' ')
					{
						buffer[i++] = '%';
						buffer[i++] = __hexDigits[c >> 4];
						buffer[i++] = __hexDigits[c & 15];
					}
					else
					{
						buffer[i++] = '+';
					}
				}
				return new String(buffer);
			}
		}
		private static readonly char[] __hexDigits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

		public static unsafe string DecodeUrl(string value)
		{
			if (value == null)
				return null;

			int i = value.IndexOf('%');
			value = value.Replace('+', ' ');
			if (i < 0)
				return value;
			var text = new StringBuilder(value.Length);
			text.Append(value, 0, i);
			char[] buffer = new char[value.Length];
			byte* temp = stackalloc byte[value.Length / 3];
			fixed (char* str = value)
			{
				fixed (char* buf = buffer)
				{
					value.CopyTo(0, buffer, 0, i);
					char* s = str + i;
					char* e = str + value.Length;
					char* b = buf + i;
					while (s != e)
					{
						char c = *s++;
						if (c != '%')
						{
							*b++ = c;
						}
						else
						{
							int x = Unpack(s, e);
							if (x < 0)
							{
								*b++ = '%';
							}
							else if (x < 128)
							{
								s += 2;
								*b++ = (char)x;
							}
							else
							{
								s += 2;
								temp[0] = (byte)x;
								int j = 1;
								while (s != e && *s == '%' && (x = Unpack(s + 1, e)) >= 0)
								{
									temp[j++] = (byte)x;
									s += 3;
								}
								b += Encoding.UTF8.GetChars(temp, j, b, j);
							}
						}
					}
					return new String(buf, 0, (int)(b - buf));
				}
			}
		}

		private static unsafe int Unpack(char* s, char* e)
		{
			if (s == e)
				return -1;
			char a = *s++;
			if (s == e)
				return -1;
			char b = *s;
			int x;
			if (a >= '0' && a <= '9')
				x = (a - '0') << 4;
			else if (a >= 'A' && a <= 'F')
				x = (a - ('A' - 10)) << 4;
			else if (a >= 'a' && a <= 'f')
				x = (a - ('a' - 10)) << 4;
			else
				return -1;
			if (b >= '0' && b <= '9')
				x += b - '0';
			else if (b >= 'A' && b <= 'F')
				x += b - ('A' - 10);
			else if (b >= 'a' && b <= 'f')
				x += b - ('a' - 10);
			else
				return -1;
			return x;
		}

		private static bool IsSafeUrlChar(char value)
		{
			if ((value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9'))
				return true;
			return value switch
			{
				'(' or ')' or '*' or '-' or '.' or '_' or '!' => true,
				_ => false,
			};
		}
	}
}
