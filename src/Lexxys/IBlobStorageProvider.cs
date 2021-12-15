// Lexxys Infrastructural library.
// file: IBlobStorageProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lexxys
{
	public interface IBlobStorageProvider: IDisposable
	{
		/// <summary>
		/// Collection of supported schemes
		/// </summary>
		IReadOnlyCollection<string> SupportedSchemes { get; }
		/// <summary>
		/// Determines if this provider can open a blob at the specified <paramref name="uri"/>. 
		/// </summary>
		/// <param name="uri">Blob location</param>
		/// <returns></returns>
		bool CanOpen(string uri);
		IBlobInfo GetFileInfo(string uri);
		void SaveFile(string uri, Stream stream, bool overwrite);
		Task SaveFileAsync(string uri, Stream stream, bool overwrite);
		void MoveFile(string source, string destination);
		void DeleteFile(string uri);
	}
}

