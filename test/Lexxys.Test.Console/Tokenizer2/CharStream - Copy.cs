// Lexxys Infrastructural library.
// file: SpanStream.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lexxys.Tokenizer2
{
	/// <summary>
	/// Represents a characters stream, optimized for parsing purposes.
	/// </summary>
	public readonly ref struct CharStream<T> where T: IEquatable<T>
	{
		private readonly ReadOnlySpan<T> _start;
		private readonly ReadOnlySpan<T> _buffer;
		private readonly int _position;
		private readonly CultureInfo _culture;

		public const int DefaultTabSize = 4;

		/// <summary>
		/// Creates a new SpanStream.
		/// </summary>
		/// <param name="value">Content of the stream</param>
		public CharStream(in ReadOnlySpan<T> value)
		{
			_buffer = _start = value;
			_position = 0;
			_culture = CultureInfo.InvariantCulture;
		}

		private CharStream(in ReadOnlySpan<T> start, in ReadOnlySpan<T> buffer, int position, CultureInfo culture)
		{
			_start = start;
			_buffer = buffer;
			_position = position;
			_culture = culture ?? CultureInfo.InvariantCulture;
		}

		/// <summary>
		/// Gets the character at a specified relative character position in the current <see cref="SpanStream"/> object.
		/// </summary>
		/// <param name="index">A relative character position in the <see cref="SpanStream"/>.</param>
		/// <returns>
		///		A Unicode character if absolute position within the <see cref="SpanStream"/> buffer boudaries;
		///		<see cref="BofMarker"/> if absolute position less then zero;
		///		<see cref="EofMarker"/> if absolute position greater or equal to the length of <see cref="SpanStream"/>.
		///	</returns>
		public T this[int index] => index >= 0 && index < _buffer.Length ? _buffer[index]: default;

		/// <summary>
		/// Gets number of characters in the current <see cref="SpanStream"/>.
		/// </summary>
		public int Length => _buffer.Length;

		public int Position => _position;

		public CultureInfo Culture => _culture;

		/// <summary>
		/// It indicates that the end of the stream has been encountered.
		/// </summary>
		public bool Eof => _buffer.Length == 0;

		/// <summary>
		/// Sets the stream position.
		/// </summary>
		/// <param name="position">New srteam position</param>
		public CharStream<T> Move(int position)
			=> position <= 0 ? SeekBegin(): position >= _start.Length ? SeekEnd(): Seek(position);

		/// <summary>
		/// Repositions the stream pointer to the beginning of the stream.
		/// </summary>
		public CharStream<T> Rewind()
			=> SeekBegin();

		/// <summary>
		/// Moves the stream position.
		/// </summary>
		/// <param name="length">Number of characters to move</param>
		public CharStream<T> Forward(int length)
			=> length <= 0 ? this: length >= _buffer.Length ? SeekEnd(): Shift(length);

		/// <summary>
		/// Creates a <see cref="LexicalToken"/> from the stream buffer fragment and move current stream position forward on <paramref name="length"/> characters.
		/// </summary>
		/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
		/// <param name="length">Number of characters to copy from current position of stream into the content of new <see cref="LexicalToken"/>.</param>
		/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
		public LexicalToken Token(LexicalTokenType tokenType, int length, CultureInfo culture = null)
			=> new LexicalToken(tokenType, Substring(0, length), _position, culture);

		/// <summary>
		/// Creates a <see cref="LexicalToken"/> and move current stream position forward on <paramref name="length"/> characters.
		/// </summary>
		/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
		/// <param name="length">Number of characters to move.</param>
		/// <param name="text">Content of the <see cref="LexicalToken"/>.</param>
		/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
		public LexicalToken Token(LexicalTokenType tokenType, string text, CultureInfo culture = default)
			=> new LexicalToken(tokenType, text, _position, culture);

		/// <summary>
		/// Creates a <see cref="LexicalToken"/> from the stream buffer fragment and move current stream position forward on <paramref name="length"/> characters.
		/// </summary>
		/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
		/// <param name="length">Number of characters to copy from current position of stream into the content (textual value) of new <see cref="LexicalToken"/>.</param>
		/// <param name="value">Value of the <see cref="LexicalToken"/>.</param>
		/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
		public LexicalToken Token(LexicalTokenType tokenType, int length, object value, CultureInfo culture = default)
			=> new LexicalToken(tokenType, Substring(0, length), _position, value, culture);

		/// <summary>
		/// Creates a <see cref="LexicalToken"/> and move current stream position forward on <paramref name="length"/> characters.
		/// </summary>
		/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
		/// <param name="length">Number of characters to move.</param>
		/// <param name="text">Content of the <see cref="LexicalToken"/>.</param>
		/// <param name="value">Value of the <see cref="LexicalToken"/>.</param>
		/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
		public LexicalToken Token(LexicalTokenType tokenType, string text, object value, CultureInfo cultureInfo = default)
			=> new LexicalToken(tokenType, text, _position, value, cultureInfo);

		/// <summary>
		/// Gets a substring from the srteam buffer.
		/// </summary>
		/// <param name="start">Offset from current position in the stream</param>
		/// <param name="length">Length of the substring</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="start"/> is less then zero.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="length"/> is less then zero.</exception>
		public string Substring(int start, int length)
		{
			if (start < 0)
				throw new ArgumentOutOfRangeException(nameof(start), start, null);
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, null);

			if (start + length <= _buffer.Length)
				return _buffer.Slice(start, length).ToString();
			return start < _buffer.Length ? _buffer.Slice(start).ToString(): String.Empty;
		}

		public ReadOnlySpan<T> Left(int length)
		{
			return _buffer.Slice(0, Math.Min(_buffer.Length, length));
		}

		private CharStream<T> Shift(int offset)
			=> new CharStream<T>(_start, _start.Slice(_position + offset), _position + offset, _culture);

		private CharStream<T> Seek(int position)
			=> new CharStream<T>(_start, _start.Slice(position), position, _culture);

		private CharStream<T> SeekBegin()
			=> new CharStream<T>(_start, _start, 0, _culture);

		private CharStream<T> SeekEnd()
			=> new CharStream<T>(_start, default, _start.Length, _culture);

		public ReadOnlySpan<T> Slice(int start, int length)
			=> _start.Slice(start, length);

		/// <summary>
		/// Reports the index of the first occurrence of the specified character in this Stream.
		/// </summary>
		/// <param name="value">Character to seek</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		public int IndexOf(T value)
			=> _buffer.IndexOf(value);

		/// <summary>
		/// Reports the index of the first occurrence of the specified string in this Stream.
		/// </summary>
		/// <param name="value">String to seek</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="value"/> is null.</exception>
		public int IndexOf(ReadOnlySpan<T> value) => _buffer.IndexOf(value);

		/// <summary>
		/// Reports the index of the first occurrence of the specified character in this Stream.
		/// </summary>
		/// <param name="value">Character to seek.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public int IndexOf(T value, int offset)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
			if (offset == 0)
				return _buffer.IndexOf(value);
			if (offset < 0 || offset >= _buffer.Length)
				return -1;
			int k = _buffer.Slice(offset).IndexOf(value);
			return k < 0 ? k: k + offset;
		}

		/// <summary>
		/// Reports the index of the first occurrence of the specified string in this Stream.
		/// </summary>
		/// <param name="value">String to seek.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="value"/> is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public int IndexOf(ReadOnlySpan<T> value, int offset)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			if (offset == 0)
				return _buffer.IndexOf(value);
			if (offset < 0 || offset >= _buffer.Length)
				return -1;
			int k = _buffer.Slice(offset).IndexOf(value);
			return k < 0 ? k : k + offset;
		}

		/// <summary>
		/// Reports the index of the first occurrence in this Stream of any character in a specified array of characters.
		/// </summary>
		/// <param name="any">A character array containing one or more characters to seek.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="any"/> is null.</exception>
		public int IndexOfAny(T[] any)
		{
			return any == null || any.Length == 0 ? -1: _buffer.IndexOfAny(any.AsSpan());
		}

		/// <summary>
		/// Reports the index of the first occurrence in this Stream of any character in a specified array of characters.
		/// </summary>
		/// <param name="any">A character array containing one or more characters to seek.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="any"/> is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public int IndexOfAny(T[] any, int offset)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
			if (any == null || any.Length == 0)
				return -1;
			if (offset == 0)
				return _buffer.IndexOfAny(any.AsSpan());
			var i = _buffer.Slice(offset).IndexOfAny(any.AsSpan());
			return i < 0 ? -1: i + offset;
		}

		/// <summary>
		/// Reports the index of the first symbol in this Stream for which the specified <paramref name="predicate"/> returns true.
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="offset"></param>
		/// <returns>Offset of the character where the <paramref name="predicate"/> returns false.</returns>
		public int IndexOf(Func<T, bool> predicate, int offset = 0)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (predicate(_buffer[i]))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Reports the index of the first symbol in this Stream for which the specified <paramref name="predicate"/> returns true.
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="offset"></param>
		/// <returns>Offset of the character where the <paramref name="predicate"/> returns false.</returns>
		public int IndexOf(Func<T, int, bool> predicate, int offset = 0)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (predicate(_buffer[i], i - offset))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Returns number of continuous characters satisfied to the specified <paramref name="predicate"/>.
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="offset"></param>
		public int Match(Func<T, bool> predicate, int offset = 0)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i]))
					return i - offset;
			}
			return _buffer.Length - offset;
		}

		/// <summary>
		/// Returns number of continuous characters satisfied to the specified <paramref name="predicate"/>.
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="offset"></param>
		public int Match(Func<T, int, bool> predicate, int offset = 0)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i], i - offset))
					return i - offset;
			}
			return _buffer.Length - offset;
		}

		/// <summary>
		/// Moves current podition of the <see cref="SpanStream"/> forward until the <paramref name="predicate"/> is true.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public CharStream<T> Forward(Func<T, bool> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			for (int i = 0; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i]))
					return Shift(i);
			}
			return SeekEnd();
		}

		/// <summary>
		/// Moves current podition of the <see cref="SpanStream"/> forward until the <paramref name="predicate"/> is true starting from specified <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public CharStream<T> Forward(int offset, Func<T, bool> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i]))
					return Forward(i);
			}
			return SeekEnd();
		}

		/// <summary>
		/// Determines whether the <see cref="SpanStream"/> matches the specified <paramref name="value"/> at the specified <paramref name="offset"/>.
		/// </summary>
		/// <param name="value">The string to compare.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>true if the value parameter matches the Stream; otherwise, false.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public bool StartsWith(ReadOnlySpan<T> value, int offset = 0)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			if (offset + value.Length > _buffer.Length)
				return false;
			return offset == 0 ? _buffer.StartsWith(value): _buffer.Slice(offset).StartsWith(value);
		}

		/// <summary>
		/// Displays current position and current 120 characters of the stream.
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{_position}: {Strings.Ellipsis(Strings.EscapeCsString(Substring(0, 120)), 120, "...\"")}";
	}
}
