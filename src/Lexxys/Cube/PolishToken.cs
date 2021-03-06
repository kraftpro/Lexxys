// Lexxys Infrastructural library.
// file: PolishToken.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Cube
{
	/// <summary>Abstract element of expression</summary>
	public abstract class PolishToken
	{
		public const int MinPriority = -9;

		/// <summary>Should the element be manipulated as an open brace</summary>
		public virtual bool IsOpenBrace { get { return false; } }
		/// <summary>Should the element be manipulated as an closed brace</summary>
		public virtual bool IsClosedBrace { get { return false; } }
		/// <summary>Priority of the Element</summary>
		public abstract int Priority { get; }
		/// <summary>Evaluate expression element</summary>
		public abstract void Evaluate(Stack<PolishToken> stack, Delegate context);
	}

	/// <summary>Represents open brace</summary>
	public class OpenBraceToken: PolishToken
	{
		public override bool IsOpenBrace
		{
			get { return true; }
		}
		public override int Priority
		{
			get { return MinPriority - 2; }
		}
		public override void Evaluate(Stack<PolishToken> stack, Delegate context)
		{
			throw EX.NotSupported("OpenBraceToken.Evaluate");
		}
	}

	/// <summary>Represents closed brace</summary>
	public class ClosedBraceToken: PolishToken
	{
		public override bool IsClosedBrace
		{
			get { return true; }
		}
		public override int Priority
		{
			get { return MinPriority - 1; }
		}
		public override void Evaluate(Stack<PolishToken> stack, Delegate context)
		{
			throw EX.NotSupported("ClosedBraceToken.Evaluate");
		}
	}

	/// <summary>Represents literal (constant, variable or function)</summary>
	public class LiteralToken: PolishToken
	{
		public override int Priority
		{
			get { return 0; }
		}
		public override void Evaluate(Stack<PolishToken> stack, Delegate context)
		{
			stack.Push(this);
		}
	}
}


