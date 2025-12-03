// Lexxys Infrastructural library.
// file: XmlDumpWriter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Lexxys.Xml;

using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace Lexxys;

public abstract class XmlDumpWriter: DumpWriterCore
{
	private const string AnonymousObjectName = "object";
	private const string AnonymousArrayName = "array";
	private const string ArrayItemName = "item";

	private static readonly XmlDumpWriterOptions DefaultOptions = new XmlDumpWriterOptions(DumpWriterOptions.Unlimited) { IncludeObjectType = false };

	private readonly Stack<string> _elements;
	private readonly XmlDumpWriterOptions _xmlOptions;
	private bool _inElement;

	protected XmlDumpWriter(DumpWriterOptions? options = null): base(options switch
	{
		null => DefaultOptions,
		XmlDumpWriterOptions xmlOptions => xmlOptions,
		_ => new XmlDumpWriterOptions(options)
	})
	{
		_elements = new Stack<string>();
		_xmlOptions = (XmlDumpWriterOptions)_options;
	}


	public override string Type => "xml";

	public bool UseAttributesForPrimitives => _xmlOptions.UseAttributesForPrimitives;

	public int CDataThreshold => _xmlOptions.CDataThreshold;

	public XmlDumpWriter Configure(Action<XmlDumpWriterOptions> configure)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));
		configure(_xmlOptions);
		return this;
	}

	public override IDumpWriter Begin(char type = default, string? name = null)
	{
		var stateName = name == null
			? _objectStack.Count == 0 ? null: _objectStack.Peek().Name
			: _objectStack.Count == 0 || _objectStack.Peek().Name == null ? name: _objectStack.Peek().Name + "." + name;
		_objectStack.Push(('>', stateName));

		if (_inElement)
			WriteText('>');
		else
			_inElement = true;
		WriteText('<');
		name = name is not null ? CleanName(name): type == '[' ? AnonymousArrayName: AnonymousObjectName;
		WriteText(name);
		_elements.Push(name);
		return this;
	}

	[return: NotNullIfNotNull(nameof(name))]
	private string? CleanName(string? name) => name == null ? null: XmlConvert.EncodeName(name);


	public override IDumpWriter End()
	{
		if (_elements.Count == 0 || _objectStack.Count == 0)
			throw new InvalidOperationException("No element to end.");
		var name = _elements.Pop();
		_objectStack.Pop();

		if (_inElement)
		{
			WriteText("/>");
			_inElement = false;
		}
		else
		{
			WriteText("</");
			WriteText(name);
			WriteText('>');
		}
		return this;
	}

	protected override void WriteReference(string reference)
	{
		if (UseAttributesForPrimitives)
		{
			WriteText("$ref$:");
			WriteText(reference);
		}
		else
		{
			WriteText(" __ref=\"");
			WriteText(reference);
			WriteText('"');
		}
	}

	protected override void WriteObjectType(Type type)
	{
		WriteText(" __type=\"");
		WriteText(type.GetTypeName());
		WriteText('"');
	}

	protected override bool WritePrimitive<T>(string? name, T value)
	{
		if (value != null)
			return base.WritePrimitive(name, value);
		if (_inElement)
			return true;

		WriteText('<');
		WriteText(name ?? "null");
		WriteText("/>");
		return true;
	}
	protected override void WritePrimitiveBegin(string? name)
	{
		name = CleanName(name);
		if (name is null) return;

		if (UseAttributesForPrimitives && _inElement)
		{
			WriteText(' ');
			WriteText(name);
			WriteText("=\"");
			return;
		}

		if (_inElement)
		{
			WriteText('>');
			_inElement = false;
		}
		WriteText('<');
		WriteText(name);
		WriteText('>');
	}

	protected override void WritePrimitiveEnd(string? name)
	{
		name = CleanName(name);
		if (name == null) return;
		if (_inElement)
		{
			WriteText('"');
		}
		else
		{
			WriteText("</");
			WriteText(name);
			WriteText('>');
		}
	}

	protected override void WriteArray(IEnumerable enumerable)
	{
		if (_inElement)
		{
			WriteText('>');
			_inElement = false;
		}
		int count = 0;
		foreach (var item in enumerable)
		{
			if (++count > ArrayMaxLength)
				break;
			Write(ArrayItemName, item);
		}
		if (count > ArrayMaxLength)
		{
			WriteText(EllipsisString);
		}
	}

	protected override void OrderProperties(List<(string Name, object? Value)> members)
	{
		if (!UseAttributesForPrimitives)
			return;
#if NET
		var mm = CollectionsMarshal.AsSpan(members);

		while (mm.Length > 0 && IsPrimitive(mm[0].Value))
			mm = mm[1..];

		for (int i = 1; i < mm.Length;)
		{
			if (!IsPrimitive(mm[i].Value))
			{
				++i;
				continue;
			}
			var tmp = mm[i];
			mm[..i].CopyTo(mm[1..]);
			mm[0] = tmp;
			mm = mm.Slice(1);
		}
#else
		int j = 0;
		while (j < members.Count && IsPrimitive(members[j].Value))
			++j;

		for (int i = j + 1; i < members.Count; ++i)
		{
			if (!IsPrimitive(members[i].Value))
				continue;

			var tmp = members[i];
			Shift(members, j, i);
			members[j] = tmp;
			++j;
		}

		static void Shift(List<(string Name, object? Value)> items, int start, int end)
		{
			for (int i = end - 1; i >= start; --i)
			{
				items[i + 1] = items[i];
			}
		}
#endif
	}

	protected override void WriteString(ReadOnlySpan<char> value)
	{
		var s = value.Length > StringMaxLength ? value.Slice(0, StringMaxLength): value;
		int i = IndexOfEscaped(s);
		while (i >= 0)
		{
			if (i > 0)
				WriteText(s[..i]);
			switch (s[i])
			{
				case '&':
					WriteText("&amp;");
					break;
				case '<':
					WriteText("&lt;");
					break;
				case '>':
					WriteText("&gt;");
					break;
				case '"':
					WriteText("&quot;");
					break;
				case '\'':
					WriteText("&apos;");
					break;
				default:
					WriteText($"&#{(int)s[i]:X};");
					break;
			}
			s = s[(i + 1)..];
			i = IndexOfEscaped(s);
		}
		WriteText(s);
		if (value.Length > StringMaxLength)
			WriteText(EllipsisString);

		static int IndexOfEscaped(ReadOnlySpan<char> s)
		{
			for (int i = 0; i < s.Length; ++i)
			{
				char c = s[i];
				if (c is '&' or '<' or '>' or '"' or '\'' ||
					c < ' ' ||
					c >= '\uD800' && c <= '\uDFFF' ||
					c >= '\uFDD0' && c <= '\uFDEF' ||
					c >= '\uFFFE')
					return i;
			}
			return -1;
		}
	}

	public static XmlDumpStringWriter Create(DumpWriterOptions? options = null) => new XmlDumpStringWriter(options);

	public static XmlDumpStringWriter Create(StringBuilder builder, DumpWriterOptions? options = null) => new XmlDumpStringWriter(builder, options);

	public static XmlDumpStreamWriter Create(TextWriter writer, DumpWriterOptions? options = null) => new XmlDumpStreamWriter(writer, options);
}

public class XmlDumpStringWriter(StringBuilder builder, DumpWriterOptions? options = null): XmlDumpWriter(options)
{
	private readonly StringBuilder _buffer = builder ?? throw new ArgumentNullException(nameof(builder));

	public XmlDumpStringWriter(DumpWriterOptions? options = null): this (new StringBuilder(), options) { }

	protected override void Text(ReadOnlySpan<char> value) => _buffer.Append(value);

	public StringBuilder GetBuffer() => _buffer;

	public override string ToString() => _buffer.ToString();
}

public class XmlDumpStreamWriter(TextWriter writer, DumpWriterOptions? options = null): XmlDumpWriter(options)
{
	private readonly TextWriter _writer = writer ?? throw new ArgumentNullException(nameof(writer));

	/// <inheritdoc />
	protected override void Text(ReadOnlySpan<char> value) => _writer.Write(value);
}

