// Lexxys Infrastructural library.
// file: FileScheme.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

namespace Lexxys.RL
{
	class FileScheme: ISchemeParser
	{
		public const string SchemeName = "file";
		public static readonly Vote<string> Scheme = Vote.Yes(SchemeName);

		public IEnumerable<string> SupportedSchemas
		{
			get { return __schemas; }
		}
		private static readonly IList<string> __schemas = ReadOnly.Wrap(new[] { SchemeName });

		public CurlBuilder Parse(string value, Vote<string> scheme)
		{
			if (scheme.Value != SchemeName)
				throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null);
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			value = value.Trim();
			if (value.Length == 0)
				throw new ArgumentNullException(nameof(value));

			var r = new CurlBuilder { Scheme = scheme.Value };

			r.SetPath(value, true, true);
			r.IsAbsolute = true;
			value = value.Replace('\\', '/');
			if (value.StartsWith("//"))
			{
				value = value.Substring(2).TrimStart();

				if (value.StartsWith("/"))
				{
					if (scheme.Score != VoteScore.Yes)
						return null;

					if (value.StartsWith("//"))
						value = value.Substring(2).TrimStart();
					else if (HasDriveLetter(value, 1))
						value = value.Substring(1);
				}

				if (!HasDriveLetter(value, 0))
				{
					int i = value.IndexOf('/');
					if (i < 0)
					{
						r.Host = value;
						value = "/";
					}
					else
					{
						r.Host = value.Substring(0, i).TrimEnd();
						value = value.Substring(i);
					}
				}
			}

			if (HasDriveLetter(value, 0))
			{
				if (value.Length > 2)
				{
					if (value[2] != '/')
						value = value.Substring(0, 2) + "/" + value.Substring(2);
				}
				else
				{
					value += '/';
				}
			}
			else if (String.IsNullOrEmpty(r.Host))
			{
				r.IsAbsolute = false;
			}
			r.FullPath = String.IsNullOrEmpty(r.Host) ? SchemeName + ":///" + value.TrimStart('/'): SchemeName + "://" + r.Host + value;

			return r;
		}

		public Vote<string> Analyze(string value)
		{
			if (value == null)
				return Vote<string>.Empty;
			value = value.Trim();
			if (value.Length < 2)
				return Vote<string>.Empty;
			if (value.StartsWith("\\\\"))
				return Vote.Yes(FileScheme.SchemeName);
			if (value.StartsWith("//"))
				return Vote.ProbablyYes(FileScheme.SchemeName);
			if (value[1] == ':')
			{
				char drive = Char.ToUpperInvariant(value[0]);
				if (drive >= 'A' && drive <= 'Z')
					return Vote.ProbablyYes(FileScheme.SchemeName);
			}
			return value.IndexOfAny(new[] { '?', '+', '[', ']', '*', ':' }) >= 0 ?
				Vote.ProbablyNot(FileScheme.SchemeName):
				Vote.Maybe(FileScheme.SchemeName);
		}

		private static bool HasDriveLetter(string value, int position)
		{
			if (value.Length < position + 2 || value[position + 1] != ':')
				return false;
			char drive = value[position];
			return drive >= 'a' && drive <= 'z' || drive >= 'A' && drive <= 'Z';
		}
	}
}
