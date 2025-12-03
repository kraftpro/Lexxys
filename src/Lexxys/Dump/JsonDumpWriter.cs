using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys;

public abstract class JsonDumpWriter: DumpWriterCore
{
	private static readonly JsonDumpWriterOptions DefaultOptions = new JsonDumpWriterOptions(DumpWriterOptions.Unlimited);

	protected JsonDumpWriter(DumpWriterOptions? options = null): base(options switch
	{
		null => DefaultOptions,
		JsonDumpWriterOptions jsonOptions => jsonOptions,
		_ => new JsonDumpWriterOptions(options)
	})
	{
	}

	public override string Type => "json";

	public JsonDumpWriter Configure(Action<JsonDumpWriterOptions> configure)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));
		configure((JsonDumpWriterOptions)_options);
		return this;
	}

	public override IDumpWriter Begin(char type = default, string? name = null) => base.Begin(type == '[' ? '[': '{', name);

	protected override void WriteName(ReadOnlySpan<char> name) => WriteString(name);

	protected override void WriteGuid(Guid value) => WriteQuoted(value.ToString());

	protected override void WriteDateTime(DateTimeOffset value)
	{
		WriteText('"');
		base.WriteDateTime(value);
		WriteText('"');
	}

	protected override void WriteTimeSpan(TimeSpan value) => WriteQuoted(value.ToString("c"));

	protected override void WriteBinary(IEnumerable<byte> bytes)
	{
		WriteText('"');
		WriteBase64(bytes);
		WriteText('"');
	}

	protected override void WriteReference(string reference)
	{
		WriteText('"');
		base.WriteReference(reference);
		WriteText('"');
	}

	protected override void WriteChar(char value)
	{
#if NET
		Span<char> span = new Span<char>(ref value);
#else
		Span<char> span = stackalloc char[1] { value };
#endif
		WriteString(span);
	}

	private void WriteQuoted(ReadOnlySpan<char> value)
	{
		WriteText('"');
		WriteText(value);
		WriteText('"');
	}


	public static JsonDumpStringWriter Create(DumpWriterOptions? options = null) => new JsonDumpStringWriter(options);

	public static JsonDumpStringWriter Create(StringBuilder builder, DumpWriterOptions? options = null) => new JsonDumpStringWriter(builder, options);

	public static JsonDumpStreamWriter Create(TextWriter writer, DumpWriterOptions? options = null) => new JsonDumpStreamWriter(writer, options);
}

public class JsonDumpStringWriter: JsonDumpWriter
{
	private readonly StringBuilder _text;

	public JsonDumpStringWriter(DumpWriterOptions? options = null): base(options) => _text = new StringBuilder();

	public JsonDumpStringWriter(StringBuilder builder, DumpWriterOptions? options = null): base(options) => _text = builder ?? throw new ArgumentNullException(nameof(builder));

	protected override void Text(ReadOnlySpan<char> value) => _text.Append(value);

	public StringBuilder GetBuffer() => _text;

	public override string ToString() => _text.ToString();
}

public class JsonDumpStreamWriter: JsonDumpWriter
{
	private readonly TextWriter _writer;

	public JsonDumpStreamWriter(TextWriter writer, DumpWriterOptions? options = null): base(options)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
	}

	/// <inheritdoc />
	protected override void Text(ReadOnlySpan<char> value) => _writer.Write(value);
}
