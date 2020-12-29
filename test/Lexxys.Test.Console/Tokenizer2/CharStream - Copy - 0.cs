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
	public ref struct CharStream
	{
		private readonly ReadOnlySpan<char> _start;
		private readonly bool _appentNewLine;
		private ReadOnlySpan<char> _buffer;
		private int _position;

		public const char BofMarker = '\0';
		public const char EofMarker = '\uFFFF';
		public const char NewLineMarker = '\n';
		public const int DefaultTabSize = 4;

		/// <summary>
		/// Creates a copy of CharScream.
		/// </summary>
		/// <param name="charStream"></param>
		/// <exception cref="System.ArgumentNullException"><paramref name="charStream"/>is null</exception>
		public CharStream(in CharStream charStream)
		{
			_start = charStream._start;
			_buffer = charStream._buffer;
			_position = charStream._position;
			_appentNewLine = charStream._appentNewLine;
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

			var text = KeepNewLine(buffer, appendNewLine);
			_start = text.AsSpan();
			_buffer = _start;
			_position = 0;
			_appentNewLine = appendNewLine;
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

			var text = KeepNewLine(stream);
			_start = text.AsSpan();
			_buffer = _start;
			_position = 0;
			_appentNewLine = true;
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
		public char this[int index] =>
			index < 0 ? BofMarker:
			index < _buffer.Length ?  _buffer[index]:
			index == _buffer.Length && _appentNewLine ? NewLineMarker: EofMarker;

		/// <summary>
		/// Gets number of characters in the current <see cref="CharStream"/>.
		/// </summary>
		public int Length => _buffer.Length + (_appentNewLine ? 1: 0);

		/// <summary>
		/// It indicates that the end of the stream has been encountered.
		/// </summary>
		public bool Eof => _position >= _start.Length && (_position > _start.Length || !_appentNewLine);

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
			_position = position <= 0 ? 0: position;
			_buffer = _position == 0 ? _start: _start.Slice(_position < _start.Length ? _position: _start.Length);
		}

		/// <summary>
		/// Repositions the stream pointer to the beginning of the stream.
		/// </summary>
		public void Rewind()
		{
			_position = 0;
			_buffer = _start;
		}

		/// <summary>
		/// Moves the stream position.
		/// </summary>
		/// <param name="length">Number of characters to move</param>
		public void Forward(int length)
		{
			Move(_position + length);
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
		public LexicalToken Token(LexicalTokenType tokenType, int length, string text, CultureInfo cultureInfo = default)
		{
			if (Eof)
				return null;
			var token = new LexicalToken(tokenType, text, _position, cultureInfo);
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
		public LexicalToken Token(LexicalTokenType tokenType, int length, string text, object value, CultureInfo cultureInfo = default)
		{
			if (Eof)
				return null;
			LexicalToken token = new LexicalToken(tokenType, text, _position, value, cultureInfo);
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

			if (start + length <= _buffer.Length)
				return new String(_buffer.Slice(start, length).ToArray());
			return start < _buffer.Length ? new String(_buffer.Slice(start).ToArray()): String.Empty;
		}

		/// <summary>
		/// Reports the index of the first occurrence of the specified character in this Stream.
		/// </summary>
		/// <param name="value">Character to seek</param>
		/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
		public int IndexOf(char value)
		{
			return _buffer.IndexOf(value);
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
			return _buffer.IndexOf(value.AsSpan());
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
		public int IndexOf(string value, int offset)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			if (offset == 0)
				return _buffer.IndexOf(value.AsSpan());
			if (offset < 0 || offset >= _buffer.Length)
				return -1;
			int k = _buffer.Slice(offset).IndexOf(value.AsSpan());
			return k < 0 ? k : k + offset;
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
			var temp = offset == 0 ? _buffer: _buffer.Slice(offset);
			var anySpan = any.AsSpan();
			var i = temp.IndexOfAny(anySpan);
			return i >= 0 ? i + offset:
				_appentNewLine && anySpan.IndexOf('\n') < 0 ? -1: _buffer.Length;
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

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (predicate(_buffer[i]))
					return i;
			}
			if (_appentNewLine && predicate('\n'))
				return _buffer.Length;
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

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (predicate(_buffer[i], i - offset))
					return i;
			}
			if (_appentNewLine && predicate('\n', _buffer.Length - offset))
				return _buffer.Length;
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

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i]))
					return i - offset;
			}
			return _appentNewLine && predicate('\n') ? _buffer.Length - offset + 1:  _buffer.Length - offset;
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

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i], i - offset))
					return i - offset;
			}
			return _appentNewLine && predicate('\n', _buffer.Length - offset) ? _buffer.Length - offset + 1:  _buffer.Length - offset;
		}

		///// <summary>
		///// Executes the <paramref name="predicate"/> until it is false starting from specified <paramref name="offset"/>.
		///// </summary>
		///// <param name="predicate"></param>
		///// <param name="offset"></param>
		///// <returns>Offset of the character where the <paramref name="predicate"/> returns false.</returns>
		//public int MatchBack(Func<char, bool> predicate, int offset = 0)
		//{
		//	if (predicate == null)
		//		throw new ArgumentNullException(nameof(predicate));
		//	if (offset < 0)
		//		throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

		//	for (int i = _at - offset - 1; i >= 0; --i)
		//	{
		//		if (!predicate(_buffer[i]))
		//			return i - _at;
		//	}
		//	return 0;
		//}

		/// <summary>
		/// Moves current podition of the <see cref="CharStream"/> forward until the <paramref name="predicate"/> is true.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public int Forward(Func<char, bool> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			for (int i = 0; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i]))
				{
					Forward(i);
					return i;
				}
			}
			Forward(_buffer.Length);
			return _buffer.Length;
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

			for (int i = offset; i < _buffer.Length; ++i)
			{
				if (!predicate(_buffer[i]))
				{
					Forward(i);
					return i;
				}
			}
			Forward(_buffer.Length);
			return _buffer.Length;
		}

		/// <summary>
		/// Determines whether the <see cref="CharStream"/> matches the specified <paramref name="value"/> at the specified <paramref name="offset"/>.
		/// </summary>
		/// <param name="value">The string to compare.</param>
		/// <param name="offset">The search starting position.</param>
		/// <returns>true if the value parameter matches the Stream; otherwise, false.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
		public bool StartsWith(string value, int offset = 0)
		{
			if (value == null || value.Length == 0)
				return false;
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

			if (offset + value.Length > _buffer.Length)
				return false;
			return offset == 0 ? _buffer.StartsWith(value.AsSpan()): _buffer.Slice(offset).StartsWith(value.AsSpan()); //.Substring(offset, value.Length).Equals(value, comparision);
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
		public SyntaxException SyntaxException(string message, int tab)
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
		public SyntaxException SyntaxException(string message, string file, int tab)
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
		public SyntaxException SyntaxException(string message, int position, int tab)
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
		public SyntaxException SyntaxException(string message, string file, int position, int tab)
		{
			var at = GetPosition(position, tab);
			return new SyntaxException(message, file, at.Line + 1, at.Column + 1);
		}

		public CharPosition GetPosition(int tab)
		{
			return GetPosition(_position, tab);
		}

		public CharPosition GetPosition(int position, CharPosition prev, int tab)
		{
			return position < prev.Position ? GetPosition(position, tab):  GetPosition(prev, position - prev.Position, tab);
		}

		public CharPosition GetPosition(int position, int tab)
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

		private (int Line, int Column) OffsetBox(ReadOnlySpan<char> part, int tabSize)
		{
			if (part.Length == 0)
				return default;

			int line = 0;
			int column = 0;
			int i = part.IndexOfAny(NewLineTokens.AsSpan());
			if (i >= 0)
			{
				column = 0;
				do
				{
					++line;
					if (IsNewLineSeq(part, i))
						++i;
					part = part.Slice(i + 1);
				} while ((i = part.IndexOfAny(NewLineTokens.AsSpan())) >= 0);
			}

			if (tabSize > 1)
			{
				i = part.IndexOf('\t');
				if (i >= 0)
				{
					do
					{
						column += i;
						column = column - (column % tabSize) + tabSize;
						part = part.Slice(i + 1);
					} while ((i = part.IndexOf('\t')) >= 0);
				}
			}
			return (line, column + part.Length);
		}
		private static readonly string NewLineTokens = "\n\r\f\u2028\u2029";

		private static bool IsNewLineSeq(ReadOnlySpan<char> part, int index)
		{
			int index1 = index + 1;
			return part.Length < index1 && (part[index] == '\n' && part[index1] == '\r' || part[index] == '\r' && part[index1] == '\n');
		}

		/// <summary>
		/// Displays current position and current 120 characters of the stream.
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{_position}: {Strings.Ellipsis(Strings.EscapeCsString(Substring(0, 120)), 120, "...\"")}";
	}
}
