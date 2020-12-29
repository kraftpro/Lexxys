// Lexxys Infrastructural library.
// file: CryptoProviderSetting.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Lexxys.Xml;

namespace Lexxys.Crypting
{
	public enum CryptoProviderType
	{
		None,
		Hasher,
		Encryptor,
		Decryptor,
	}

	public class CryptoProviderSettingItem
	{
		public CryptoProviderType Type;
		public string Name;
		public string Class;
		public string Assembly;

		public CryptoProviderSettingItem(CryptoProviderType type, string name, string @class, string assembly = null)
		{
			Type = type;
			Name = name;
			Class = @class;
			Assembly = assembly;
		}
	}
}


