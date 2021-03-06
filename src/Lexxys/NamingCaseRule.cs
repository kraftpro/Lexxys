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
		PreferLowerCaseWithDashes = 5,
		PreferPascalCaseWithDashes = 6,
		PreferUpperCaseWithDashes = 7,
		PreferLowerCaseWithUnserscores = 8,
		PreferPascalCaseWithUnserscores = 9,
		PreferUpperCaseWithUnserscores = 10,

		Force = 32,
		ForceLowerCase = Force + PreferLowerCase,
		ForceCamelCase = Force + PreferCamelCase,
		ForcePascalCase = Force + PreferPascalCase,
		ForceUpperCase = Force + PreferUpperCase,
		ForceLowerCaseWithDashes = Force + PreferLowerCaseWithDashes,
		ForcePascalCaseWithDashes = Force + PreferPascalCaseWithDashes,
		ForceUpperCaseWithDashes = Force + PreferUpperCaseWithDashes,
		ForceLowerCaseWithUnserscores = Force + PreferLowerCaseWithUnserscores,
		ForceUpperCaseWithUnserscores = Force + PreferUpperCaseWithUnserscores,
		ForcePascalCaseWithUnserscores = Force + PreferPascalCaseWithUnserscores,
	}
}


