// Lexxys Infrastructural library.
// file: Ref.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Threading;
using Microsoft.Extensions.Options;

#pragma warning disable CA2225 // Operator overloads have named alternates
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1051 // Do not declare visible instance fields

namespace Lexxys
{
	public class Out<T>: IValue<T>
	{
		private readonly Func<T> _getter;

		public Out(Func<T> getter)
		{
			_getter = getter;
		}

		public virtual T Value
		{
			get => _getter();
			set => throw new NotImplementedException("Out<T>.Value.Set in not implemented.");
		}

		object? IValue.Value => Value;

		public static implicit operator T(Out<T> value) => value.Value;
	}

	public sealed class Ref<T>: Out<T>
	{
		private readonly Action<T> _setter;

		public Ref(Func<T> getter, Action<T> setter): base(getter)
		{
			_setter = setter;
		}

		public override T Value
		{
			get => base.Value;
			set => _setter(value);
		}

		public static implicit operator T(Ref<T> value) => value.Value;

		public static implicit operator Ref<T>(T value)
		{
			var boxed = new Boxed { Value = value };
			return new Ref<T>(() => boxed.Value, v => Interlocked.Exchange(ref boxed, new Boxed { Value = v }));
		}

		private class Boxed { public T? Value; }
	}


	public class OptOut<T>: IValue<T>, IOptions<T> where T: class
	{
		protected readonly Func<T> _getter;

		public OptOut(Func<T> getter)
		{
			_getter = getter;
		}

		public virtual T Value
		{
			get => _getter();
			set => throw new NotImplementedException("Out<T>.Value.get");
		}

		object IValue.Value => Value;

		public static implicit operator T(OptOut<T> value) => value.Value;
		public static implicit operator Out<T>(OptOut<T> value) => new Out<T>(value._getter);
	}

	public sealed class OptRef<T>: OptOut<T> where T: class
	{
		private readonly Action<T> _setter;

		public OptRef(Func<T> getter, Action<T> setter): base(getter)
		{
			_setter = setter;
		}

		public override T Value
		{
			get => base.Value;
			set => _setter(value);
		}

		public static implicit operator T(OptRef<T> value) => value.Value;

		public static implicit operator OptRef<T>(T value)
		{
			var copy = value;
			return new OptRef<T>(() => copy, v => Interlocked.Exchange(ref copy, v));
		}
		public static implicit operator Ref<T>(OptRef<T> value) => new Ref<T>(value._getter, value._setter);
	}
}


