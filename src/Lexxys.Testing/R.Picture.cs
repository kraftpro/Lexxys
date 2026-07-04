using System.Text.RegularExpressions;

namespace Lexxys.Testing;

public static partial class R
{
	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified <paramref name="weight"/> and the specified <paramref name="picture"/> for the item generator.
	/// </summary>
	/// <param name="weight">Weight of the item.</param>
	/// <param name="picture">The picture for the item generator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<string> Picture(double weight, string picture) => new RandItem<string>(GetPicture(weight, picture ?? throw new ArgumentNullException(nameof(picture))));

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified collection of <paramref name="picture"/>s to be randomly selected for the item generator.
	/// </summary>
	/// <param name="picture">Collection of the pictures for the item generator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<string> Picture(IEnumerable<string> picture) => new RandItem<string>(picture.Select(o => GetPicture(1, o)));

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified collection of <paramref name="picture"/>s to be randomly selected for the item generator.
	/// </summary>
	/// <param name="picture">Collection of the pictures for the item generator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<string> Picture(params string[] picture) => new RandItem<string>(Array.ConvertAll(picture, o => GetPicture(1, o)), false);

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified collection of <paramref name="items"/>s to be randomly selected for the item generator.
	/// </summary>
	/// <param name="items">Collection of the weight-pictures pairs for the item generator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<string> Picture(params (double Weight, string Picture)[] items) => new RandItem<string>(Array.ConvertAll(items, o => GetPicture(o.Weight, o.Picture)), false);

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified random item generator <paramref name="item"/> for selecting the picture.
	/// </summary>
	/// <param name="item">Random item generator for selecting the picture.</param>
	/// <returns></returns>
	public static RandItem<string> Picture(RandItem<string> item) => new RandItem<string>(() => ParsePicture(1, item.NextValue()).NextValue());

	private static RandItem<string> GetPicture(double weight, string picture) => ParsePicture(weight, picture);

	private static RandItem<string> ParsePicture(double weight, string picture)
	{
		var items = new List<RandItem<string>>();
		int l = 0;
		var rs = __pic.Replace(picture, Evaluator);
		if (rs == picture)
			return new RandItem<string>(weight, picture);
		if (l < picture.Length)
			items.Add(I(picture.Substring(l)));
		var array = items.ToArray();
		return new RandItem<string>(weight, () => String.Join(null, array));

		string Evaluator(Match m)
		{
			string pad = String.Empty;
			if (m.Index > l)
			{
				string txt = picture.Substring(l, m.Index - l);
				if (txt.EndsWith(' '))
				{
					pad = " ";
					txt = txt.Substring(0, txt.Length - 1);
				}
				if (txt.Length > 0)
					items.Add(I(txt));
			}
			l = m.Index + m.Length;
			string value = m.Value;
			string s = value;
			RandItem<string> item;
			if (!s.StartsWith('{'))
			{
				int len = s.Length;
				item = I(() => pad + new String(R.DigitChar.Collect(len)));
			}
			else
			{
				s = s.Substring(1, s.Length - 2);
				int i = s.IndexOf(':');
				string? f = null;
				double p = 1.0;
				if (i >= 0)
				{
					f = s.Substring(i + 1);
					s = s.Substring(0, i);
				}
				i = s.IndexOf('|');
				if (i >= 0 && double.TryParse(s.Substring(0, i), out p))
					s = s.Substring(0, i + 1);
				item = I(() => Pad(pad, GetResourceItem(p, s, f)));
			}
			items.Add(item);
			return String.Empty;
		}
	}
	private static readonly Regex __pic = new Regex(@"#+|\{[^}]*}");

	private static string GetResourceItem(double probability, string name, string? format)
	{
		return Rand.Dbl() >= probability ? "" :
			!Resources.Resource.TryGetValue(name, out var val) ? "" :
			format == null ? val.ToString() :
			val.ToString(format, null);
	}

	private static string Pad(string pad, string? value)
	{
		if (value == null)
			return "";
		value = value.Trim();
		return value.Length == 0 ? value : pad + value;
	}
}
