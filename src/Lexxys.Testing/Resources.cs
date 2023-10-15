#pragma warning disable CA1724

namespace Lexxys.Testing;

public static partial class Resources
{
	public const string DefaultResourceFile = "resource*.json";
	
	/// <summary>
	/// Returns a <see cref="IDictionary{TKey,TValue}"/> of the random generators loaded from default resource file.
	/// </summary>
	public static IDictionary<string, RandItem<string>> Resource { get; } = LoadResources(DefaultResourceFile);

	public static RandItem<string> Phone { get; } = R.Concat(
		R.Picture("###-###-####", "(###) ###-####", "1-###-###-####", "###.###.####"),
		R.Picture((5, ""),(1, " x ###"), (1, " x ####"))
		);
	public static RandItem<string> PhoneType { get; } = R.I((2, ""), (4, "mobile"), (3, "work"), (1, "home"), (1, "fax"));
	public static RandItem<string> FirstName { get; } = Resource["FirstName"];
	public static RandItem<string> MiddleName { get; } = Resource["MiddleName"];
	public static RandItem<string> LastName { get; } = Resource["LastName"];
	public static RandItem<string> Salutation { get; } = Resource["Salutation"];
	public static RandItem<string> NameSuffix { get; } = Resource["NameSuffix"];
	public static RandItem<string> PersonName { get; } = R.Picture("{Salutation} {FirstName} {LastName} {NameSuffix}");
	public static RandItem<string> Lorem { get; } = Resource["Loremwords"];

	public static RandItem<string> Lorem50 { get; } = R.Text(10, 50, Lorem);
	public static RandItem<string> Lorem100 { get; } = R.Text(10, 100, Lorem);
	public static RandItem<string> Lorem200 { get; } = R.Text(20, 200, Lorem);
	public static RandItem<string> Lorem500 { get; } = R.Text(50, 500, Lorem);
	public static RandItem<string> Lorem999 { get; } = R.Text(50, 1000, Lorem);
	public static RandItem<string> CompanyName { get; } = Resource["CompanyName"];
	public static RandItem<string> DomainSuffix { get; } = Resource["DomainSuffix"];
	public static RandItem<string> TitleDescriptor { get; } = Resource["TitleDescriptor"];
	public static RandItem<string> TitleLevel { get; } = Resource["TitleLevel"];
	public static RandItem<string> TitleJob { get; } = Resource["TitleJob"];
	public static RandItem<string> OperationSystem { get; } = Resource["OperationSystem"];
	public static RandItem<string> EMail { get; } = R.Concat<string>(o => o?.ToLowerInvariant() ?? String.Empty,
		R.I(10, "") | R.Concat(R.Lower(1) | FirstName, R.Any("", "", "", ".", "", "-", "_")),
		LastName | FirstName,
		R.I("@"),
		LastName,
		DomainSuffix);
	public static RandItem<string> JobTitle { get; } = R.Concat(R.I("") | TitleDescriptor, R.I("") | TitleLevel, TitleJob);
	public static RandItem<string> Os { get; } = OperationSystem;
	public static RandItem<string> Locale { get; } = Resource["Locale"];
	public static RandItem<string> Version { get; } = R.Concat(
		R.Fmt(R.Int(0, 10), "0"),
		R.Fmt(R.Int(0, 1000), "\\.0"),
		R.Fmt(R.Int(0, 1000), "\\.0"),
		R.I(5, "") | R.Fmt(R.Int(1, 65536), "\\.0"),
		R.I(5, "") | R.Any("-test", "-alpha1", "-alpha2", "-beta1", "-beta2", "-beta3", "-rc", "-rc2", "-oem"));
	public static RandItem<string> MobileDevice { get; } = Resource["MobileDevice"];

	public static RandItem<string> AddressLine1 { get; } = R.Picture("{FirstOrLastName} {StreetSuffix}");
	public static RandItem<string> AddressLine2 { get; } = R.Fmt(R.Int(1, 999), R.I(3, "Site 0").Or(9, "Apt. 0")) | R.Fmt(R.Int(1, 15), "Floor 0");

	public static RandItem<string> AddressUsCity { get; } = R.Picture("{CityPrefix} {FirstOrLastName}", "{FirstOrLastName} {CitySuffix}", "{CityPrefix} {FirstOrLastName} {CitySuffix}");
	public static RandItem<string> AddressUsStateCode { get; } = Resource["AddressUsState"];
	public static RandItem<string> AddressUsZip { get; } = R.Picture((5, "####"), (1, "#####-####"));
	public static RandItem<(string City, string State)> AddressUsCityState { get; } = GetUsCityState();

	public static RandItem<string> MimeType { get; } = Resource["MimeType"];
	public static RandItem<string> Website { get; } = R.Picture("www.{LoremWords}.{TopLevelDomainName}");

	public static RandItem<string> UrlMedia { get; } = Resource["Url"];

	private static RandItem<(string City, string State)> GetUsCityState()
	{
		var r = Resource["AddressUsCityState"];
		return r.IsEmpty ? RandItem<(string City, string State)>.Empty :
			new RandItem<(string City, string State)>(() =>
			{
				var s = r.NextValue();
				var p = s.LastIndexOf(',');
				return p < 0 ? (s, String.Empty) : (s.AsSpan().Slice(0, p).TrimEnd().ToString(), s.AsSpan(p + 1).TrimStart().ToString());
			});
	}

	static Resources()
	{
		Resource["FirstOrLastName"] = FirstName | LastName;
	}
}
