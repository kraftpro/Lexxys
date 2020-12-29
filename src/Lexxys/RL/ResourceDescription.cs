// Lexxys Infrastructural library.
// file: ResourceDescription.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Lexxys.RL
{
	public interface IResourceTransportFactory
	{
		IEnumerable<string> SupportedSchemas { get; }
		IResourceTransportService Open(Curl location);
	}

	public interface IResourceTransportService: IDisposable
	{
		ResourceInfo Description { get; }

		/// <summary>
		/// Read first <paramref name="count"/> bytes from the resorce.
		/// </summary>
		/// <param name="count">Number of bytes to read.</param>
		/// <returns>First <paramref name="count"/> or less bytes from the resource or null if the resource is not accessible.</returns>
		byte[] LookAhead(int count);
		/// <summary>
		/// Open resource for reading.
		/// </summary>
		/// <returns>Resource stream</returns>
		Stream OpenStream();
	}

	public class ResourceInfo
	{
		public Curl Locator { get; set; }
		public string Name { get; set; }
		public DateTime? Created { get; set; }
		public DateTime? Modified { get; set; }
		public long? Length { get; set; }
		public IDictionary<string, object> Options { get; set; }

		public IList<byte> ResourceHashCode { get; set; }
		public string Version { get; set; }

		public ResourceContentType ContentType { get; set; }
		public IResourceService Service { get; set; }
	}

	public interface IResourceService
	{
		ResourceInfo Description { get; }
		IEnumerable<ResourceInfo> Enumerate();
		Stream Open(FileAccess accesMode);
	}

	public class ResourceContentType
	{
		public string DocumentType { get; private set; }
		public Encoding Encodong { get; private set; }

		//public ContentType GetMimeContentType()
		//{
		//	return null;
		//}
	}
}


