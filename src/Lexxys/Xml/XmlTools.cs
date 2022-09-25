// Lexxys Infrastructural library.
// file: XmlTools.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Xml
{
	public static partial class XmlTools
	{
		private static ILogging Log => __logger ??= StaticServices.Create<ILogging>("XmlTools");
		private static ILogging? __logger;

		public const string OptionIgnoreCase = "opt:ignoreCase";
		public const string OptionForceAttributes = "opt:forceAttributes";
	}
}
