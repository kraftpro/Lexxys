// Lexxys Infrastructural library.
// file: NamingCaseRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public enum NamingCaseRule
	{
		None = 0,
		PreferLowerCase = 1,
		PreferCamelCase = 2,
		PreferPascalCase = 3,
		PreferUpperCase = 4,
		ForceLowerCase = 8 + PreferLowerCase,
		ForceCamelCase = 8 + PreferCamelCase,
		ForcePascalCase = 8 + PreferPascalCase,
		ForceUpperCase = 8 + PreferUpperCase,
	}
}


