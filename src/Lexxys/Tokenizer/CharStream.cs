// Lexxys Infrastructural library.
// file: CharStream.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lexxys.Tokenizer
{
	#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

	/// <summary>
	/// Represents a characters stream, optimized for parsing purposes.
	/// </summary>
	public class CharStream
	{
		private readonly string _buffer;
		private readonly int _tabSize;
		private readonly CultureInfo _culture;
		private int _position;

		public static readonly CharStream Empty = new CharStream("", false);

		public const char BofMarker = '\0';
		public const char EofMarker = '\uFFFF';
		public const int DefaultTabSize = 4;

		/// <summary>
		/// Creates a copy of CharScream.
		/// </summary>
		/// <param name="charStream"></param>
		/// <exception cref="System.ArgumentNullException"><paramref name="charStream"/>is null</exception>
		public CharStream(CharStream charStream)
		{
			if (charStream == null)
				throw new ArgumentNullException(nameof(charStream));

			_buffer = charStream._buffer;
			_position = charStream._position;
			_tabSize = charStream._tabSize;
			_culture = charStream._culture;
		}

		public CharStream(string buffer, int tabSize = 0, CultureInfo culture = null)
			: this(buffer, true, tabSize, culture)
		{
		}

		/// <summary>
		/// Creates a new CharStream.
		/// </summary>
		/// <param name="buffer">Content of the stream</param>
		/// <param name="appendNewLine">append new line at the end of stream if missing (default true)</param>
		/// <param name="tabSize">Tab size (default 4)</param>
		/// <param name="culture">Culture-specific information (default null)</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="buffer"/>is null</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="tabSize"/> is less than zero or greater than 32</exception>
		public CharStream(string buffer, bool appendNewLine, int tabSize = 0, CultureInfo culture = null)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (tabSize < 0 || tabSize > 32)
				throw new ArgumentOutOfRangeException(nameof(tabSize), tabSize, null);

			_buffer = KeepNewLine(buffer, appendNewLine);
			_tabSize = tabSize == 0 ? DefaultTabSize: tabSize;
			_culture = culture == null ? CultureInfo.InvariantCulture: _culture;
		}

		/// <summary>
		/// Creates a new CharStream.
		/// </summary>
		/// <param name="stream">Input stream</param>
		/// <param name="tabSize">Tab size (default 4)</param>
		/// <param name="culture">Culture-specific information (default null)</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="stream"/> is null</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="tabSize"/> is less than zero or greater than 32</exception>
		public CharStream(TextReader stream, int tabSize = 0, CultureInfo culture = null)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (tabSize < 0 || tabSize > 32)
				throw new ArgumentOutOfRangeException(nameof(tabSize), tabSize, null);

			_buffer = KeepNewLine(stream);
			_tabSize = tabSize == 0 ? DefaultTabSize: tabSize;
			_culture = culture == null ? CultureInfo.InvariantCulture : _culture;
		}

		private static string KeepNewLine(TextReader stream)
		{
			var buffer = new StringBuilder();
			string line;
			while ((line = stream.ReadLine()) != null)
			{
				buffer.Append(line).Append('\n');
			}
			return buffer.ToString();
		}

		private static unsafe string KeepNewLine(string text, bool appendNewLine)
		{
			int i = text.IndexOfAny(new [] {'\r', '\f', '\u2028', '\u2029'});
			if (i < 0)
				return (appendNewLine && (text.Length == 0 || text[text.Length-1] != '\n')) ? text + "\n": text;
			if (i > 0 && text[i-1] == '\n')
				--i;

			if (text.Length < 1024*64)
			{
				char* buffer = stackalloc char[text.Length + 1];
				return KeepNewLine(text, buffer, i, appendNewLine);
			}

			fixed (char* buffer = new char[text.Length + 1])
				return KeepNewLine(text, buffer, i, appendNewLine);
		}

		private static unsafe string KeepNewLine(string value, char* buffer, int index, bool appendNewLine)
		{
			char* p = buffer;
			int length = value.Length - 1;
			fixed (char* pvalue = value)
			{
				char* q = pvalue;
				for (int j = 0; j < index; ++j)
				{
					*p++ = *q++;
				}
				for (; index <= length; ++index)
				{
					char c = *q++;
					switch (c)
					{
						case '\r':
						case '\n':
							if (index < length)
							{
								if (*q == (c ^ ('\r' ^ '\n')))
								{
									++index;
									++q;
								}
							}
							c = '\n';
							break;
						case '\f':
						case '\u2028':
						case '\u2029':
							c = '\n';
							break;
					}
					*p++ = c;
				}
			}
			if (appendNewLine && p[-1] != '\n')
				*p++ = '\n';
			return new string(buffer, 0, (int)(p - buffer));
		}

		/// <summary>
		/// Creates a copy of the <see cref="CharStream"/>.
		/// </summary>
		/// <returns></returns>
		public CharStream Clone()
		{
			return new CharStream(this);
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
		public char this[int index]
		{
			get
			{
				index += _position;
				return index < 0 ? BofMarker: index >= _buffer.Length ? EofMarker: _buffer[index];
			}
		}

		/// <summary>
		/// Gets culture-specific information associated with the stream.
		/// </summary>
		public CultureInfo CultureInfo => _culture;

		/// <summary>
		/// Gets number of characters in the current <see cref="CharStream"/>.
		/// </summary>
		public int Length => _buffer.Length - _position;

		/// <summary>
		/// It indicates that the end of the stream has been encountered.
		/// </summary>
		public bool Eof => _position >= _buffer.Length;

		/// <summary>
		/// Gets current Tab size.
		/// </summary>
		public int TabSize => _tabSize;

		/// <summary>
		/// Gets current position in the stream.
		/// </summary>
		public int Position => _position;

		/// <summary>
		/// Sets the stream position.
		/// </summary>
		/// <param name="position">New srteam position</param>
		public void Move(int position)
		{
			_position = position <= 0 ? 0: position >= _buffer.Length ? _buffer.Length: position;
		}

		/// <summary>
		/// Repositions the stream pointer to the beginning of the stream.
		/// </summary>
		public void Rewind()
		{
			_position = 0;
		}

		/// <summary>
		/// Moves the stream position.
		/// </summary>
		/// <param name="length">Number of characters to move (0 if less then zero)</param>
		public void Forward(int length)
		{
			if (length <= 0)
				return;
			_position += length;
			if (_position > _buffer.Length)
				_position = _buffer.Length;
		}

		/// <summary>
		/// Creates a <see cref="LexicalToken"/> from the stream buffer fragment and move current stream position forward on <paramref name="length"/> characters.
		/// </summary>
		/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
		/// <param name="length">Number of characters to copy from current position of stream into the content of new <see cref="LexicalToken"/>.</param>
		/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
		public LexicalToken Token(LexicalTokenType tokenType, int length)
		{
			return Token(tokenType, length, Substring(0, length));
		}

		/// <summary>
		/// Creates a <see cref="LexicalToken"/> and move current stream position forward on <paramref name="length"/> characters.
		/// </summary>
		/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
		/// <param name="length">Number of characters to move.</param>
		/// <param name="text">Content of the <see cref="LexicalToken"/>.</param>
		/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
		public LexicalToken Token(LexicalTokenType tokenType, int length, string text)
		{
			if (Eof)
				return null;
			var token = new LexicalToken(tokenType, text, _position, _culture);
			Forward(length);
			return token;
		}

		/// <summary>
		/// Creates a <see cref="LexicalToken"/> from the stream buffer fragment and move current stream position forward on <paramref name="length"/> characters.
		/// </summary>
		/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
		/// <param name="length">Number of characters to copy from current position of stream into the content (textual value) of new <see cref="LexicalToken"/>.</param>
		/// <param name="value">Value of the <see cref="LexicalToken"/>.</param>
		/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
		public LexicalToken Token(LexicalTokenType tokenType, int length, object value)
		{
			return Token(tokenType, length, Substring(0, length), value);
		}

		/// <summary>
		/// Creates a <see cref="LexicalToken"/> and move current stream position forward on <paramref name="length"/> characters.
		/// </summary>
		/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
		/// <param name="length">Number of characters to move.</param>
		/// <param name="text">Content of the <see cref="LexicalToken"/>.</param>
		/// <param name="value">Value of the <see cref="LexicalToken"/>.</param>
		/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
		public LexicalToken Token(LexicalTokenType tokenType, int length, string text, object value)
		{
			if (Eof)
				return null;
			var token = new LexicalToken(tokenType, text, _position, value, _culture);
			Forward(length);
			return token;
		}

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

			start += _position;
			if (start + length <= _buffer.Length)
				return _buffer.Substring(start, length);
			return start < _buffer.Length ? _buffer.Substring(start): String.Empty;
		}

		/// <summary>
		/// Reports the index of the first occurrence of the specified character in this Stream.
		/// </summary>
		/// <param name="value">Character to seek</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		public int IndexOf(char value)
		{
			return _position < _buffer.Length ? _buffer.IndexOf(value, _position) - _position: -1;
		}

		/// <summary>
		/// Reports the index of the first occurrence of the specified string in this Stream.
		/// </summary>
		/// <param name="value">String to seek</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="value"/> is null.</exception>
		public int IndexOf(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return _position < _buffer.Length ? _buffer.IndexOf(value, _position, StringComparison.Ordinal) - _position: -1;
		}

		/// <summary>
		/// Reports the index of the first occurrence of the specified character in this Stream.
		/// </summary>
		/// <param name="value">Character to seek.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public int IndexOf(char value, int offset)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			return _position + offset < _buffer.Length ? _buffer.IndexOf(value, _position + offset) - _position: -1;
		}

		/// <summary>
		/// Reports the index of the first occurrence of the specified string in this Stream.
		/// </summary>
		/// <param name="value">String to seek.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="value"/> is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public int IndexOf(string value, int offset)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			return _position + offset < _buffer.Length ? _buffer.IndexOf(value, _position + offset, StringComparison.Ordinal) - _position: -1;
		}

		/// <summary>
		/// Reports the index of the first occurrence in this Stream of any character in a specified array of characters.
		/// </summary>
		/// <param name="any">A character array containing one or more characters to seek.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="any"/> is null.</exception>
		public int IndexOfAny(char[] any)
		{
			if (any == null)
				throw new ArgumentNullException(nameof(any));
			return IndexOfAny(any, 0);
		}

		/// <summary>
		/// Reports the index of the first occurrence in this Stream of any character in a specified array of characters.
		/// </summary>
		/// <param name="any">A character array containing one or more characters to seek.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="any"/> is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public int IndexOfAny(char[] any, int offset)
		{
			if (any == null)
				throw new ArgumentNullException(nameof(any));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			return _position + offset < _buffer.Length ? _buffer.IndexOfAny(any, _position + offset) - _position: -1;
		}

		/// <summary>
		/// Reports the index of the first occurrence in this Stream of specified reqular expression.
		/// </summary>
		/// <param name="regex">A regualr expression to seek.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>The zero-based index position of value if that regular expression is found, or -1 it is not.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="regex"/> is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public int IndexOf(Regex regex, int offset = 0)
		{
			if (regex == null)
				throw new ArgumentNullException(nameof(regex));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			if (_position + offset >= _buffer.Length)
				return -1;
			Match m = regex.Match(_buffer, _position + offset);
			if (!m.Success)
				return -1;
			return m.Index - _position;
		}

		/// <summary>
		/// Reports the index of the first symbol in this Stream for which the specified <paramref name="predicate"/> returns true.
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="offset"></param>
		/// <returns>Offset of the character where the <paramref name="predicate"/> returns false.</returns>
		public int IndexOf(Func<char, bool> predicate, int offset = 0)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			offset += _position;
			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (predicate(_buffer[i]))
					return i - _position;
			}
			return -1;
		}

		/// <summary>
		/// Reports the index of the first symbol in this Stream for which the specified <paramref name="predicate"/> returns true.
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="offset"></param>
		/// <returns>Offset of the character where the <paramref name="predicate"/> returns false.</returns>
		public int IndexOf(Func<char, int, bool> predicate, int offset = 0)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			offset += _position;
			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (predicate(_buffer[i], i - offset))
					return i - _position;
			}
			return -1;
		}

		/// <summary>
		/// Returns number of continuous characters satisfied to the specified <paramref name="predicate"/>.
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="offset"></param>
		public int Match(Func<char, bool> predicate, int offset = 0)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			offset += _position;
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
		public int Match(Func<char, int, bool> predicate, int offset = 0)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			offset += _position;
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
		public int Forward(Func<char, bool> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			for (int i = _position; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i]))
				{
					Forward(i - _position);
					return i - _position;
				}
			}
			Forward(_buffer.Length - _position);
			return _buffer.Length - _position;
		}

		/// <summary>
		/// Moves current podition of the <see cref="CharStream"/> forward until the <paramref name="predicate"/> is true starting from specified <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public int Forward(int offset, Func<char, bool> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			for (int i = _position + offset; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i]))
				{
					Forward(i - _position);
					return i - _position;
				}
			}
			Forward(_buffer.Length - _position);
			return _buffer.Length - _position;
		}

		/// <summary>
		/// Searches the first occurrence of a regular expression in the <see cref="CharStream"/>.
		/// </summary>
		/// <param name="regex">A regualr expression to search.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>An object that contains information about the match.</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="regex"/> is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public Match Match(Regex regex, int offset = 0)
		{
			if (regex == null)
				throw new ArgumentNullException(nameof(regex));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			if (_position + offset >= _buffer.Length)
				return null;
			Match m = regex.Match(_buffer, _position + offset);
			return m.Success ? m: null;
		}

		/// <summary>
		/// Determines whether the <see cref="CharStream"/> matches the specified <paramref name="value"/> at the specified <paramref name="offset"/>.
		/// </summary>
		/// <param name="value">The string to compare.</param>
		/// <param name="offset">The search starting position.</param>
		/// <param name="comparision">One of the enumeration values that determines how this string and value are compared.</param>
		/// <returns>true if the value parameter matches the Stream; otherwise, false.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public bool StartsWith(string value, int offset, StringComparison comparision)
		{
			if (value == null || value.Length == 0)
				return false;
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			offset += _position;
			if (offset + value.Length > _buffer.Length)
				return false;
			return _buffer.Substring(offset, value.Length).Equals(value, comparision);
		}

		/// <summary>
		/// Calculates offset value for absolute position in the <see cref="CharStream"/> buffer.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int Offset(int index)
		{
			return index - _position;
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message and current stream position.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
		public SyntaxException SyntaxException(string message)
		{
			var at = GetPosition();
			return new SyntaxException(message, null, at.Line + 1, at.Column + 1);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message, file info and current stream position.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="file">File info to include into the result</param>
		/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
		public SyntaxException SyntaxException(string message, string file)
		{
			var at = GetPosition();
			return new SyntaxException(message, file, at.Line + 1, at.Column + 1);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message and specified position in the stream.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="position">Position in the Stream.</param>
		/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
		public SyntaxException SyntaxException(string message, int position)
		{
			var at = GetPosition(position);
			return new SyntaxException(message, null, at.Line + 1, at.Column + 1);
		}

		/// <summary>
		/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message, file info and specified position in the stream.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="file">File info to include into the result</param>
		/// <param name="position">Position in the Stream.</param>
		/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
		public SyntaxException SyntaxException(string message, string file, int position)
		{
			var at = GetPosition(position);
			return new SyntaxException(message, file, at.Line + 1, at.Column + 1);
		}

		public CharPosition GetPosition()
		{
			return GetPosition(_position);
		}

		public CharPosition GetPosition(int position, CharPosition prev)
		{
			return position < prev.Position ? GetPosition(position):  GetPosition(prev, position - prev.Position);
		}

		public CharPosition GetPosition(int position)
		{
			if (position <= 0)
				return default;

			int end = Math.Min(position, _buffer.Length);
			var (line, column) = OffsetBox(_buffer, 0, 0, end);
			return new CharPosition(end, line, column);
		}

		private CharPosition GetPosition(CharPosition at, int offset)
		{
			if (offset <= 0)
				return at;

			int start = at.Position;
			int end = start + offset;
			if (end > _buffer.Length)
				end = _buffer.Length;
			var (line, column) = OffsetBox(_buffer, at.Column, start, end);
			return new CharPosition(end, line + at.Line, column);
		}

		private (int Line, int Column) OffsetBox(string part, int column, int start, int end)
		{
			if (start >= end)
				return default;

			int line = 0;
			int i = part.IndexOf('\n', start);
			if (i >= 0 && i < end)
			{
				column = 0;
				do
				{
					++line;
					start = i + 1;
				} while ((i = part.IndexOf('\n', start)) >= 0 && i < end);
			}

			if (_tabSize > 1)
			{
				i = part.IndexOf('\t', start);
				if (i >= 0 && i < end)
				{
					do
					{
						column += i - start;
						column = column - (column % _tabSize) + _tabSize;
						start = i + 1;
					} while ((i = part.IndexOf('\t', start)) >= 0 && i < end);
				}
			}
			return (line, column + (end - start));
		}

		/// <summary>
		/// Displays current position and current 120 characters of the stream.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format(_culture, "{0}: {1}", _position, _buffer == null ? "(null)": Strings.Ellipsis(Strings.EscapeCsString(Substring(0, 120)), 120, "…\""));
		}
	}
}


