// Lexxys Infrastructural library.
// file: Crypto.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Lexxys.Configuration;
using System.Linq;
using Lexxys;

namespace Lexxys.Crypting
{

	public static class Crypto
	{
		public const string ConfigSection = "lexxys.crypto.providers";

		private static readonly ConcurrentDictionary<AlgKeyPair, Hasher> _hasherCache = new ConcurrentDictionary<AlgKeyPair, Hasher>(AlgKeyPair.Comparer);
		private static readonly ConcurrentDictionary<AlgKeyPair, Encryptor> _encryptorCache = new ConcurrentDictionary<AlgKeyPair, Encryptor>(AlgKeyPair.Comparer);
		private static readonly ConcurrentDictionary<AlgKeyPair, Decryptor> _decryptorCache = new ConcurrentDictionary<AlgKeyPair, Decryptor>(AlgKeyPair.Comparer);
		private static readonly object SyncObj = new object();

		public static Hasher Hasher(string algorithm)
		{
			return _hasherCache.GetOrAdd(new AlgKeyPair(algorithm, null), o => new Hasher((IHasherAlgorythm)CreateInstance(CryptoProviderType.Hasher, o.Algorithm)));
		}

		public static Hasher Hasher(string algorithm, object key)
		{
			return _hasherCache.GetOrAdd(new AlgKeyPair(algorithm, key), o => new Hasher((IHasherAlgorythm)CreateInstance(CryptoProviderType.Hasher, o.Algorithm, o.Key)));
		}

		public static Encryptor Encryptor(string algorithm, object key)
		{
			return _encryptorCache.GetOrAdd(new AlgKeyPair(algorithm, key), o => new Encryptor((IEncryptorAlgorythm)CreateInstance(CryptoProviderType.Encryptor, o.Algorithm, o.Key)));
		}

		public static Decryptor Decryptor(string algorithm, object key)
		{
			return _decryptorCache.GetOrAdd(new AlgKeyPair(algorithm, key), o => new Decryptor((IDecryptorAlgorythm)CreateInstance(CryptoProviderType.Decryptor, o.Algorithm, o.Key)));
		}

		private struct AlgKeyPair: IEquatable<AlgKeyPair>
		{
			public static readonly IEqualityComparer<AlgKeyPair> Comparer = new EqualityComparer();
			public readonly string Algorithm;
			public readonly object Key;

			public AlgKeyPair(string algorithm, object key)
			{
				Algorithm = algorithm ?? throw EX.ArgumentNull(nameof(algorithm));
				Key = key;
			}

			public override bool Equals(object obj)
			{
				return obj is AlgKeyPair akp && Equals(akp);
			}

			public bool Equals(AlgKeyPair other)
			{
				return Algorithm == other.Algorithm && EqualityComparer<object>.Default.Equals(Key, other.Key);
			}

			public override int GetHashCode()
			{
				return HashCode.Join(Algorithm.GetHashCode(), Key?.GetHashCode() ?? 0);
			}

			public override string ToString()
			{
				return Algorithm + ": " + (Key == null ? "<null>": Key.ToString());
			}

			private class EqualityComparer : IEqualityComparer<AlgKeyPair>
			{
				public bool Equals(AlgKeyPair x, AlgKeyPair y) => x.Equals(y);
				public int GetHashCode(AlgKeyPair obj) => obj.GetHashCode();
			}
		}

		private static object CreateInstance(CryptoProviderType providerType, string name, params object[] args)
		{
			CryptoProviderSettingItem item = Settings(providerType, name);
			if (item == null)
				throw new ArgumentOutOfRangeException(nameof(name), name, null)
					.Add(nameof(providerType), providerType);

			try
			{
				Type type = Factory.GetType(item.Class);
				if (type == null && item.Assembly != null && Factory.TryLoadAssembly(item.Assembly, false) != null)
					type = Factory.GetType(item.Class);
				if (type == null)
					throw EX.InvalidOperation(SR.CR_CannotCreateAgorithm(item.Class, name));
				return args == null || args.Length == 0 ? Factory.TryConstruct(type, true) : Factory.TryConstruct(type, true, args);
			}
			catch (Exception flaw)
			{
				flaw
					.Add("providerType", providerType)
					.Add("name", name)
					.Add("class", item.Class);
				throw;
			}
		}

		private static CryptoProviderSettingItem Settings(CryptoProviderType providerType, string name)
		{
			if (__settings == null)
			{
				lock (SyncObj)
				{
					if (__settings == null)
					{
						const string Cryptors = "Lexxys.Crypting.Cryptors.";
						__settings = Config.Default.GetCollection<CryptoProviderSettingItem>(ConfigSection).Value?.ToList() ?? new List<CryptoProviderSettingItem>();
						foreach (var x in new[] { "Md5", "Sha1", "Sha2", "Sha3", "Sha5", "Des", "Hma" })
						{
							__settings.Add(new CryptoProviderSettingItem(CryptoProviderType.Hasher, x, Cryptors + x + "Hasher"));
						}
						foreach (var x in new[] { "Des", "TripleDes" })
						{
							__settings.Add(new CryptoProviderSettingItem(CryptoProviderType.Encryptor, x, Cryptors + x + "Cryptor"));
							__settings.Add(new CryptoProviderSettingItem(CryptoProviderType.Decryptor, x, Cryptors + x + "Cryptor"));
						}
						foreach (var x in new[] { "Rsa" })
						{
							__settings.Add(new CryptoProviderSettingItem(CryptoProviderType.Encryptor, x, Cryptors + x + "Encryptor"));
							__settings.Add(new CryptoProviderSettingItem(CryptoProviderType.Decryptor, x, Cryptors + x + "Decryptor"));
						}
						foreach (var x in __settings.Where(o=> o.Name == "Md5").ToList())
						{
							__settings.Add(new CryptoProviderSettingItem(x.Type, "Md5p", x.Class, x.Assembly));
						}
						foreach (var x in __settings.Where(o => o.Name == "Sha1").ToList())
						{
							__settings.Add(new CryptoProviderSettingItem(x.Type, "Sha", x.Class, x.Assembly));
						}
						foreach (var x in __settings.Where(o=> o.Name == "TripleDes").ToList())
						{
							__settings.Add(new CryptoProviderSettingItem(x.Type, "Des3", x.Class, x.Assembly));
						}
					}
				}
			}
			return __settings.Find(x => x.Type == providerType && String.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
		}
		private static List<CryptoProviderSettingItem> __settings;
	}
}
