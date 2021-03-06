// Lexxys Infrastructural library.
// file: IConfigurationProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.Configuration
{
	public interface IConfigurationProvider: ITrackedConfiguration
	{
		string Name { get; }
		bool Initialized { get; }
		bool IsEmpty { get; }
		object GetValue(string reference, Type returnType);
		List<T> GetList<T>(string reference);
	}
}


