// Lexxys Infrastructural library.
// file: ResultValueExtensions.cs
//
// Copyright (c) 2001-2024, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys;

public static partial class ResultValueExtensions
{
	public static ResultValue<T> AsResultValue<T>(this T value) => new ResultValue<T>(value);
}
