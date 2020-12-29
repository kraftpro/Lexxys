// Lexxys Infrastructural library.
// file: CharStream.cs
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
	public readonly ref struct ByteStream
	{
		private readonly ReadOnlySpan<byte> _start;
		private readonly ReadOnlySpan<byte> _buffer;
		private readonly int _position;
		private readonly CultureInfo _culture;

		public const byte BofMarker = 0;
		public const byte EofMarker = (byte)'\xFF';
		public const byte NewLineMarker = (byte)'\n';
		public const int DefaultTabSize = 8;

		/// <summary>
		/// Creates a new Utf8CharStream.
		/// </summary>
		/// <param name="value">Content of the stream</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="value"/>is null</exception>
		public ByteStream(byte[] value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			_buffer = _start = value.AsSpan();
			_position = 0;
			_culture = CultureInfo.InvariantCulture;
		}

		/// <summary>
		/// Creates a new Utf8CharStream.
		/// </summary>
		/// <param name="stream">Input stream</param>
		/// <param name="tabSize">Tab size (default 4)</param>
		/// <param name="culture">Culture-specific information (default null)</param>
		/// <exception cref="ArgumentNullException"><paramref name="stream"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="tabSize"/> is less than zero or greater than 32</exception>
		public ByteStream(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			using (var mem = new MemoryStream(stream.CanSeek ? (int)stream.Length: 512))
			{
				stream.CopyTo(mem);
				_buffer = _start = mem.GetBuffer().AsSpan((int)mem.Length);
			}
			_position = 0;
			_culture = CultureInfo.InvariantCulture;
		}

		private ByteStream(in ReadOnlySpan<byte> start, in ReadOnlySpan<byte> buffer, int position, CultureInfo culture)
		{
			_start = start;
			_buffer = buffer;
			_position = position;
			_culture = culture ?? CultureInfo.InvariantCulture;
		}

		/// <summary>
		/// Gets the character at a specified relative character position in the current <see cref="CharStream"/> object.
		/// </summary>
		/// <param name="index">A relative character position in the <see cref="CharStream"/>.</param>
		/// <returns>
		///		A Unicode character if absolute position within the <see cref="CharStream"/> buffer boudaries;
		///		<see cref="BofMarker"/> if absolute position less then zero;
		///		<see cref="EofMarker"/> if absolute position greater or equal to the length of <see cref="CharStream"/>.
		///	</returns>
		public byte this[int index] => index < 0 ? BofMarker: index >= _buffer.Length ? EofMarker: _buffer[index];

		/// <summary>
		/// Gets number of characters in the current <see cref="CharStream"/>.
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
		public ByteStream Move(int position)
			=> position <= 0 ? SeekBegin() : position >= _start.Length ? SeekEnd() : Seek(position);

		/// <summary>
		/// Repositions the stream pointer to the beginning of the stream.
		/// </summary>
		public ByteStream Rewind()
			=> SeekBegin();

		/// <summary>
		/// Moves the stream position.
		/// </summary>
		/// <param name="length">Number of characters to move</param>
		public ByteStream Forward(int length)
		{
			return length <= 0 ? this: length >= _buffer.Length ? SeekEnd(): Shift(length);
		}


		private ByteStream Shift(int offset)
			=> new ByteStream(_start, _start.Slice(_position + offset), _position + offset, _culture);

		private ByteStream Seek(int position)
			=> new ByteStream(_start, _start.Slice(position), position, _culture);

		private ByteStream SeekBegin()
			=> new ByteStream(_start, _start, 0, _culture);

		private ByteStream SeekEnd()
			=> new ByteStream(_start, default, _start.Length, _culture);

		public ReadOnlySpan<byte> Slice(int start, int length)
			=> _start.Slice(start, length);

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

		/// <summary>
		/// Reports the index of the first occurrence of the specified character in this Stream.
		/// </summary>
		/// <param name="value">Character to seek</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		public int IndexOf(byte value)
		{
			return _buffer.IndexOf(value);
		}

		/// <summary>
		/// Reports the index of the first occurrence of the specified string in this Stream.
		/// </summary>
		/// <param name="value">String to seek</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="value"/> is null.</exception>
		public int IndexOf(ReadOnlySpan<byte> value)
		{
			return value.Length == 0 ? -1: _buffer.IndexOf(value);
		}

		/// <summary>
		/// Reports the index of the first occurrence of the specified character in this Stream.
		/// </summary>
		/// <param name="value">Character to seek.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public int IndexOf(byte value, int offset)
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
		public int IndexOf(ReadOnlySpan<byte> value, int offset)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			if (value.Length == 0)
				return -1;
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
		public int IndexOfAny(byte[] any)
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
		public int IndexOfAny(byte[] any, int offset)
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
		public int IndexOf(Func<byte, bool> predicate, int offset = 0)
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
		public int IndexOf(Func<byte, int, bool> predicate, int offset = 0)
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
		public int Match(Func<byte, bool> predicate, int offset = 0)
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
		public int Match(Func<byte, int, bool> predicate, int offset = 0)
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
		/// Moves current podition of the <see cref="CharStream"/> forward until the <paramref name="predicate"/> is true.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public ByteStream Forward(Func<byte, bool> predicate)
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
		/// Moves current podition of the <see cref="CharStream"/> forward until the <paramref name="predicate"/> is true starting from specified <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public ByteStream Forward(int offset, Func<byte, bool> predicate)
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
		/// Determines whether the <see cref="CharStream"/> matches the specified <paramref name="value"/> at the specified <paramref name="offset"/>.
		/// </summary>
		/// <param name="value">The string to compare.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>true if the value parameter matches the Stream; otherwise, false.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public bool StartsWith(ReadOnlySpan<byte> value, int offset = 0)
		{
			if (value.Length == 0)
				return false;
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			if (offset + value.Length > _buffer.Length)
				return false;
			return offset == 0 ? _buffer.StartsWith(value): _buffer.Slice(offset).StartsWith(value);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message and current stream position.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
		public SyntaxException SyntaxException(string message, int tab = 0)
		{
			var at = GetPosition(tab);
			return new SyntaxException(message, null, at.Line + 1, at.Column + 1);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message, file info and current stream position.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="file">File info to include into the result</param>
		/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
		public SyntaxException SyntaxException(string message, string file, int tab = 0)
		{
			var at = GetPosition(tab);
			return new SyntaxException(message, file, at.Line + 1, at.Column + 1);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message and specified position in the stream.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="position">Position in the Stream.</param>
		/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
		public SyntaxException SyntaxException(string message, int position, int tab = 0)
		{
			var at = GetPosition(tab);
			return new SyntaxException(message, null, at.Line + 1, at.Column + 1);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message, file info and specified position in the stream.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="file">File info to include into the result</param>
		/// <param name="position">Position in the Stream.</param>
		/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
		public SyntaxException SyntaxException(string message, string file, int position, int tab = 0)
		{
			var at = GetPosition(position, tab);
			return new SyntaxException(message, file, at.Line + 1, at.Column + 1);
		}

		public CharPosition GetPosition(int tab = 0)
		{
			return GetPosition(_position, tab);
		}

		public CharPosition GetPosition(int position, CharPosition prev, int tab = 0)
		{
			return position < prev.Position ? GetPosition(position, tab):  GetPosition(prev, position - prev.Position, tab);
		}

		public CharPosition GetPosition(int position, int tab = 0)
		{
			if (position <= 0)
				return default;

			int end = position;
			if (end > _start.Length)
				end = _start.Length;
			var lc = OffsetBox(_start.Slice(0, end), tab);
			return new CharPosition(end, lc.Line, lc.Column);
		}

		private CharPosition GetPosition(CharPosition at, int offset, int tab)
		{
			if (offset <= 0)
				return at;

			int start = at.Position;
			int end = start + offset;
			if (end > _start.Length)
				end = _start.Length;
			var lc = OffsetBox(_start.Slice(start, end - start), tab);
			return new CharPosition(end, lc.Line + at.Line, lc.Line == 0 ? lc.Column + at.Column: lc.Column);
		}

		private (int Line, int Column) OffsetBox(ReadOnlySpan<byte> part, int tab)
		{
			if (part.Length == 0)
				return default;
			if (tab == 0)
				tab = DefaultTabSize;

			int line = 0;
			int column = 0;
			var newLineTokens = new ReadOnlySpan<byte>(NewLineTokensArray);
			int i = part.IndexOfAny(newLineTokens);
			if (i >= 0)
			{
				column = 0;
				do
				{
					++line;
					if (IsNewLineSeq(part, i))
						++i;
					part = part.Slice(i + 1);
				} while ((i = part.IndexOfAny(newLineTokens)) >= 0);
			}

			if (tab > 1)
			{
				i = part.IndexOf(TAB);
				if (i >= 0)
				{
					do
					{
						column += i;
						column = column - (column % tab) + tab;
						part = part.Slice(i + 1);
					} while ((i = part.IndexOf(TAB)) >= 0);
				}
			}
			return (line, column + part.Length);
		}
		private const byte CR = (byte)'\r';
		private const byte LF = (byte)'\n';
		private const byte FF = (byte)'\f';
		private const byte TAB = (byte)'\t';


		private static readonly byte[] NewLineTokensArray = new[] { CR, LF, FF };

		private static bool IsNewLineSeq(ReadOnlySpan<byte> part, int index)
		{
			byte c = part[index];
			return (c == CR || c == LF) && (c ^ part[index + 1]) == (CR ^ LF);
		}

		/// <summary>
		/// Displays current position and current 120 characters of the stream.
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{_position}: {Strings.Ellipsis(Strings.EscapeCsString(Substring(0, 120)), 120, "...\"")}";
	}
}
