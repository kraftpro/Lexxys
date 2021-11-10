// Lexxys Infrastructural library.
// file: CryptoProviderSetting.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

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
		public CryptoProviderType ProviderType;
		public string Name;
		public string Class;
		public string Assembly;
		private Type _type;

		public CryptoProviderSettingItem(CryptoProviderType type, string name, string @class, string assembly = null)
		{
			ProviderType = type;
			Name = name;
			Class = @class;
			Assembly = assembly;
		}

		public Type Type
		{
			get
			{
				if (_type == null)
				{
					_type = Factory.GetType(Class);
					if (_type == null && Assembly != null && Factory.TryLoadAssembly(Assembly, false) != null)
						_type = Factory.GetType(Class);
					if (_type == null)
						throw EX.InvalidOperation(SR.CR_CannotCreateAgorithm(Class));
				}
				return _type; 
			}
		}
	}
}


