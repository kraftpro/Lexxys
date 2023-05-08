// Lexxys Infrastructural library.
// file: TestInfo.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//

namespace Lexxys.Tests.Tools
{
	internal class TestInfo
	{
		public string Test;
		public string ExpectedResult;

		public static TestInfo Parse(string value)
		{
			int index = value.IndexOf("=>", StringComparison.Ordinal);
			if (index == -1)
				throw new ArgumentException("Value must contain '=>' string");


			return new TestInfo()
			{
				Test = value.Substring(0, index).Trim(),
				ExpectedResult = value.Substring(index + 2, value.Length - index - 2).Trim()
			};
		}
	}
}
