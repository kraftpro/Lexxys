// Lexxys Infrastructural library.
// file: IConfigurationProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

namespace Lexxys.Configuration
{
	public interface IConfigProvider: IConfigSource
	{
		string Name { get; }
		Uri Location { get; }
	}
}