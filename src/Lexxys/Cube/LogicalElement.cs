// Lexxys Infrastructural library.
// file: LogicalElement.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Cube
{
	/// <summary>Represents logical constant or logucal variable.</summary>
	public abstract class LogicalValue: LiteralToken
	{
		public abstract bool Evaluate(Delegate context);

		public static readonly LogicalValue True = new LogicalTrue();
		public static readonly LogicalValue False = new LogicalFalse();

		public static explicit operator LogicalValue(bool value)
		{
			return value ? True : False;
		}

		public static LogicalValue FromBoolean(bool value)
		{
			return value ? True : False;
		}

		#region Constants implementation
		private class LogicalFalse: LogicalValue
		{
			public override bool Evaluate(Delegate context) => false;
		}
		private class LogicalTrue: LogicalValue
		{
			public override bool Evaluate(Delegate context) => true;
		}
		#endregion
	}

	/// <summary>Holds logical value</summary>
	public class LogicalVariable: LogicalValue
	{
		public LogicalVariable(int index)
		{
			Index = index;
		}

		public int Index { get; }

		public override bool Evaluate(Delegate context)
		{
			return context is Predicate<int> checker && checker(Index);
		}
	}

	/// <summary>Unary logical NOT operation</summary>
	public class LogicalNot: PolishToken
	{
		public override int Priority => 3;

		public override void Evaluate(Stack<Cube.PolishToken> stack, Delegate context)
		{
			if (stack.Count < 1)
				throw EX.Argument(SR.EXP_MissingParameters("NOT"), "stack");
			PolishToken token = stack.Pop();
			if (!(token is LogicalValue x))
				throw EX.ArgumentWrongType("x)", token.GetType(), typeof(LogicalValue));

			stack.Push((LogicalValue)(!x.Evaluate(context)));
		}
	}

	/// <summary>Logical AND operation</summary>
	public class LogicalAnd: PolishToken
	{
		public override int Priority => 2;

		public override void Evaluate(Stack<Cube.PolishToken> stack, Delegate context)
		{
			if (stack.Count < 2)
				throw EX.Argument(SR.EXP_MissingParameters("AND"), "stack");
			PolishToken token = stack.Pop();
			if (!(token is LogicalValue x))
				throw EX.ArgumentWrongType("x", token.GetType(), typeof(LogicalValue));
			token = stack.Pop();
			if (!(token is LogicalValue y))
				throw EX.ArgumentWrongType("y", token.GetType(), typeof(LogicalValue));

			stack.Push((LogicalValue)(x.Evaluate(context) & y.Evaluate(context)));
		}
	}

	/// <summary>Logical OR operation</summary>
	public class LogicalOr: PolishToken
	{
		public override int Priority => 1;

		public override void Evaluate(Stack<Cube.PolishToken> stack, Delegate context)
		{
			if (stack.Count < 2)
				throw EX.Argument(SR.EXP_MissingParameters("OR"), "stack");
			PolishToken token = stack.Pop();
			if (!(token is LogicalValue x))
				throw EX.ArgumentWrongType("x", token.GetType(), typeof(LogicalValue));
			token = stack.Pop();
			if (!(token is LogicalValue y))
				throw EX.ArgumentWrongType("y", token.GetType(), typeof(LogicalValue));

			stack.Push((LogicalValue)(x.Evaluate(context) | y.Evaluate(context)));
		}
	}

	/// <summary>Logical XOR operation</summary>
	public class LogicalXor: PolishToken
	{
		public override int Priority => 1;

		public override void Evaluate(Stack<Cube.PolishToken> stack, Delegate context)
		{
			if (stack.Count < 2)
				throw EX.Argument(SR.EXP_MissingParameters("XOR"), "stack");
			PolishToken token = stack.Pop();
			if (!(token is LogicalValue x))
				throw EX.ArgumentWrongType("x", token.GetType(), typeof(LogicalValue));
			token = stack.Pop();
			if (!(token is LogicalValue y))
				throw EX.ArgumentWrongType("y", token.GetType(), typeof(LogicalValue));

			stack.Push((LogicalValue)(x.Evaluate(context) ^ y.Evaluate(context)));
		}
	}
}


