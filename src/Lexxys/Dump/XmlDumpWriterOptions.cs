namespace Lexxys;

public class XmlDumpWriterOptions: DumpWriterOptions
{
	public int CDataThreshold { get; init; }
	public bool UseAttributesForPrimitives { get; init; }

	public XmlDumpWriterOptions()
	{
		UseAttributesForPrimitives = true;
	}

	public XmlDumpWriterOptions(DumpWriterOptions options): base(options)
	{
		if (options is XmlDumpWriterOptions xmlOptions)
		{
			CDataThreshold = xmlOptions.CDataThreshold;
			UseAttributesForPrimitives = xmlOptions.UseAttributesForPrimitives;
		}
		else
		{
			UseAttributesForPrimitives = true;
		}
	}
}
