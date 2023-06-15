// Lexxys Infrastructural library.
// file: IBlobInfo.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

public interface IBlobInfo
{
	bool Exists { get; }
	long Length { get; }
	string Path { get; }
	DateTimeOffset LastModified { get; }
	Stream CreateReadStream();
	Task<Stream> CreateReadStreamAsync();
}


