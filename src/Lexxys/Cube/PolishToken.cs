// Lexxys Infrastructural library.
// file: PolishToken.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

namespace Lexxys.Cube
{
	/// <summary>Abstract element of expression</summary>
	public abstract class PolishToken
	{
		public const int MinPriority = -9;

		/// <summary>Should the element be manipulated as an open brace</summary>
		public virtual bool IsOpenBrace => false;
		/// <summary>Should the element be manipulated as an closed brace</summary>
		public virtual bool IsClosedBrace => false;
		/// <summary>Priority of the Element</summary>
		public abstract int Priority { get; }
		/// <summary>Evaluate expression element</summary>
		public abstract void Evaluate(Stack<PolishToken> stack, Delegate context);

		protected static Exception ParameterTypeException(string name, PolishToken token) => new ArgumentTypeException(name, token?.GetType() ?? typeof(void), typeof(LogicalValue));
	}

	/// <summary>Represents open brace</summary>
	public class OpenBraceToken: PolishToken
	{
		public override bool IsOpenBrace => true;
		public override int Priority => MinPriority - 2;
		public override void Evaluate(Stack<PolishToken> stack, Delegate context)
		{
			throw new NotSupportedException(SR.OperationNotSupported("OpenBraceToken.Evaluate"));
		}
	}

	/// <summary>Represents closed brace</summary>
	public class ClosedBraceToken: PolishToken
	{
		public override bool IsClosedBrace => true;
		public override int Priority => MinPriority - 1;
		public override void Evaluate(Stack<PolishToken> stack, Delegate context)
		{
			throw new NotSupportedException(SR.OperationNotSupported("ClosedBraceToken.Evaluate"));
		}
	}

	/// <summary>Represents literal (constant, variable or function)</summary>
	public class LiteralToken: PolishToken
	{
		public override int Priority => 0;
		public override void Evaluate(Stack<PolishToken> stack, Delegate context)
		{
			if (stack is null)
				throw new ArgumentNullException(nameof(stack));
			stack.Push(this);
		}
	}
}


