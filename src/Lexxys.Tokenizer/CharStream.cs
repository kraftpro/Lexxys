// Lexxys Infrastructural library.
// file: CharStream.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Buffers;
using System.Text.RegularExpressions;

namespace Lexxys.Tokenizer;

/// <summary>
/// Represents a characters stream, optimized for parsing purposes.
/// </summary>
public ref struct CharStream
{
	public const char BofMarker = '\0';
	public const char EofMarker = '\uFFFF';
	public const char NewLineMarker = '\n';
	public const int DefaultTabSize = 4;

	private readonly ReadOnlySpan<char> _start;		// Actual buffer
	private ReadOnlySpan<char> _buffer; // := _start.Slice(Position)

	/// <summary>
	/// Creates a new CharStream.
	/// </summary>
	/// <param name="text">Content of the stream</param>
	/// <param name="tabSize">The tab size. If 0 or less, defaults to the default tab size.</param>
	/// <exception cref="System.ArgumentNullException"><paramref name="text"/>is null</exception>
	public CharStream(ReadOnlySpan<char> text, int tabSize = 0)
	{
		_buffer = _start = text;
		TabSize = tabSize > 0 ? tabSize: DefaultTabSize;
	}

	/// <summary>
	/// Creates a new CharStream.
	/// </summary>
	/// <param name="text">Content of the stream</param>
	/// <param name="tabSize">Tab size (default 4)</param>
	/// <exception cref="System.ArgumentNullException"><paramref name="text"/>is null</exception>
	public CharStream(string text, int tabSize = 0)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		_buffer = _start = text.AsSpan();
		TabSize = tabSize > 0 ? tabSize: DefaultTabSize;
	}

	/// <summary>
	/// Initializes a new instance of the CharStream class by reading all content from the specified text reader.
	/// </summary>
	/// <param name="stream">The text reader to read from.</param>
	/// <param name="tabSize">The tab size. If 0 or less, defaults to the default tab size.</param>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
	public CharStream(TextReader stream, int tabSize = 0)
	{
		if (stream == null)
			throw new ArgumentNullException(nameof(stream));

		_buffer = _start = stream.ReadToEnd().AsSpan();
		TabSize = tabSize > 0 ? tabSize: DefaultTabSize;
	}

	/// <summary>
	/// Gets the character at a specified relative character position in the current <see cref="CharStream"/> object.
	/// </summary>
	/// <param name="index">A relative character position in the <see cref="CharStream"/>.</param>
	/// <returns>
	///		A Unicode character if absolute position within the <see cref="CharStream"/> buffer boundaries;
	///		<see cref="BofMarker"/> if absolute position less then zero;
	///		<see cref="EofMarker"/> if absolute position greater or equal to the length of <see cref="CharStream"/>.
	///	</returns>
	public readonly char this[int index] => (uint)index < (uint)_buffer.Length ? _buffer[index]: index < 0 ? BofMarker: EofMarker;

	/// <summary>
	/// Gets number of characters in the current <see cref="CharStream"/>.
	/// </summary>
	public readonly int Length => _buffer.Length;

	public readonly int Position => _start.Length - _buffer.Length;

	public readonly int Capacity => _start.Length;
	
	public readonly int TabSize { get; }

	/// <summary>
	/// It indicates that the end of the stream has been encountered.
	/// </summary>
	public readonly bool Eof => _buffer.Length == 0;

	/// <summary>
	/// Sets the stream position.
	/// </summary>
	/// <param name="position">New stream position</param>
	public void Move(int position) => _buffer = position <= 0 ? _start: _start.Slice(Math.Min(position, _start.Length));

	/// <summary>
	/// Repositions the stream pointer to the beginning of the stream.
	/// </summary>
	public void Rewind() => _buffer = _start;

	/// <summary>
	/// Moves forward by the specified length.
	/// </summary>
	/// <param name="length">Number of positions to move forward (may be negative).</param>
	public void Forward(int length) => Move(Position + length);

	/// <summary>
	/// Forms a relative slice of the buffer starting at a specified index for a specified length.
	/// </summary>
	/// <param name="start">The relative index at which to begin the slice.</param>
	/// <param name="length">The desired length for the slice.</param>
	/// <returns>A read-only span that consists of <paramref name="length"/> characters from the buffer starting at <paramref
	/// name="start"/>.</returns>
	public readonly ReadOnlySpan<char> Slice(int start, int length)
		=> _buffer.Slice(start, length);

	/// <summary>
	/// Forms a relative slice out of the current span starting at a specified index.
	/// </summary>
	/// <param name="start">The relative index at which to begin the slice.</param>
	/// <returns>A span that consists of all elements from <paramref name="start"/> to the end of the span.</returns>
	public readonly ReadOnlySpan<char> Slice(int start)
		=> _buffer.Slice(start);

	/// <summary>
	/// Returns an absolute slice of the span starting at the specified position with the specified length.
	/// </summary>
	/// <param name="start">The absolute zero-based starting position of the chunk.</param>
	/// <param name="length">The number of characters in the chunk.</param>
	/// <returns>A read-only span representing the specified chunk.</returns>
	public readonly ReadOnlySpan<char> Chunk(int start, int length)
		=> _start.Slice(start, length);

	/// <summary>
	/// Creates a <see cref="LexicalToken"/> from the stream buffer fragment and move current stream position forward on <paramref name="length"/> characters.
	/// </summary>
	/// <param name="tokenType">Type of the created <see cref="LexicalToken"/></param>
	/// <param name="length">Number of characters to copy from current position of stream into the content of new <see cref="LexicalToken"/>.</param>
	/// <returns>New <see cref="LexicalToken"/> or null if the stream has been at the EOF state.</returns>
	public LexicalToken Token(LexicalTokenType tokenType, int length)
	{
		var token = new LexicalToken(tokenType, Position, length);
		Forward(length);
		return token;
	}

	public LexicalToken Token(LexicalTokenType tokenType, int length, LexicalToken.Getter getter)
	{
		var token = new LexicalToken(tokenType, Position, length, getter);
		Forward(length);
		return token;
	}

	public LexicalToken Token(LexicalTokenType tokenType, int length, object value)
	{
		var token = new LexicalToken(tokenType, Position, length, (_,_) => value);
		Forward(length);
		return token;
	}

	/// <summary>
	/// Gets a substring from the stream buffer.
	/// </summary>
	/// <param name="start">Offset from current position in the stream</param>
	/// <param name="length">Length of the substring</param>
	/// <returns></returns>
	/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="start"/> is less then zero.</exception>
	/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="length"/> is less then zero.</exception>
	public readonly string Substring(int start, int length)
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
	public readonly int IndexOf(char value) => _buffer.IndexOf(value);

	/// <summary>
	/// Reports the index of the first occurrence of the specified character in this Stream.
	/// </summary>
	/// <param name="value">Character to seek.</param>
	/// <param name="offset">The search starting position.</param>
	/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
	/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
	public readonly int IndexOf(char value, int offset)
	{
		if (offset < 0)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
		if (offset == 0)
			return _buffer.IndexOf(value);
		if (offset >= _buffer.Length)
			return -1;
		int k = _buffer.Slice(offset).IndexOf(value);
		return k < 0 ? k: k + offset;
	}

	/// <summary>
	/// Reports the index of the first occurrence of the specified string in this Stream.
	/// </summary>
	/// <param name="value">String to seek.</param>
	/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
	public readonly int IndexOf(ReadOnlySpan<char> value) => _buffer.IndexOf(value);

	/// <summary>
	/// Reports the index of the first occurrence of the specified string in this Stream.
	/// </summary>
	/// <param name="value">String to seek.</param>
	/// <param name="offset">The search starting position.</param>
	/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
	/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
	public readonly int IndexOf(ReadOnlySpan<char> value, int offset)
	{
		if (offset < 0)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
		if (offset == 0)
			return _buffer.IndexOf(value);
		if (offset >= _buffer.Length)
			return -1;
		int k = _buffer.Slice(offset).IndexOf(value);
		return k < 0 ? k: k + offset;
	}

	/// <summary>
	/// Reports the index of the first occurrence of the specified string in this Stream.
	/// </summary>
	/// <param name="value">String to seek</param>
	/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
	public readonly int IndexOf(string? value) => String.IsNullOrEmpty(value) ? -1: _buffer.IndexOf(value.AsSpan());

	public readonly int IndexOf(string? value, int offset) => String.IsNullOrEmpty(value) ? -1: IndexOf(value.AsSpan(), offset);

	/// <summary>
	/// Reports the index of the first occurrence in this Stream of any character in a specified array of characters.
	/// </summary>
	/// <param name="any">A character array containing one or more characters to seek.</param>
	/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
	/// <exception cref="System.ArgumentNullException"><paramref name="any"/> is null.</exception>
	public readonly int IndexOfAny(ReadOnlySpan<char> any) => _buffer.IndexOfAny(any);

#if NET8_0_OR_GREATER
	public readonly int IndexOfAny(SearchValues<char> any) => _buffer.IndexOfAny(any);

	public readonly int IndexOfAnyExcept(SearchValues<char> any) => _buffer.IndexOfAnyExcept(any);
#endif

	/// <summary>
	/// Reports the index of the first occurrence in this Stream of any character in a specified array of characters.
	/// </summary>
	/// <param name="any">A character array containing one or more characters to seek.</param>
	/// <param name="offset">The search starting position.</param>
	/// <returns>The zero-based index position of value if that character is found, or -1 it is not.</returns>
	/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
	public readonly int IndexOfAny(char[]? any, int offset) => IndexOfAny(any.AsSpan(), offset);

	public readonly int IndexOfAny(ReadOnlySpan<char> any, int offset)
	{
		if (offset < 0)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
		if (offset == 0)
			return _buffer.IndexOfAny(any);
		var i = _buffer.Slice(offset).IndexOfAny(any);
		return i < 0 ? -1: i + offset;
	}

	/// <summary>
	/// Reports the index of the first symbol in this Stream for which the specified <paramref name="predicate"/> returns true.
	/// </summary>
	/// <param name="predicate">A function that defines the conditions of the element to search for.</param>
	/// <param name="offset">The search starting position.</param>
	/// <returns>Offset of the character where the <paramref name="predicate"/> returns false.</returns>
	public readonly int IndexOf(Func<char, bool> predicate, int offset = 0)
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
	/// <param name="predicate">A function that defines the conditions of the element to search for.</param>
	/// <param name="offset">The search starting position.</param>
	/// <returns>Offset of the character where the <paramref name="predicate"/> returns false.</returns>
	public readonly int IndexOf(Func<char, int, bool> predicate, int offset = 0)
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
	/// Determines the size of the newline sequence at the specified offset.
	/// </summary>
	/// <param name="offset">The zero-based position in the buffer to check for a newline sequence.</param>
	/// <returns>The size of the newline sequence: 2 for CRLF, 1 for LF or CR, or 0 if no newline character is present.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is less than zero.</exception>
	public readonly int NewLineSize(int offset = 0)
	{
		if (offset < 0)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

		if (offset >= _buffer.Length)
			return 0;

		return _buffer[offset] switch
		{
			LF => 1,
			CR => offset + 1 < _buffer.Length && _buffer[offset + 1] == LF ? 2: 1,
			_ => 0
		};
	}
	private const char CR = '\r';
	private const char LF = '\n';

	/// <summary>
	/// Returns number of continuous characters satisfied to the specified <paramref name="predicate"/>.
	/// </summary>
	/// <param name="predicate">A function that defines the conditions of the element to match.</param>
	/// <param name="offset">The search starting position.</param>
	/// <returns>The number of continuous characters that satisfy the specified <paramref name="predicate"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is less than zero.</exception>
	public readonly int Match(Func<char, bool> predicate, int offset = 0)
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
	/// <param name="predicate">A function that defines the conditions of the element to match.</param>
	/// <param name="offset">The search starting position.</param>
	/// <returns>The number of continuous characters that satisfy the specified <paramref name="predicate"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is less than zero.</exception>
	public readonly int Match(Func<char, int, bool> predicate, int offset = 0)
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

#if NET7_0_OR_GREATER

	public readonly ValueMatch Match(Regex regex, int offset = 0)
	{
		foreach(var match in regex.EnumerateMatches(_buffer, offset))
			return match;
		return default;
	}

    public readonly Regex.ValueMatchEnumerator MatchAll(Regex regex, int offset = 0)
    {
		return regex.EnumerateMatches(_buffer, offset);
    }

#endif

    /// <summary>
    /// Moves current position of the <see cref="CharStream"/> forward until the <paramref name="predicate"/> is true.
    /// </summary>
    /// <param name="predicate">A function that defines the conditions of the element to match.</param>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public void Forward(Func<char, bool> predicate)
	{
		if (predicate == null)
			throw new ArgumentNullException(nameof(predicate));

		for (int i = 0; i < _buffer.Length; ++i)
		{
			if (!predicate(_buffer[i]))
			{
				_buffer = _buffer.Slice(i);
				return;
			}
		}
		_buffer = _start.Slice(_start.Length);
	}

	/// <summary>
	/// Moves current position of the <see cref="CharStream"/> forward until the <paramref name="predicate"/> is true starting from specified <paramref name="offset"/>.
	/// </summary>
	/// <param name="offset">The search starting position.</param>
	/// <param name="predicate">A function that defines the conditions of the element to match.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is less than zero.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
	public void Forward(int offset, Func<char, bool> predicate)
	{
		if (predicate == null)
			throw new ArgumentNullException(nameof(predicate));
		if (offset < 0)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

		for (int i = offset; i < _buffer.Length; ++i)
		{
			if (!predicate(_buffer[i]))
			{
				_buffer = _buffer.Slice(i);
				return;
			}
		}
		_buffer = _start.Slice(_start.Length);
	}

	/// <summary>
	/// Determines whether the <see cref="CharStream"/> matches the specified <paramref name="value"/> at the specified <paramref name="offset"/>.
	/// </summary>
	/// <param name="value">The string to compare.</param>
	/// <param name="offset">The search starting position.</param>
	/// <returns>true if the value parameter matches the Stream; otherwise, false.</returns>
	/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is less then zero.</exception>
	public readonly bool StartsWith(string value, int offset = 0)
	{
		if (String.IsNullOrEmpty(value))
			return false;
		if (offset < 0)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

		if (offset + value.Length > _buffer.Length)
			return value.Length == 0;
		return offset == 0 ? _buffer.StartsWith(value.AsSpan()): _buffer.Slice(offset).StartsWith(value.AsSpan());
	}

	/// <summary>
	/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message and current stream position.
	/// </summary>
	/// <param name="message">Exception message.</param>
	/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
	public SyntaxException SyntaxException(string? message)
	{
		var at = GetCharPosition();
		return new SyntaxException(message, null, at.Line + 1, at.Column + 1);
	}

	public string FormatSyntaxException(string? message)
	{
		var at = GetCharPosition();
		return $"{message} at line {at.Line + 1}, column {at.Column + 1}";
	}

	/// <summary>
	/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message, file info and current stream position.
	/// </summary>
	/// <param name="message">Exception message.</param>
	/// <param name="file">File info to include into the result</param>
	/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
	public SyntaxException SyntaxException(string? message, string? file)
	{
		var at = GetCharPosition();
		return new SyntaxException(message, file, at.Line + 1, at.Column + 1);
	}

	/// <summary>
	/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message and specified position in the stream.
	/// </summary>
	/// <param name="message">Exception message.</param>
	/// <param name="position">Position in the Stream.</param>
	/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
	public SyntaxException SyntaxException(string? message, int position)
	{
		var at = GetCharPosition(position);
		return new SyntaxException(message, null, at.Line + 1, at.Column + 1);
	}

	/// <summary>
	/// Creates a new <see cref="T:SyntaxException"/> object with specified exception message, file info and specified position in the stream.
	/// </summary>
	/// <param name="message">Exception message.</param>
	/// <param name="file">File info to include into the result</param>
	/// <param name="position">Position in the Stream.</param>
	/// <returns>A new <see cref="T:SyntaxException"/> object.</returns>
	public SyntaxException SyntaxException(string? message, string? file, int position)
	{
		var at = GetCharPosition(position);
		return new SyntaxException(message, file, at.Line + 1, at.Column + 1);
	}

	public CharPosition GetCharPosition() => GetCharPosition(Position);

	public CharPosition GetCharPosition(int position, CharPosition prev)
		=> position < prev.Position ? GetCharPosition(position): GetCharPosition(prev, position - prev.Position);

	public CharPosition GetCharPosition(int position)
	{
		return _last = _last.Position <= position ?
			_last.Next(_start, position - _last.Position, TabSize):
			new CharPosition(_start, position, TabSize);
	}
	private CharPosition _last = default;

	private readonly CharPosition GetCharPosition(CharPosition at, int offset) => at.Next(_start, offset, TabSize);

	/// <summary>
	/// Displays current position and current 120 characters of the stream.
	/// </summary>
	/// <returns></returns>
	public readonly override string ToString()
	{
		var s = Regex.Replace(Substring(0, 121), @"\p{C}", m => m.Value[0] switch
		{
			'\r' => "\\r",
			'\n' => "\\n",
			'\t' => "\\t",
			var c => $"\\u{(int)c:X4}"
		});
		return s.Length > 120 ? $"{Position}: {s.Substring(0, 117)}...": $"{Position}: {s}";
	}
}
