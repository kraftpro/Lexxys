using System.Collections;
using System.Numerics;

namespace Lexxys;

public abstract class DumpWriterCore(DumpWriterOptions? options = null): IDumpWriter
{
	protected DumpWriterOptions _options = options ?? new DumpWriterOptions();
	protected readonly Stack<(char Ch, string? Name)> _objectStack = [];
	protected ConditionalWeakTable<object, string> _observed = new();
	protected string? _lastName;
	protected bool _continues;
	protected int _written;

	public int MaxDepth => _options.MaxDepth;
	public int MaxLength => _options.MaxLength;
	public int StringMaxLength => _options.StringMaxLength;
	public int BinaryMaxLength => _options.BinaryMaxLength;
	public int ArrayMaxLength => _options.ArrayMaxLength;
	public string NullValue => _options.NullValue;
	public char FieldSeparator => _options.FieldSeparator;
	public char EqualChar => _options.EqualChar;
	public bool Compact => _options.Compact;
	public bool FormatIndentation => _options.FormatIndentation;
	public string IndentationString => _options.IndentationString;
	public bool IncludeObjectType => _options.IncludeObjectType;
	public string EllipsisString => _options.EllipsisString;
	public bool Base64Binary => _options.Base64Binary;
	public ObjectDumpLevel Level => _options.Level;
	public NamingCaseRule NamingRule => _options.NamingRule;

	public int Depth => _objectStack.Count;
	public int Length => _written;

	public virtual string Type => "text";

	protected abstract void Text(ReadOnlySpan<char> value);

	IDumpWriter IDumpWriter.Write<T>(string? name, T value)
		=> Write(name != null && NamingRule.HasFlag(NamingCaseRule.Force) ? Strings.ToNamingRule(name, NamingRule): name, value);

	IDumpWriter IDumpWriter.Begin(char type, string? name)
		=> Begin(type, name != null && NamingRule.HasFlag(NamingCaseRule.Force) ? Strings.ToNamingRule(name, NamingRule): name);

	public virtual IDumpWriter Write<T>(string? name, T value)
	{
		if (Depth > MaxDepth) return this;

		if (!WritePrimitive(name, value))
			WriteObject(name, value);
		_continues = true;
		return this;
	}

	public virtual IDumpWriter Begin(char type = default, string? name = null)
	{
		if (type == default)
			type = '{';
		var ch = type switch
		{
			'{' => '}',
			'[' => ']',
			'(' => ')',
			'<' => '>',
			_ => throw new ArgumentOutOfRangeException(nameof(type), "Type must be one of the following characters: {, [, (, or <.")
		};
		if (Depth > MaxDepth)
		{
			PushState(ch);
			return this;
		}

		WriteNameEqual(name);
		PushState(ch);
		WriteText(type);
		if (Depth > MaxDepth)
		{
			WriteText(EllipsisString);
			WriteText(ch);
		}
		else if (FormatIndentation)
		{
			PrintIndentation();
		}
		_continues = false;
		return this;
	}

	protected void PushState(char ch)
	{
		_objectStack.Push((ch, _objectStack.Count == 0 ? _lastName: _objectStack.Peek().Name + "." + _lastName));
	}

	public virtual IDumpWriter End()
	{
		if (_objectStack.Count == 0)
			throw new InvalidOperationException("No object or array to end.");

		var ch = _objectStack.Pop().Ch;
		if (Depth >= MaxDepth)
			return this;
		if (FormatIndentation)
			PrintIndentation();
		WriteText(ch);
		return this;
	}

	protected void Comma(char comma)
	{
		if (!_continues)
			return;

		WriteText(comma);
		if (FormatIndentation)
			PrintIndentation();
		else if (!Compact)
			WriteText(' ');
		_continues = false;
	}

	protected void PrintIndentation()
	{
		WriteText('\n');
		int depth = Depth;
		for (int i = 0; i < depth; i++)
		{
			WriteText(IndentationString);
		}
	}

	protected virtual void WriteNameEqual(string? name)
	{
		if (Depth > MaxDepth) return;

		char comma = _objectStack.Count == 0 || _objectStack.Peek().Ch == '}' ? FieldSeparator: ',';
		Comma(comma);
		if (name == null) return;
		_lastName = name;
		WriteName(name);
		WriteEqual();
	}

	protected virtual void WriteEqual()
	{
		if (!Compact && EqualChar != ':')
			WriteText(' ');
		WriteText(EqualChar);
		if (!Compact)
			WriteText(' ');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void WriteText(char value)
	{
#if NET
		Span<char> span = new Span<char>(ref value);
#else
		Span<char> span = stackalloc char[1] { value };
#endif
		WriteText(span);
	}

	protected void WriteText(ReadOnlySpan<char> text)
	{
		if (_written + text.Length + EllipsisString.Length <= MaxLength)
		{
			Text(text);
			_written += text.Length;
		}
		else if (_written < MaxLength)
		{
			Text(text.Slice(0, Math.Max(0, MaxLength - _written - EllipsisString.Length)));
			Text(EllipsisString);
			_written = MaxLength;
		}
	}

	protected bool IsPrimitive<T>(T value)
	{
		return value switch
		{
			null => true,
			string => true,
			char => true,
			bool => true,
			byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or nint or BigInteger => true,
			Complex => true,
			DateTime => true,
			DateTimeOffset => true,
			TimeSpan => true,
			Guid => true,
			IEnumerable<byte> => true,
			System.Type => true,
			IDumpValue => true,
			_ => value.GetType().IsEnum || GetObserved(value) is not null,
		};
	}

	protected virtual void WritePrimitiveBegin(string? name) => WriteNameEqual(name);

	protected virtual void WritePrimitiveEnd(string? name) { }


	protected virtual bool WritePrimitive<T>(string? name, T value)
	{
		if (!IsPrimitive(value))
			return false;

		WritePrimitiveBegin(name);
		WritePrimitiveValue(value);
		WritePrimitiveEnd(name);
		return true;
	}

	protected bool WritePrimitiveValue<T>(T value)
	{
		switch (value)
		{
			case null:
				WriteText(NullValue);
				return true;
			case string s:
				WriteString(s);
				return true;
			case char c:
				WriteChar(c);
				return true;
			case bool b:
				WriteBoolean(b);
				return true;
			case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or nint or BigInteger:
				WriteNumeric(value);
				return true;
			case Complex complex:
				WriteComplexNumeric(complex);
				return true;
			case DateTime dt:
				if (dt == DateTime.MinValue || dt == DateTime.MaxValue)
					dt = dt.ToUniversalTime();
				WriteDateTime(dt);
				return true;
			case DateTimeOffset dto:
				WriteDateTime(dto);
				return true;
			case TimeSpan ts:
				WriteTimeSpan(ts);
				return true;
			case Guid g:
				WriteGuid(g);
				return true;
			case IEnumerable<byte> bytes:
				WriteBinary(bytes);
				return true;
			case Type type:
				WriteString(type.GetTypeName());
				return true;
			default:
				break;
		}
		if (value.GetType().IsEnum)
		{
			WriteEnum(value);
			return true;
		}
		var reference = GetObserved(value);
		if (reference is not null)
		{
			WriteReference(reference);
			return true;
		}
		if (value is IDumpValue dv)
		{
			dv.DumpContent(this);
			return true;
		}
		return false;
	}

	protected virtual void WriteName(ReadOnlySpan<char> name) => WriteText(name);

	protected virtual void WriteEnum<T>(T value) => WriteText(value!.ToString());

	protected virtual void WriteDateTime(DateTimeOffset value)
	{
		if (value.Offset == TimeSpan.Zero)
			WriteText(value.UtcDateTime.ToString("o"));
		else
			WriteText(value.ToString("o"));
	}

	protected virtual void WriteTimeSpan(TimeSpan value) => WriteText(value.ToString("c"));

	protected virtual void WriteGuid(Guid value) => WriteText(value.ToString());

	protected virtual void WriteBoolean(bool value) => WriteText(value ? "true": "false");

	protected virtual void WriteString(ReadOnlySpan<char> value)
	{
		WriteText('"');
		var s = value.Length > StringMaxLength ? value.Slice(0, StringMaxLength): value;

		foreach (var c in s)
		{
			if (c == '"')
				WriteText("\\\"");
			else
				EscapeChar(c);
		}
		if (value.Length > StringMaxLength)
			WriteText(EllipsisString);
		WriteText('"');
	}

	protected virtual void WriteChar(char value)
	{
		WriteText('\'');
		if (value == '\'')
			WriteText("\\'");
		else
			EscapeChar(value);
		WriteText('\'');
	}

	protected virtual void WriteNumeric<T>(T value) => WriteText(value!.ToString());

	protected virtual void WriteComplexNumeric(Complex complex) => WriteText(complex.Imaginary < 0 ? $"{complex.Real}{complex.Imaginary}i": $"{complex.Real}+{complex.Imaginary}i");

	protected virtual void WriteReference(string reference)
	{
		// WriteText($"($ref=#{reference})");
		WriteText("($ref");
		WriteEqual();
		WriteText('#');
		WriteText(reference);
		WriteText(')');
	}

	protected virtual void WriteBinary(IEnumerable<byte> bytes)
	{
		if (_options.Base64Binary)
			WriteBase64(bytes);
		else
			WriteHexadecimal(bytes);
	}

	private void WriteHexadecimal(IEnumerable<byte> bytes)
	{
		WriteText("0x");
		Span<char> buffer = stackalloc char[1024];
		int i = 0;
		int count = 0;
		foreach (var b in bytes)
		{
			if (++count > BinaryMaxLength)
				break;
			buffer[i++] = Hex(b >> 4);
			buffer[i++] = Hex(b & 15);
			if (i == buffer.Length)
			{
				i = 0;
				WriteText(buffer);
			}
		}
		if (i > 0)
			WriteText(buffer.Slice(0, i));
		if (count > BinaryMaxLength)
			WriteText(EllipsisString);

		static char Hex(int value) => value < 10 ? (char)('0' + value): (char)('A' + value - 10);
	}

	protected void WriteBase64(IEnumerable<byte> bytes)
	{
		int count = 0;
#if NET
		Span<byte> bits = stackalloc byte[3];
		Span<char> chars = stackalloc char[4];
#else
		var bits = new byte[3];
		var chars = new char[4];
#endif
		int bufferCount = 0;
		foreach (var b in bytes)
		{
			if (++count > BinaryMaxLength)
				break;
			bits[bufferCount++] = b;
			if (bufferCount == 3)
			{
#if NET
				Convert.TryToBase64Chars(bits, chars, out _, Base64FormattingOptions.None);
#else
				Convert.ToBase64CharArray(bits, 0, 3, chars, 0, Base64FormattingOptions.None);
#endif
				WriteText(chars);
				bufferCount = 0;
			}
		}
		if (bufferCount > 0)
		{
#if NET
			Convert.TryToBase64Chars(bits[0..bufferCount], chars, out var n, Base64FormattingOptions.None);
			WriteText(chars[..n]);
#else
			var n = Convert.ToBase64CharArray(bits, 0, bufferCount, chars, 0, Base64FormattingOptions.None);
			WriteText(chars.AsSpan(0, n));
#endif
		}
		if (count > BinaryMaxLength)
			WriteText(EllipsisString);
	}

	private void EscapeChar(char value)
	{
		switch (value)
		{
			case '\\':
				WriteText("\\\\");
				break;
			case '\b':
				WriteText("\\b");
				break;
			case '\f':
				WriteText("\\f");
				break;
			case '\n':
				WriteText("\\n");
				break;
			case '\r':
				WriteText("\\r");
				break;
			case '\t':
				WriteText("\\t");
				break;
			default:
				if (Char.IsControl(value))
				{
					WriteText("\\u");
					WriteText(((int)value).ToString("x4"));
				}
				else
				{
					WriteText(value);
				}
				break;
		}
	}

	protected virtual void AddObserved(string? name, object value)
	{
		if (value is null) throw new ArgumentNullException(nameof(value));
		string refName = _objectStack.Count > 0 ? _objectStack.Peek().Name ?? String.Empty: String.Empty;
		if (name != null)
			refName += refName.Length > 0 ? "." + name: name;
		if (!_observed.TryGetValue(value, out _))
			_observed.Add(value, refName);
	}

	protected virtual string? GetObserved<T>(T value)
	{
		return value is not null && typeof(T).IsClass && _observed.TryGetValue(value, out var reference) ? reference: null;
	}

	protected virtual void WriteObject<T>(string? name, T value)
	{
		if (value is null) throw new ArgumentNullException(nameof(value));

		AddObserved(name, value);

		IDump? idump = value as IDump;
		IDictionary? dictionary = idump is null ? value as IDictionary: null;
		IEnumerable? enumerable = dictionary is null ? value as IEnumerable: null;
		Type? pairsCollectionType = enumerable is null ? null:
			value.GetType().GetInterfaces().FirstOrDefault(o =>
				o.IsGenericType &&
				o.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
				o.GetGenericArguments().Any(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
				);

		char type = enumerable is null || pairsCollectionType is not null ? '{': '[';
		Begin(type, name);

		if (Depth > MaxDepth)
		{
			End();
			return;
		}


		if (IncludeObjectType)
			WriteObjectType(value.GetType());

		if (idump is not null)
		{
			idump.DumpContent(this);
		}
		else if (dictionary != null)
		{
			foreach (DictionaryEntry entry in dictionary)
			{
				var key = Convert.ToString(entry.Key);
				if (key != null && NamingRule != 0 && !NamingRule.HasFlag(NamingCaseRule.Force))
					key = Strings.ToNamingRule(key, NamingRule);
				Write(key, entry.Value);
			}
		}
		else if (enumerable is null)
		{
			var objAcc = Factory.CreateAccessor(value, includeNonPublic: Level >= ObjectDumpLevel.Verbose);
			var members = new List<(string Name, object? Value)>();
			foreach (var propName in objAcc.Properties)
			{
				if (objAcc.TryGetValue(propName, out var propValue))
					members.Add((propName, propValue));
			}
			OrderProperties(members);
			foreach (var item in members)
			{
				var key = NamingRule == 0 || NamingRule.HasFlag(NamingCaseRule.Force) ? item.Name: Strings.ToNamingRule(item.Name, NamingRule);
				Write(key, item.Value);
			}
		}
		else if (pairsCollectionType is not null)
		{
			var enumerator = enumerable.GetEnumerator();
			IObjectTypeAccessor? typeAcc = null;
			while (enumerator.MoveNext())
			{
				var entry = enumerator.Current;
				if (entry is null)
					continue;
				typeAcc ??= Factory.CreateTypeAccessor(entry.GetType());
				typeAcc.TryGetValue(entry, "Key", out var entryKey);
				typeAcc.TryGetValue(entry, "Value", out var entryValue);
				string key = entryKey is null ?
					"$null":
					NamingRule == 0 || NamingRule.HasFlag(NamingCaseRule.Force) ?
						Convert.ToString(entryKey) ?? "$null":
						Strings.ToNamingRule(Convert.ToString(entryKey) ?? "$null", NamingRule);
				Write(key, entryValue);
			}
		}
		else
		{
			WriteArray(enumerable);
		}

		End();
	}

	protected virtual void OrderProperties(List<(string Name, object? Value)> members)
	{
	}

	protected virtual void WriteArray(IEnumerable enumerable)
	{
		int count = 0;
		foreach (var item in enumerable)
		{
			if (++count > ArrayMaxLength)
				break;
			if (count > 0)
				Comma(',');
			Write(null, item);
		}
		if (count > ArrayMaxLength)
		{
			if (count > 0)
				Comma(',');
			WriteText(EllipsisString);
		}
	}

	protected virtual void WriteObjectType(Type type)
	{
		WriteText(type.GetTypeName());
		WriteText(':');
	}
}
