using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lexxys.Con;

static class Assert
{
	public static AssertBuilder<T> That<T>(T actual, [CallerArgumentExpression(nameof(actual))] string? expression = null) => new AssertBuilder<T>(actual, expression);

	public class AssertBuilder<T>
	{
		T _actual;
		string? _expression;

		public AssertBuilder(T actual, string? expression)
		{
			_actual = actual;
			_expression = expression;
		}

		public AssertBuilder(T actual) => _actual = actual;

		public void EqualTo(T expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
		{
			if (!Equals(_actual, expected))
			{
				Debugger.Break();
				throw new InvalidOperationException($"Expected: {expected}, Actual: {_actual}");
			}
		}
		public void IsNull()
		{
			if (_actual != null)
			{
				Debugger.Break();
				throw new InvalidOperationException($"Expected: null, Actual: {_actual}");
			}
		}
	}
}