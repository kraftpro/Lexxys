// Lexxys Infrastructural library.
// file: ICryptors.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Crypting;

public interface IHasherAlgorithm
{
	bool SupportsStream { get; }
	bool SupportsBlock { get; }
	int HashSize { get; }
	byte[] Hash(byte[] text, int offset, int length);
	byte[] HashStream(Stream text);
}
public interface IEncryptorAlgorithm
{
	bool SupportsStream { get; }
	bool SupportsBlock { get; }
	int BlockSize { get; }
	byte[] Encrypt(byte[] text, int offset, int length);
	void EncryptStream(Stream bits, Stream text);
}
public interface IDecryptorAlgorithm
{
	bool SupportsStream { get; }
	bool SupportsBlock { get; }
	int BlockSize { get; }
	byte[] Decrypt(byte[] bits, int offset, int length);
	void DecryptStream(Stream text, Stream bits);
}


