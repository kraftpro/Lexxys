// Lexxys Infrastructural library.
// file: NamingCaseRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#pragma warning disable CA1027 // Mark enums with FlagsAttribute

namespace Lexxys;

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
	PreferLowerCaseWithUnderscores = 8,
	PreferPascalCaseWithUnderscores = 9,
	PreferUpperCaseWithUnderscores = 10,

	Force = 32,
	ForceLowerCase = Force + PreferLowerCase,
	ForceCamelCase = Force + PreferCamelCase,
	ForcePascalCase = Force + PreferPascalCase,
	ForceUpperCase = Force + PreferUpperCase,
	ForceLowerCaseWithDashes = Force + PreferLowerCaseWithDashes,
	ForcePascalCaseWithDashes = Force + PreferPascalCaseWithDashes,
	ForceUpperCaseWithDashes = Force + PreferUpperCaseWithDashes,
	ForceLowerCaseWithUnderscores = Force + PreferLowerCaseWithUnderscores,
	ForceUpperCaseWithUnderscores = Force + PreferUpperCaseWithUnderscores,
	ForcePascalCaseWithUnderscores = Force + PreferPascalCaseWithUnderscores,
}


