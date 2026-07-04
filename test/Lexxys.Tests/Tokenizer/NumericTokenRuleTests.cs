using Lexxys.Tokenizer;

using System.Globalization;

namespace Lexxys.Tests.Tokenizer;

[TestClass]
public class NumericTokenRuleTests
{
	[TestMethod]
	[DataRow("0", "0", typeof(int), 1)]
	[DataRow("42", "42", typeof(int), 2)]
	[DataRow("-2147483649", "-2147483649", typeof(long), 11)]
	[DataRow("3.25", "3.25", typeof(decimal), 4)]
	[DataRow("1.5e2", "150", typeof(double), 5)]
	public void TryParse_DefaultStyle_ParsesCommonNumbers(string input, string expected, Type expectedType, int expectedLength)
	{
		var stream = new CharStream(input);
		var token = new NumericTokenRule().TryParse(ref stream);
		var value = token.GetValue(stream);

		Assert.AreEqual(LexicalTokenType.NUMERIC, token.TokenType);
		Assert.AreEqual(expectedLength, token.Length);
		Assert.IsInstanceOfType(value, expectedType);
		Assert.AreEqual(expected, Convert.ToString(value, CultureInfo.InvariantCulture));
		Assert.AreEqual(expectedLength, stream.Position);
	}

	[TestMethod]
	[DataRow("0x10", 16)]
	[DataRow("0b1010", 10)]
	[DataRow("0o17", 15)]
	[DataRow("-0x10", -16)]
	public void TryParse_IntegerStyle_ParsesPrefixedNumbers(string input, object expected)
	{
		var stream = new CharStream(input);
		var token = new NumericTokenRule(NumericTokenStyles.Integer).TryParse(ref stream);

		Assert.AreEqual(expected, token.GetValue(stream));
		Assert.AreEqual(input.Length, stream.Position);
	}

	[TestMethod]
	public void TryParse_DisallowedPrefix_ReturnsEmptyAndDoesNotAdvance()
	{
		var stream = new CharStream("0x10");
		var token = new NumericTokenRule(NumericTokenStyles.Ordinal).TryParse(ref stream);

		Assert.IsTrue(token.IsEmpty);
		Assert.AreEqual(0, stream.Position);
	}

	[TestMethod]
	public void TestBeginning_ReflectsConfiguredSignAndDecimalOptions()
	{
		var ordinal = new NumericTokenRule(NumericTokenStyles.Ordinal);
		Assert.IsTrue(ordinal.TestBeginning('1'));
		Assert.IsFalse(ordinal.TestBeginning('-'));
		Assert.IsFalse(ordinal.TestBeginning('.'));

		var flexible = new NumericTokenRule(NumericTokenStyles.Double | NumericTokenStyles.PositiveSign | NumericTokenStyles.StartingWithDot);
		Assert.IsTrue(flexible.TestBeginning('-'));
		Assert.IsTrue(flexible.TestBeginning('+'));
		Assert.IsTrue(flexible.TestBeginning('.'));
	}
}
