namespace Lexxys;

public class DumpWriterOptions
{
	public int MaxDepth { get; set; }
	public int MaxLength { get; set; }
	public int StringMaxLength { get; set; }
	public int BinaryMaxLength { get; set; }
	public int ArrayMaxLength { get; set; }
	public string NullValue { get; set; }
	public char FieldSeparator { get; set; }
	public char EqualChar { get; set; }
	public bool Compact { get; set; }
	public bool FormatIndentation { get; set; }
	public string IndentationString { get; set; }
	public bool IncludeObjectType { get; set; }
	public string EllipsisString { get; set; }
	public bool Base64Binary { get; set; }
	public ObjectDumpLevel Level { get; set; }
	public NamingCaseRule NamingRule { get; set; }

	public DumpWriterOptions()
	{
		MaxDepth = 16;
		MaxLength = 1024;
		StringMaxLength = 1024;
		BinaryMaxLength = 1024;
		ArrayMaxLength = 1024;
		NullValue = "null";
		FieldSeparator = ';';
		EqualChar = '=';
		IndentationString = "  ";
		EllipsisString = "\u2026";
	}

	public DumpWriterOptions(DumpWriterOptions other)
	{
		if (other == null)
			throw new ArgumentNullException(nameof(other));
		MaxDepth = other.MaxDepth;
		MaxLength = other.MaxLength;
		StringMaxLength = other.StringMaxLength;
		BinaryMaxLength = other.BinaryMaxLength;
		ArrayMaxLength = other.ArrayMaxLength;
		NullValue = other.NullValue;
		FieldSeparator = other.FieldSeparator;
		EqualChar = other.EqualChar;
		Compact = other.Compact;
		FormatIndentation = other.FormatIndentation;
		IndentationString = other.IndentationString;
		IncludeObjectType = other.IncludeObjectType;
		EllipsisString = other.EllipsisString;
		Base64Binary = other.Base64Binary;
		Level = other.Level;
		NamingRule = other.NamingRule;
	}

	public static readonly DumpWriterOptions Default = new DumpWriterOptions { IncludeObjectType = true };
	public static readonly DumpWriterOptions Unlimited = new DumpWriterOptions
		{ 
			MaxDepth = int.MaxValue,
			MaxLength = int.MaxValue,
			StringMaxLength = int.MaxValue,
			BinaryMaxLength = int.MaxValue,
			ArrayMaxLength = int.MaxValue,
			Compact = true,
			IncludeObjectType = true
		};

	public override string ToString()
		=> $"DumpWriterOptions {{ MaxDepth = {MaxDepth}, MaxLength = {MaxLength}, StringMaxLength = {StringMaxLength}, BinaryMaxLength = {BinaryMaxLength}, ArrayMaxLength = {ArrayMaxLength}, NullValue = {NullValue}, FieldSeparator = '{FieldSeparator}', EqualChar = '{EqualChar}', Compact = {Compact}, FormatIndentation = {FormatIndentation}, IndentationString = \"{IndentationString}\", IncludeObjectType = {IncludeObjectType}, EllipsisString = \"{EllipsisString}\", Level = {Level} }}";
}

