// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
// Re Sharper disable ConditionIsAlwaysTrueOrFalse

namespace Lexxys;

/// <summary>
/// Directory structure generation mode
/// </summary>
public enum DirectoryGenerationMode
{
	/// <summary>
	/// id: 1234567 -> "/12/34"
	/// </summary>
	BigEndian,
	/// <summary>
	/// id: 1234567 -> "/67/45"
	/// </summary>
	LittleEndian,
	/// <summary>
	/// id: 1234567 -> "/34/12"
	/// </summary>
	Compatible,
}


