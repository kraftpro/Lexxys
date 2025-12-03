using System.Text;

namespace Lexxys;

public static class DumpWriterExtensions
{
	public static IDumpWriter Write<T>(IDumpWriter writer, string? name, T? value)
		where T: struct
		=> value.HasValue ? writer.Write(name, value.GetValueOrDefault()) : writer.Write(name, (object?)null);

	public static IDumpWriter Write<T>(this IDumpWriter writer, T? value) where T: struct
		=> Write<T>(writer, null, value);

	public static IDumpWriter Write<T>(this IDumpWriter writer, T value) => writer.Write<T>(null, value);

	public static IDumpWriter Begin(this IDumpWriter writer, string? name, char type = default) => writer.Begin(type, name);

	public static IDumpWriter BeginObject(this IDumpWriter writer, string? name = null) => writer.Begin('{', name);

	public static IDumpWriter BeginArray(this IDumpWriter writer, string? name = null) => writer.Begin('[', name);


	public static IDumpWriter Field(this IDumpWriter writer, object? value, [CallerArgumentExpression(nameof(value))] string? name = null)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));
		return writer.Write(name, value);
	}

	public static IDumpWriter Dump(this IDump obj, IDumpWriter writer)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));
		if (obj is null)
			return writer;
		bool isValue = obj is IDumpValue;
		if (!isValue)
			writer.BeginObject();
		obj.DumpContent(writer);
		if (!isValue)
			writer.End();
		return writer;
	}

	public static string Dump(this IDump? obj, DumpWriterOptions? options = null)
	{
		if (obj == null)
			return String.Empty;
		var writer = new DumpStringWriter(options);
		bool isValue = obj is IDumpValue;
		if (!isValue)
			writer.BeginObject();
		obj.DumpContent(writer);
		if (!isValue)
			writer.End();
		return writer.ToString();
	}


	public static void Dump(this IDumpWriter writer, object? value, DumpWriterOptions? options = null)
	{
		if (writer is null) throw new ArgumentNullException(nameof(writer));
		if (value is IDump dv)
			dv.Dump(writer);
		else
			writer.Write(value);
	}

	public static StringBuilder Dump(this StringBuilder sb, object? value, DumpWriterOptions? options = null)
	{
		if (sb is null) throw new ArgumentNullException(nameof(sb));

		new DumpStringWriter(sb, options).Write(value);
		return sb;
	}

	public static TextWriter Dump(this TextWriter writer, object? value, DumpWriterOptions? options = null)
	{
		if (writer is null) throw new ArgumentNullException(nameof(writer));

		new DumpStreamWriter(writer, options).Write(value);
		return writer;
	}
}
