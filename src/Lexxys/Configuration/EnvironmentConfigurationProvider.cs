// Lexxys Infrastructural library.
// file: EnvironmentConfigurationProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys.Xml;

#nullable enable

namespace Lexxys.Configuration
{
	class EnvironmentConfigurationProvider: IConfigurationProvider
	{
		public string Name => "System.Environment";
		public object? GetValue(string reference, Type returnType)
		{
			if (reference == null)
				return null;
			if (reference.StartsWith("env::", StringComparison.OrdinalIgnoreCase))
				reference = reference.Substring(5);
			return XmlTools.TryGetValue(Environment.GetEnvironmentVariable(reference), returnType, out object result) ? result : null;
		}

		public List<T> GetList<T>(string reference) => new List<T>();
		
		#pragma warning disable CS0067 
		public event EventHandler<ConfigurationEventArgs>? Changed;
	}
}


