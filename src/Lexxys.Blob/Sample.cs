namespace Lexxys;

internal class Sample
{
	public static void Test(ISampleFactory factory)
	{
		Console.WriteLine("Sample class in Lexxys.Blob");

		factory.RegisterStorage("documents", new Uri(@"C:\Data\Documents"));
		factory.RegisterStorage("images", new Uri(@"s3://blah.aws.com/asoeiurkqwjrh"));
		
		// factory.RegisterStorage("documents", new FileBlobService();

	}

	public interface ISampleFactory
	{
		Sample Create();
		void RegisterStorage(string name, Uri baseUri);
	}
}
