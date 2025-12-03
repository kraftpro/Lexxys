using System.Text;

namespace Lexxys;

public interface ITextWriter
{
	void Text(ReadOnlySpan<char> value);
}

public class DumpWriter: DumpWriterCore
{
	private readonly ITextWriter _writer;

	public DumpWriter(ITextWriter writer, DumpWriterOptions? options = null): base(options)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
	}

	public DumpWriter Configure(Action<DumpWriterOptions> configure)
	{
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));
		configure(_options);
		return this;
	}

	protected override void Text(ReadOnlySpan<char> value) => _writer.Text(value);

	public static DumpStringWriter Create(StringBuilder sb, DumpWriterOptions? options = null) => new DumpStringWriter(sb, options);

	public static DumpStreamWriter Create(TextWriter writer, DumpWriterOptions? options = null) => new DumpStreamWriter(writer, options);

	public static DumpStringWriter Create(DumpWriterOptions? options = null) => new DumpStringWriter(options);

	public static DumpWriter Create(ITextWriter writer, DumpWriterOptions? options = null) => new DumpWriter(writer, options);

	public static StringBuilder Dump(object? value, DumpWriterOptions? options = null)
	{
		var dw = new DumpStringWriter(options);
		dw.Write(value);
		return dw.GetBuffer();
	}
}

public class DumpStringWriter: DumpWriterCore
{
	private readonly StringBuilder _text;

	public DumpStringWriter(DumpWriterOptions? options = null) : base(options) => _text = new StringBuilder();

	public DumpStringWriter(StringBuilder builder, DumpWriterOptions? options = null) : base(options) => _text = builder ?? throw new ArgumentNullException(nameof(builder));

	protected override void Text(ReadOnlySpan<char> value) => _text.Append(value);

	public StringBuilder GetBuffer() => _text;

	public override string ToString() => _text.ToString();
}

public class DumpStreamWriter: DumpWriterCore
{
	private readonly TextWriter _writer;

	public DumpStreamWriter(TextWriter writer, DumpWriterOptions? options = null) : base(options)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
	}

	/// <inheritdoc />
	protected override void Text(ReadOnlySpan<char> value) => _writer.Write(value);
}
