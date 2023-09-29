namespace Lexxys;

public static class HashCode
{
	public static int Join(int h1, int h2)
	{
		return ((h1 << 5) + h1) ^ h2;
	}

	public static int Join(int h1, int h2, int h3)
	{
		int h = ((h1 << 5) + h1) ^ h2;
		return ((h << 5) + h) ^ h3;
	}

	public static int Join(int h1, int h2, int h3, int h4)
	{
		int h = ((h1 << 5) + h1) ^ h2;
		h = ((h << 5) + h) ^ h3;
		return ((h << 5) + h) ^ h4;
	}

	public static int Join(int h1, params int[] hh)
	{
		if (hh is null) throw new ArgumentNullException(nameof(hh));

		int h = h1;
		for (int i = 0; i < hh.Length; ++i)
		{
			h = ((h << 5) + h) ^ hh[i];
		}
		return h;
	}

	public static int Join(int offset, IEnumerable<int>? items)
	{
		if (items == null) return offset;
		foreach (int h in items)
		{
			offset += ((offset << 5) + offset) ^ h;
		}
		return offset;
	}

	public static int Join<T>(int offset, IEnumerable<T>? items)
	{
		if (items == null) return offset;
		foreach (var t in items)
		{
			offset += ((offset << 5) + offset) ^ (t?.GetHashCode() ?? 0);
		}
		return offset;
	}
}