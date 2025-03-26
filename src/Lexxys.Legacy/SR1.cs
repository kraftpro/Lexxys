// Lexxys Infrastructural library.
// file: SR.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;
using System.Globalization;

namespace Lexxys
{
	static class SR1
	{
		internal static string AssocNodeMissReference() => "Lists of forward and backward references are unbalanced";

		internal static string CR_CannotCreateAlgorithm(string type) => String.Format(SR.Culture, "Cannot create instance of the cryptographic algorithm for type {0}.", type);
	}
}
