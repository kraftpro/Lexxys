using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con.Js
{
	public static class Strings2
	{
		public static int EscapeCsString(Span<byte> buffer, ReadOnlySpan<byte> value, byte marker = (byte)'"')
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (buffer.Length == 0)
				return 0;

			int j = 0;
			if (marker != '\0')
			{
				buffer[0] = marker;
				++j;
			}
			for (int i = 0; i < value.Length; i++)
			{
				byte c = value[i];
				if (c < ' ' || c == 127)
				{
					if (j + 1 >= buffer.Length)
						return -j;
					buffer[j++] = (byte)'\\';
					switch (c)
					{
						case (byte)'\n':
							buffer[j++] = (byte)'n';
							break;
						case (byte)'\r':
							buffer[j++] = (byte)'r';
							break;
						case (byte)'\t':
							buffer[j++] = (byte)'t';
							break;
						case (byte)'\f':
							buffer[j++] = (byte)'f';
							break;
						case (byte)'\v':
							buffer[j++] = (byte)'v';
							break;
						case (byte)'\a':
							buffer[j++] = (byte)'a';
							break;
						case (byte)'\b':
							buffer[j++] = (byte)'b';
							break;
						case (byte)'\0':
							buffer[j++] = (byte)'0';
							break;
						default:
							if (value.Length > i + 1 && IsHex(value[i + 1]))
							{
								if (j + 4 >= buffer.Length)
									return -(j - 1);
								buffer[j++] = (byte)'u';
								buffer[j++] = (byte)'0';
								buffer[j++] = (byte)'0';
								buffer[j++] = __hexB[(c & 0xF0) >> 4];
								buffer[j++] = __hexB[c & 0xF];
							}
							else
							{
								if (j + 2 >= buffer.Length)
									return -(j - 1);
								buffer[j++] = (byte)'x';
								buffer[j++] = __hexB[(c & 0xF0) >> 4];
								buffer[j++] = __hexB[c & 0xF];
							}
							break;
					}
				}
				else if (c >= (byte)0x80)
				{
					int len = Utf8.SeqLength(c);
					if (j + len >= buffer.Length || i + len >= value.Length)
						return -j;
					Utf8.ReadChar3(value, out var u);
					if (u >= 0xD800)
					{
						if (j + 5 > buffer.Length)
							return -j;
						buffer[j++] = (byte)'\\';
						buffer[j++] = (byte)'u';
						buffer[j++] = __hexB[(u & 0xF000) >> 12];
						buffer[j++] = __hexB[(u & 0xF00) >> 8];
						buffer[j++] = __hexB[(u & 0xF0) >> 4];
						buffer[j++] = __hexB[u & 0xF];
					}
					else
					{
						buffer[j++] = value[i];
						if (len > 1)
						{
							buffer[j++] = value[i];
							if (len > 2)
							{
								buffer[j++] = value[i];
								if (len > 3)
								{
									buffer[j++] = value[i];
								}
							}
						}
					}
				}
				else if (c == marker)
				{
					if (j + 1 >= buffer.Length)
						return -j;
					buffer[j++] = (byte)'\\';
					buffer[j++] = marker;
				}
				else if (c == (byte)'\\')
				{
					if (j + 1 >= buffer.Length)
						return -j;
					buffer[j++] = (byte)'\\';
					buffer[j++] = (byte)'\\';
				}
				else
				{
					if (j >= buffer.Length)
						return -j;
					buffer[j++] = c;
				}
			}
			if (marker != '\0')
			{
				if (j >= buffer.Length)
					return -j;
				buffer[++j] = marker;
			}
			return j;

			static bool IsHex(byte c) => c >= (byte)'0' && (c <= (byte)'9' || (c >= (byte)'A' && (c <= (byte)'F' || c >= (byte)'a' && c <= (byte)'f')));
		}

		public static int EscapeCsString(Span<byte> buffer, ReadOnlySpan<char> value, char marker = '"')
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (buffer.Length == 0)
				return 0;

			int j = 0;
			if (marker != '\0')
			{
				j = Utf8.Append(buffer, marker);
				if (j == 0)
					return 0;
			}
			for (int i = 0; i < value.Length; i++)
			{
				char c = value[i];
				if (c < ' ' || c == 127)
				{
					if (j + 1 >= buffer.Length)
						return -j;
					buffer[j++] = (byte)'\\';
					switch (c)
					{
						case '\n':
							buffer[j++] = (byte)'n';
							break;
						case '\r':
							buffer[j++] = (byte)'r';
							break;
						case '\t':
							buffer[j++] = (byte)'t';
							break;
						case '\f':
							buffer[j++] = (byte)'f';
							break;
						case '\v':
							buffer[j++] = (byte)'v';
							break;
						case '\a':
							buffer[j++] = (byte)'a';
							break;
						case '\b':
							buffer[j++] = (byte)'b';
							break;
						case '\0':
							buffer[j++] = (byte)'0';
							break;
						default:
							if (value.Length > i + 1 && IsHex(value[i + 1]))
							{
								if (j + 4 >= buffer.Length)
									return -(j - 1);
								buffer[j++] = (byte)'u';
								buffer[j++] = (byte)'0';
								buffer[j++] = (byte)'0';
								buffer[j++] = __hexB[(c & 0xF0) >> 4];
								buffer[j++] = __hexB[c & 0xF];
							}
							else
							{
								if (j + 2 >= buffer.Length)
									return -(j - 1);
								buffer[j++] = (byte)'x';
								buffer[j++] = __hexB[(c & 0xF0) >> 4];
								buffer[j++] = __hexB[c & 0xF];
							}
							break;
					}
				}
				else if (c >= '\x80')
				{
					if (c >= 0xD800)
					{
						if (j + 5 > buffer.Length)
							return -j;
						buffer[j++] = (byte)'\\';
						buffer[j++] = (byte)'u';
						buffer[j++] = __hexB[(c & 0xF000) >> 12];
						buffer[j++] = __hexB[(c & 0xF00) >> 8];
						buffer[j++] = __hexB[(c & 0xF0) >> 4];
						buffer[j++] = __hexB[c & 0xF];
					}
					else
					{
						int k = Utf8.Append(buffer.Slice(j), c);
						if (k == 0)
							return -j;
						j += k;
					}
				}
				else if (c == marker)
				{
					if (j + 1 >= buffer.Length)
						return -j;
					buffer[j++] = (byte)'\\';
					int k = Utf8.Append(buffer.Slice(j), marker);
					if (k == 0)
						return -j;
					j += k;
				}
				else if (c == (byte)'\\')
				{
					if (j + 1 >= buffer.Length)
						return -j;
					buffer[j++] = (byte)'\\';
					buffer[j++] = (byte)'\\';
				}
				else
				{
					if (j >= buffer.Length)
						return -j;
					buffer[j++] = (byte)c;
				}
			}
			if (marker != '\0')
			{
				int k = Utf8.Append(buffer.Slice(j), marker);
				if (k == 0)
					return -j;
				j += k;
			}
			return j;

			static bool IsHex(char c) => c >= '0' && (c <= '9' || (c >= 'A' && (c <= 'F' || c >= 'a' && c <= 'f')));
		}

		private static readonly byte[] __hexB = { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f' };
	}
}
