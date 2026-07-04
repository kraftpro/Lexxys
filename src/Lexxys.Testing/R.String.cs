using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Testing;

public static partial class R
{
	/// <summary>
	/// Creates a new random item that returns the string representation of the specified item using the provided format
	/// and format provider.
	/// </summary>
	/// <remarks>Use this method to convert a random item of any type to a random item of strings, formatted
	/// according to the specified format and culture. The resulting random item will generate a new formatted string each
	/// time its value is retrieved.</remarks>
	/// <typeparam name="T">The type of the value contained in the random item.</typeparam>
	/// <param name="item">The random item whose value will be formatted as a string.</param>
	/// <param name="format">A standard or custom format string that defines how the value should be formatted.</param>
	/// <param name="formatProvider">An object that supplies culture-specific formatting information, or null to use the current culture.</param>
	/// <returns>A <see cref="RandItem{T}"><c>RandItem&lt;string&gt;</c></see> that produces the formatted string representation of the original item's value.</returns>
	public static RandItem<string> Fmt<T>(RandItem<T> item, string format, IFormatProvider? formatProvider = null) => new RandItem<string>(() => item.ToString(format, formatProvider));

	/// <summary>
	/// Returns a new <see cref="RandItem{T}">RandItem&lt;<see cref="string"/>&gt;</see> returning the formatted string value of the <paramref name="item"/> using randomly selected <paramref name="format"/> and the specified <paramref name="formatProvider"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="item">The item to format.</param>
	/// <param name="format">Format string generator.</param>
	/// <param name="formatProvider">Format provider.</param>
	/// <returns></returns>
	public static RandItem<string> Fmt<T>(RandItem<T> item, RandItem<string> format, IFormatProvider? formatProvider = null) => new RandItem<string>(() => item.ToString(format, formatProvider));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> containing values of the specified <paramref name="items"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="items">Items to concatenate.</param>
	/// <returns></returns>
	public static RandItem<string> Concat<T>(params RandItem<T>[] items) => new RandItem<string>(() => String.Join(null, items));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> containing concatenated values of the specified <paramref name="items"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="items">Items to concatenate.</param>
	/// <returns></returns>
	public static RandItem<string> Concat<T>(IReadOnlyCollection<RandItem<T>> items) => new RandItem<string>(() => String.Join(null, items));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> containing concatenated values of the specified <paramref name="items"/> converted to string using the specified <paramref name="convert"/> function.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="convert">Conversion function.</param>
	/// <param name="items">Items to concatenate.</param>
	/// <returns></returns>
	public static RandItem<string> Concat<T>(Func<T, string> convert, params RandItem<T>[] items) => new RandItem<string>(() => String.Join(null, items.Select(o => convert(o!))));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> containing concatenated values of the specified <paramref name="items"/> converted to string using the specified <paramref name="convert"/> function.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="convert">Conversion function.</param>
	/// <param name="items">Items to concatenate.</param>
	/// <returns></returns>
	public static RandItem<string> Concat<T>(Func<T, string> convert, IReadOnlyCollection<RandItem<T>> items) => new RandItem<string>(() => String.Join(null, items.Select(o => convert(o!))));
}
