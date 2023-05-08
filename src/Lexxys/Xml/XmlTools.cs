// Lexxys Infrastructural library.
// file: XmlTools.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using Microsoft.Extensions.Logging;

namespace Lexxys.Xml;

public static partial class XmlTools
{
	private static ILogger? Log => __logger ??= Statics.TryGetLogger("Lexxys.XmlTools");
	private static ILogger? __logger;

	public const string OptionIgnoreCase = "opt:ignoreCase";
	public const string OptionForceAttributes = "opt:forceAttributes";
}
