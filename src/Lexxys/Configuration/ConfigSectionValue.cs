// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Threading;

using Lexxys;

#nullable disable

namespace Lexxys
{
	internal class ConfigSectionValue<T>: ISectionValue<T>
	{
		private readonly Func<T> _value;
		private readonly Func<int> _version;
		volatile private VersionValue? _item;

		public ConfigSectionValue(Func<T> value, Func<int> version)
		{
			_value = value ?? throw new ArgumentNullException(nameof(value));
			_version = version ?? throw new ArgumentNullException(nameof(version));
			_item = default;
		}

		public T Value
		{
			get
			{
				for (; ; )
				{
					var current = _item;
					var version = _version();
					if (current?.Version == version)
						return current.Value;
					var value = _value();
					var updated = new VersionValue(version, value);
					Interlocked.CompareExchange(ref _item, updated, current);
				}
			}
		}

		public int Version => _item?.Version ?? 0;

		object? IValue.Value => Value;

		class VersionValue
		{
			public T Value { get; }
			public int Version { get; }

			public VersionValue(int version, T value)
			{
				Value = value;
				Version = version;
			}
		}
	}
}
