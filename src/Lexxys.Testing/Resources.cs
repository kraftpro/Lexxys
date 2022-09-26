using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lexxys.Testing;

public static partial class Resources
{
	public static IDictionary<string, RandItem<string>> Resource { get; } = LoadResources("resource*.json");

	public static RandItem<string> Phone { get; } = R.Concat(R.Pic("###-###-####", "(###) ###-####", "1-###-###-####", "###.###.####") | R.Pic(R.P(5, ""), R.P(" x ###"), R.P(" x ####")));
	public static RandItem<string> PhoneType { get; } = R.V(2, "").Or(4, "mobile").Or(3, "work").Or("home").Or("fax");
	public static RandItem<string> FirstName { get; } = Resource["FirstName"];
	public static RandItem<string> MiddleName { get; } = Resource["MiddleName"];
	public static RandItem<string> LastName { get; } = Resource["LastName"];
	public static RandItem<string> Salutation { get; } = Resource["Salutation"];
	public static RandItem<string> NameSuffix { get; } = Resource["NameSuffix"];
	public static RandItem<string> PersonName { get; } = R.Pic("{Salutation} {FirstName} {LastName} {NameSuffix}");
	public static RandItem<string> Lorems { get; } = Resource["Loremwords"];

	public static RandItem<string> Lorem50 { get; } = R.Lorem(10, 50, Lorems);
	public static RandItem<string> Lorem100 { get; } = R.Lorem(10, 100, Lorems);
	public static RandItem<string> Lorem200 { get; } = R.Lorem(20, 200, Lorems);
	public static RandItem<string> Lorem500 { get; } = R.Lorem(50, 500, Lorems);
	public static RandItem<string> Lorem999 { get; } = R.Lorem(50, 1000, Lorems);
	public static RandItem<string> CompanyName { get; } = Resource["CompanyName"];
	public static RandItem<string> DomainSuffix { get; } = Resource["DomainSuffix"];
	public static RandItem<string> TitleDescriptor { get; } = Resource["TitleDescriptor"];
	public static RandItem<string> TitleLevel { get; } = Resource["TitleLevel"];
	public static RandItem<string> TitleJob { get; } = Resource["TitleJob"];
	public static RandItem<string> OperationSystem { get; } = Resource["OperationSystem"];
	public static RandItem<string> EMail { get; } = R.Concat(o => o?.ToLowerInvariant(),
		R.V(10, "") + R.Concat(R.Lower(1) + FirstName, R.Any("", "", "", ".", "", "-", "_")),
		LastName + FirstName,
		R.V("@"),
		LastName,
		DomainSuffix);
	public static RandItem<string> JobTitle { get; } = R.Concat(R.V("") + TitleDescriptor, R.V("") + TitleLevel, TitleJob);
	public static RandItem<string> Os { get; } = OperationSystem;
	public static RandItem<string> Locale { get; } = Resource["Locale"];
	public static RandItem<string> Version { get; } = R.Concat(R.Int(0, 8, "0"), R.V("."), R.Int(0, 999, "0"), R.V("."), R.Int(0, 99999, "0"), R.V(5, "") + R.Any("-test", "-alpha1", "-alpha4", "-beta1", "-beta2", "-beta3", "-rc", "-rc2", "-oem"));
	public static RandItem<string> MobileDevice { get; } = Resource["MobileDevice"];

	public static RandItem<string> AddressLine1 { get; } = R.Pic("{FirstOrLastName} {StreetSuffix}");
	public static RandItem<string> AddressLine2 { get; } = R.Int(1, 999, R.V(3, "Site 0").Or(9, "Apt. 0")) + R.Int(1, 15, "Floor 0");

	public static RandItem<string> AddressUsCity { get; } = R.Pic("{CityPrefix} {FirstOrLastName}", "{FirstOrLastName} {CitySuffix}", "{CityPrefix} {FirstOrLastName} {CitySuffix}");
	public static RandItem<string> AddressUsStateCode { get; } = Resource["AddressUsState"];
	public static RandItem<string> AddressUsZip { get; } = R.Pic(R.P(5, "####"), R.P("#####-####"));
	public static RandItem<string> MimeType { get; } = Resource["MimeType"];
	public static RandItem<string> Website { get; } = R.Pic("www.{LoremWords}.{TopLevelDomenName}");

	public static RandItem<string> UrlMedia { get; } = Resource["Url"];

	static Resources()
	{
		Resource["FirstOrLastName"] = FirstName + LastName;
	}
}
