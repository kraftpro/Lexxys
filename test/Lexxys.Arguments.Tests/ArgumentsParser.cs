internal class ArgumentsParser<T>
{
	private string[] args;

	public ArgumentsParser(string[] args)
	{
		this.args = args;
	}

	internal T? Parse()
	{
		throw new NotImplementedException();
	}
}