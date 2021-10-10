﻿// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

#nullable enable

namespace Lexxys
{
	public interface IConfigValue
	{
		event EventHandler<ConfigurationEventArgs> Changed;
		int Version {  get; }
		object? GetValue(string key, Type type);
		IReadOnlyList<T> GetList<T>(string key);
	}
}