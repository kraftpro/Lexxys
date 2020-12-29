// Lexxys Infrastructural library.
// file: ResourceTransportFactory.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;

namespace Lexxys.RL
{
	class ResourceTransportFactory
	{
		public static IResourceTransportFactory Create(string shema)
		{
			return null;
		}


		public static void TestMe()
		{
			var location = Curl.Create("www.google.com");
			IResourceTransportFactory factory = ResourceTransportFactory.GetFactory(location.Scheme);
			using IResourceTransportService transport = factory.Open(location);
			ResourceInfo description = transport.Description;
		}

		public static IResourceTransportFactory GetFactory(string scheme)
		{
			throw new NotImplementedException();
		}
	}

	class LocalFileTransport: IResourceTransportService
	{
		private static readonly IList<string> _schemas = ReadOnly.Wrap(new[] { FileScheme.SchemeName });

		private readonly FileInfo _fileInfo;

		public LocalFileTransport(Curl location)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));
			if (location.Scheme != FileScheme.SchemeName)
				throw new ArgumentOutOfRangeException(nameof(location.Scheme), location.Scheme, null);

			string path = Environment.ExpandEnvironmentVariables(location.FullPath);
			_fileInfo = new FileInfo(path);

			var _info = new ResourceInfo()
			{
				Locator = location,
				Name = _fileInfo.Name,
				Created = _fileInfo.CreationTime,
				Modified = _fileInfo.LastWriteTime,
				Length = _fileInfo.Length,

			};

		}

		public IEnumerable<string> SupportedSchemas
		{
			get { return _schemas; }
		}

		public ResourceInfo Description
		{
			get { throw new NotImplementedException(); }
		}

		public byte[] LookAhead(int count)
		{
			throw new NotImplementedException();
		}

		public System.IO.Stream OpenStream()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}


