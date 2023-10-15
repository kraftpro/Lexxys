
// Lexxys Infrastructural library.
// file: NamingCaseRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

[Flags]
public enum NamingCaseRule
{
	None = 0,
	PreferLowerCase = 1,
	PreferCamelCase = 2,
	PreferPascalCase = 3,
	PreferUpperCase = 4,

	Dash = 8,
	Underscore = 16,
	Dot = 24,
	Separators = Dot,

	Force = 32,

	PreferKebabCase = Dash + PreferLowerCase,
	PreferSnakeCase = Underscore + PreferLowerCase,
	PreferDottedCase = Dot + PreferLowerCase,

	ForceLowerCase = Force + PreferLowerCase,
	ForceCamelCase = Force + PreferCamelCase,
	ForcePascalCase = Force + PreferPascalCase,
	ForceUpperCase = Force + PreferUpperCase,
	ForceKebabCase = Force + PreferKebabCase,
	ForceSnakeCase = Force + PreferSnakeCase,
	ForceDottedCase = Force + PreferDottedCase,
}


