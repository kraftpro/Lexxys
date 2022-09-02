using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

#nullable enable

namespace Lexxys
{
	public readonly struct ErrorCode: IEquatable<ErrorCode>
	{
		private const int GroupsCount = 64;
		private const int GroupSize = 32;
		private const int BasketSize = 32;

		public const int ErrorCodesCapacity = GroupsCount * GroupSize * BasketSize;
		public const int TemporaryIndexStart = (GroupsCount - 16) * GroupSize * BasketSize;

		private static readonly string?[]?[]?[] __groups = new string[GroupsCount][][];
		private static volatile int __index = TemporaryIndexStart;

		public static readonly ErrorCode Default = Create(0, "Default");
		/// <summary>
		/// The value is null or empty. parameters: value
		/// </summary>
		public static readonly ErrorCode NullValue = Create(1, nameof(NullValue));
		/// <summary>
		/// Foreign key reference not found. parameters: value, reference
		/// </summary>
		public static readonly ErrorCode BadReference = Create(2, nameof(BadReference));
		/// <summary>
		/// The value is not unique. parameters: value
		/// </summary>
		public static readonly ErrorCode NotUniqueValue = Create(3, nameof(NotUniqueValue));
		/// <summary>
		/// The value is out of range of the valid values. parameters: value [, min] [, max]
		/// </summary>
		public static readonly ErrorCode OutOfRange = Create(4, "OutOfRange");
		/// <summary>
		/// Invalid format of the value. parameters: value
		/// </summary>
		public static readonly ErrorCode BadFormat = Create(5, nameof(BadFormat));


		public int Code { get; }

		public ErrorCode(int value)
		{
			if (value is < 0 or >= ErrorCodesCapacity)
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			if(__groups[(uint)value >> 10]?[((uint)value >> 5) & 31]?[value & 31] == null)
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			Code = value;
		}

		public string Name
		{
			get
			{
				var i = (uint)Code;
				return __groups[i >> 10]![(i >> 5) & 31]![i & 31]!;
			}
		}

		private ErrorCode(uint value)
		{
			Code = (int)value;
		}

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <inheritdoc />
		public override bool Equals([NotNullWhen(true)] object? obj) => obj is ErrorCode error && Equals(error);

		/// <inheritdoc />
		public override int GetHashCode() => Code.GetHashCode();

		/// <inheritdoc />
		public bool Equals(ErrorCode other) => Code == other.Code;

		public static bool operator == (ErrorCode left, ErrorCode right) => left.Code == right.Code;
		public static bool operator != (ErrorCode left, ErrorCode right) => left.Code != right.Code;

		public static ErrorCode FromInt32(int value) => new ErrorCode(value);
		public int ToInt32() => Code;

		public static explicit operator ErrorCode(int value) => new ErrorCode(value);
		public static implicit operator int(ErrorCode value) => value.Code;

		/// <summary>
		/// Finds the first <see cref="ErrorCode"/> with the specified name or creates a new one with random value.
		/// </summary>
		/// <param name="name"><see cref="ErrorCode"/> name to find or create a new one</param>
		public static ErrorCode GetOrCreate(string name)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if ((name = name.Trim()).Length == 0)
				throw new ArgumentOutOfRangeException(nameof(name));

			int i = FindIndex(name);
			if (i >= 0)
				return new ErrorCode((uint)i);

			for (;;)
			{
				i = Interlocked.Increment(ref __index) - 1;
				if (i >= ErrorCodesCapacity)
					throw new InvalidOperationException($"Cannot create an ErrorCode for {name}: Too many ErrorCodes.");
				if (TryCreate(i, name, out var code))
					return code;
			}
		}

		/// <summary>
		/// Creates a new <see cref="ErrorCode"/> with the specified <paramref name="value"/> and <paramref name="name"/>.
		/// The function doesn't check the <paramref name="name"/> for uniqueness.
		/// </summary>
		/// <param name="value">Integer value of the created <see cref="ErrorCode"/></param>
		/// <param name="name">Name of the created <see cref="ErrorCode"/></param>
		public static ErrorCode Create(int value, string name)
		{
			if (value < 0 || value > ErrorCodesCapacity)
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if ((name = name.Trim()).Length == 0)
				throw new ArgumentOutOfRangeException(nameof(name));

			if (!TryCreate(value, name, out var code))
				throw new ArgumentOutOfRangeException(nameof(name), name, null);
			return code;
		}

		/// <summary>
		/// Creates a new <see cref="ErrorCode"/> with the specified <paramref name="value"/> and <paramref name="name"/>.
		/// The function doesn't check the <paramref name="name"/> for uniqueness.
		/// </summary>
		/// <param name="value">Integer value of the created <see cref="ErrorCode"/></param>
		/// <param name="name">Name of the created <see cref="ErrorCode"/></param>
		/// <param name="result">Created <see cref="ErrorCode"/> or default value</param>
		/// <returns>True if the <see cref="ErrorCode"/> has been created.</returns>
		public static bool TryCreate(int value, string? name, out ErrorCode result)
		{
			if (value is < 0 or > ErrorCodesCapacity || (name = name.TrimToNull()) == null)
			{
				result = default;
				return false;
			}
			var gi = (uint)value >> 10;
			var bi = (uint)(value >> 5) & 31;
			var ix = value & 31;
			if (__groups[gi] == null)
				Interlocked.CompareExchange(ref __groups[gi], new string[GroupSize][], null);
			var group = __groups[gi]!;
			if (group[bi] == null)
				Interlocked.CompareExchange(ref group[bi], new string[BasketSize], null);
			var items = group[bi]!;
			if (Interlocked.CompareExchange(ref items[ix], name, null) != null && !String.Equals(name, items[ix], StringComparison.OrdinalIgnoreCase))
			{
				result = default;
				return false;
			}
			result = new ErrorCode((uint)value);
			return true;
		}

		/// <summary>
		/// Converts the string representation of an error-code to its <see cref="ErrorCode"/> equivalent or default value of <see cref="ErrorCode"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static ErrorCode Parse(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (TryParse(value, out var result))
				return result;
			throw new FormatException($"'{value}' does not contain a valid string representation of an error code");
		}

		/// <summary>
		/// Converts the string representation of a error-code to its <see cref="ErrorCode"/> equivalent or default value of <see cref="ErrorCode"/>.
		/// </summary>
		/// <param name="value">A string containing a error-code to convert</param>
		public static ErrorCode ParseOrDefault(string value)
		{
			_ = TryParse(value, out var code);
			return code;
		}

		/// <summary>
		/// Converts the string representation of a error-code to its <see cref="ErrorCode"/> equivalent.
		/// A return value indicates whether the conversion succeeded.
		/// </summary>
		/// <param name="value">A string containing a error-code to convert</param>
		/// <param name="code">Result of the conversion or default value of <see cref="ErrorCode"/></param>
		/// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
		public static bool TryParse(string value, out ErrorCode code)
		{
			if (String.IsNullOrEmpty(value))
			{
				code = default;
				return false;
			}
			if (int.TryParse(value, out var index))
			{
				if (index < 0 || index > ErrorCodesCapacity || __groups[(uint)index >> 10]?[((uint)index >> 5) & 31]?[index & 31] == null)
				{
					code = default;
					return false;
				}
				code = new ErrorCode(index);
				return true;
			}
			value = value.Replace(" ", "").Replace("-", "").Replace("_", "");
			int i = FindIndex(value);
			if (i < 0)
			{
				code = default;
				return false;
			}
			code = new ErrorCode((uint)i);
			return true;
		}

		private static int FindIndex(string value)
		{
			if (value.Length == 0)
				return -1;
			for (int i = 0; i < __groups.Length; i++)
			{
				var group = __groups[i];
				if (group == null)
					continue;
				for (int j = 0; j < group.Length; j++)
				{
					var basket = group[j];
					if (basket == null)
						continue;
					for (int k = 0; k < basket.Length; k++)
					{
						if (basket[k] != null && String.Equals(basket[k], value, StringComparison.OrdinalIgnoreCase))
							return (i * GroupSize + j) * BasketSize + k;
					}
				}
			}
			return -1;
		}
	}
}
