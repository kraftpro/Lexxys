// Lexxys Infrastructural library.
// file: Crypto.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Lexxys.Crypting
{

	public static partial class Crypto
	{
		public const string ConfigSection = "lexxys.crypto.providers";

		private static readonly ConcurrentDictionary<AlgKeyPair, Hasher> _hasherCache = new ConcurrentDictionary<AlgKeyPair, Hasher>(AlgKeyPair.Comparer);
		private static readonly ConcurrentDictionary<AlgKeyPair, Encryptor> _encryptorCache = new ConcurrentDictionary<AlgKeyPair, Encryptor>(AlgKeyPair.Comparer);
		private static readonly ConcurrentDictionary<AlgKeyPair, Decryptor> _decryptorCache = new ConcurrentDictionary<AlgKeyPair, Decryptor>(AlgKeyPair.Comparer);
		private static readonly object SyncObj = new object();

		public static Hasher Hasher(string algorithm) => _hasherCache.GetOrAdd(
			new AlgKeyPair(algorithm, null),
			o => new Hasher((IHasherAlgorythm)CreateInstance(CryptoProviderType.Hasher, o.Algorithm)));

		public static Hasher Hasher(string algorithm, object key) => _hasherCache.GetOrAdd(
			new AlgKeyPair(algorithm, key),
			o => new Hasher((IHasherAlgorythm)CreateInstance(CryptoProviderType.Hasher, o.Algorithm, o.Key)));

		public static Encryptor Encryptor(string algorithm, object key) => _encryptorCache.GetOrAdd(
			new AlgKeyPair(algorithm, key),
			o => new Encryptor((IEncryptorAlgorythm)CreateInstance(CryptoProviderType.Encryptor, o.Algorithm, o.Key)));

		public static Decryptor Decryptor(string algorithm, object key) => _decryptorCache.GetOrAdd(
			new AlgKeyPair(algorithm, key),
			o => new Decryptor((IDecryptorAlgorythm)CreateInstance(CryptoProviderType.Decryptor, o.Algorithm, o.Key)));

		private readonly struct AlgKeyPair: IEquatable<AlgKeyPair>
		{
			public static readonly IEqualityComparer<AlgKeyPair> Comparer = new EqualityComparer();
			public readonly string Algorithm;
			public readonly object? Key;

			public AlgKeyPair(string algorithm, object? key)
			{
				Algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
				Key = key;
			}

			public override bool Equals(object? obj)
				=> obj is AlgKeyPair akp && Equals(akp);

			public bool Equals(AlgKeyPair other)
				=> Algorithm == other.Algorithm && Object.Equals(Key, other.Key);

			public override int GetHashCode()
				=> HashCode.Join(Algorithm.GetHashCode(), Key?.GetHashCode() ?? 0);

			public override string ToString()
				=> Algorithm + ": " + (Key?.ToString() ?? "<null>");

			private class EqualityComparer: IEqualityComparer<AlgKeyPair>
			{
				public bool Equals(AlgKeyPair x, AlgKeyPair y) => x.Equals(y);
				public int GetHashCode(AlgKeyPair obj) => obj.GetHashCode();
			}
		}

		private static object CreateInstance(CryptoProviderType providerType, string name, params object?[] args)
		{
			CryptoProviderSettingItem? item = Settings(providerType, name);
			if (item == null)
				throw new ArgumentOutOfRangeException(nameof(name), name, null)
					.Add(nameof(providerType), providerType);

			try
			{
				return (args == null || args.Length == 0 ? Factory.TryConstruct(item.Type, true): Factory.TryConstruct(item.Type, true, args)) ?? throw new ArgumentException(SR.Factory_CannotFindConstructor(item.Type, args?.Length ?? 0));
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

		private static CryptoProviderSettingItem? Settings(CryptoProviderType providerType, string name)
		{
			if (__settings == null)
			{
				lock (SyncObj)
				{
#pragma warning disable CA1508 // Avoid dead conditional code.  __settings could be changed in a parallel thread
					if (__settings == null)
					{
						const string Cryptors = "Lexxys.Crypting.Cryptors.";
						__settings = Config.Current.GetCollection<CryptoProviderSettingItem>(ConfigSection).Value?.ToList() ?? new List<CryptoProviderSettingItem>();
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
						var synonyms = new[]
						{
							("MD5", new [] { "MD5P" }),
							("Sha1", new [] { "SHA128", "SHA-128", "SHA" }),
							("Sha2", new [] { "SHA256", "SHA-256" }),
							("Sha3", new [] { "SHA384", "SHA-384" }),
							("Sha5", new [] { "SHA512", "SHA-512" }),
							("TripleDes", new [] { "3DES", "DES3" }),
						};
						foreach (var key in synonyms)
						{
							var i = __settings.FindIndex(o => String.Equals(o.Name, key.Item1, StringComparison.OrdinalIgnoreCase));
							var x = __settings[i];
							foreach (var syn in key.Item2)
							{
								__settings.Add(new CryptoProviderSettingItem(x.ProviderType, syn, x.Class, x.Assembly));
							}
						}
					}
#pragma warning restore CA1508 // Avoid dead conditional code
				}
			}
			return __settings.Find(x => x.ProviderType == providerType && String.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
		}
		private static volatile List<CryptoProviderSettingItem>? __settings;
	}
}
