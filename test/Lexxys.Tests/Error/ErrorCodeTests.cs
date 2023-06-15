using Lexxys.Validation;

namespace Lexxys.Tests.Error
{
	[TestClass]
	public class ErrorCodeTests
	{

		[TestMethod]
		public void CreateExtraErrorCodes()
		{
			var c10 = ErrorCode.Create(10, "Ten");
			var c10a = ErrorCode.Create(10, " ten ");
			Assert.AreEqual(c10, c10a);
			var c1000 = ErrorCode.Create(1000, "Thnd");
			var c1011 = ErrorCode.Create(1011, "ThndEleven");
			Assert.AreEqual(c10.Name, "Ten");
			Assert.AreEqual(c1000.Name, "Thnd");
			Assert.AreEqual(c1011.Name, "ThndEleven");
		}

		[TestMethod]
		public void TestConstructor()
		{
			var c10 = ErrorCode.Create(10, "Ten");
			var c10a = new ErrorCode(10);
			Assert.AreEqual(c10, c10a);

			var c0 = new ErrorCode();
			var c0a = new ErrorCode(0);
			Assert.AreEqual(c0, c0a);
		}

		[TestMethod]
		public void TestParse()
		{
			var c10 = ErrorCode.Create(10, "Ten");
			var c10a = new ErrorCode(10);
			var c10b = ErrorCode.Parse("TEN");
			var c10c = ErrorCode.Parse(" ten ");
			Assert.AreEqual(c10, c10a);
			Assert.AreEqual(c10, c10b);
			Assert.AreEqual(c10, c10c);

			var c0 = new ErrorCode();
			var c0a = ErrorCode.Parse("default");
			Assert.AreEqual(c0, c0a);
		}
	}
}
