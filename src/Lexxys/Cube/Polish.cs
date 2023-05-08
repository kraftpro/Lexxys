// Lexxys Infrastructural library.
// file: Polish.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lexxys.Cube
{
	/// <summary>Expression in Reverse Polish Notation</summary>
	public class Polish
	{
		/// <summary>Expression element in Reverse Polish Notation (<see cref="Polish"/>)</summary>
		readonly List<PolishToken> _reverse = new List<PolishToken>();
		readonly Stack<PolishToken> _stack = new Stack<PolishToken>();

		public ReadOnlyCollection<PolishToken> Expression
		{
			get
			{
				if (_stack.Count > 0)
					Flush();
				return _reverse.AsReadOnly();
			}
		}
		/// <summary>Add element to the expression</summary>
		/// <param name="token"><see cref="PolishToken"/> to add</param>
		public void Add(PolishToken token)
		{
			if (token is null)
				throw new ArgumentNullException(nameof(token));

			if (token.Priority == 0)
			{
				_reverse.Add(token);
			}
			else if (token.IsOpenBrace)
			{
				_stack.Push(token);
			}
			else
			{
				while (_stack.Count > 0 && _stack.Peek().Priority >= token.Priority)
					_reverse.Add(_stack.Pop());
				if (token.IsClosedBrace)
				{
					if (!_stack.Peek().IsOpenBrace)
						throw new InvalidOperationException(SR.EXP_UnbalancedBraces());
					_stack.Pop();
				}
				else
					_stack.Push(token);
			}
		}

		/// <summary>Evaluate expression and return top element of stack</summary>
		/// <param name="context">execution context</param>
		/// <returns>top element of stack as a result of evaluation</returns>
		public PolishToken? Evaluate(Delegate context)
		{
			if (_stack.Count > 0)
				Flush();
			if (_reverse is not { Count: >0 })
				return null;

			foreach (PolishToken tok in _reverse)
			{
				tok.Evaluate(_stack, context);
			}
			if (_stack.Count != 1)
				throw new InvalidOperationException(SR.EXP_MissingOperation());
			return _stack.Pop();
		}

		/// <summary>Flush stack and finish building of expression</summary>
		private void Flush()
		{
			PolishToken t;
			while (_stack.Count > 0)
			{
				t = _stack.Pop();
				if (t.IsOpenBrace)
					throw new InvalidOperationException(SR.EXP_UnbalancedBraces());
				_reverse.Add(t);
			}
		}
	}
}


