// Lexxys Infrastructural library.
// file: Check.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Validation;
using Data;

public static class Check
{
	public const string ConfigSection = "lexxys.validation";
	public const string ConfigReference = ConfigSection + ".reference";

	public static readonly DateTime MinDate = new DateTime(1901, 1, 1);
	public static readonly DateTime MaxDate = new DateTime(2099, 12, 31);

	public static bool ReferenceKey(IDataContext dc, int? value, string table) => value == null || ReferenceKey(dc, value.GetValueOrDefault(), table);

	public static bool ReferenceKey(IDataContext dc, int? value, string table, bool nullable) => value == null ? nullable : ReferenceKey(dc, value.GetValueOrDefault(), table);

	public static bool ReferenceKey(IDataContext dc, int? value, string table, string key) => ReferenceKey(dc, value, table, key, true);

	public static bool ReferenceKey(IDataContext dc, int? value, string table, string key, bool nullable) => value == null ? nullable : ReferenceKey(dc, value.GetValueOrDefault(), table, key);

	public static bool ReferenceKey(IDataContext dc, int value, string table, string? key = null)
	{
		if (dc == null)
			throw new ArgumentNullException(nameof(dc));
		if (table is not { Length: > 0 })
			throw new ArgumentNullException(nameof(table));

		if (value <= 0)
			return false;
		key = key.TrimToNull()?.ToUpperInvariant();
		if (key == "ID")
			key = null;
		return __localCache.Get((table, key, value), o => IsReferenceKey(dc, o.Value, o.Table, o.Key));
	}
	private static readonly LocalCache<(string Table, string? Key, int Value), bool> __localCache = new LocalCache<(string, string?, int), bool>(
		capacity: Config.Current.GetValueInRange(ConfigReference + ":cacheCapacity", 16, 128 * 1024, 8 * 1024),
		timeToLive: Config.Current.GetValueInRange(ConfigReference + ":cacheTimeout", new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0), new TimeSpan(0, 10, 0)));

	private static bool IsReferenceKey(IDataContext dc, int value, string table, string? key = null) => 1 == dc.GetValue<int>((SqlPart)$"select top 1 1 from {Dc.Name(table)} where {Dc.Name(key ?? "ID")}=@I", Dc.Parameter("@I", value));

	public static bool Range<T>(T? value, ValueTuple<T, T>[]? ranges)
		where T : struct, IComparable<T>, IEquatable<T> => value == null || Range(value.GetValueOrDefault(), ranges);

	public static bool Range<T>(T? value, ValueTuple<T, T>[]? ranges, bool nullable)
		where T : struct, IComparable<T>, IEquatable<T> => value == null ? nullable : Range(value.GetValueOrDefault(), ranges);

	public static bool Range<T>(T value, ValueTuple<T, T>[]? ranges)
		where T: struct, IComparable<T>, IEquatable<T>
	{
		if (ranges != null)
		{
			for (int i = 0; i < ranges.Length; ++i)
			{
				if (value.CompareTo(ranges[i].Item1) >= 0 && value.CompareTo(ranges[i].Item2) <= 0)
					return true;
			}
		}
		return false;
	}

	public static bool Range<T>(T? value, T[]? values)
		where T : struct, IEquatable<T> => value == null || Range(value.GetValueOrDefault(), values);

	public static bool Range<T>(T? value, T[]? values, bool nullable)
		where T : struct, IEquatable<T> => value == null ? nullable : Range(value.GetValueOrDefault(), values);

	public static bool Range<T>(T value, T[]? values)
		where T: struct, IEquatable<T>
	{
		if (values == null) return false;
		for (int i = 0; i < values.Length; ++i)
		{
			if (value.Equals(values[i]))
				return true;
		}
		return false;
	}

	public static bool FieldValue<T>(T? value, T min, T max)
		where T : struct, IComparable<T> => FieldValue(value, min, max, true);

	public static bool FieldValue<T>(T? value, T min, T max, bool nullable)
		where T : struct, IComparable<T> => value == null ? nullable : FieldValue(value.GetValueOrDefault(), min, max);

	public static bool FieldValue<T>(T value, T min, T max)
		where T : IComparable<T> => value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;

	public static bool FieldValue(string? value, int length, bool nullable) => value == null ? nullable : (length <= 0 || value.Length <= length);

	public static bool FieldValue(byte[]? value, int length, bool nullable) => value == null ? nullable : (length <= 0 || value.Length <= length);

	public static bool EmailAddress(string? value, int length, bool nullable) => value == null ? nullable : ValueValidator.IsEmail(value, length);

	public static bool PhoneNumber(string? value, int length, bool nullable, bool strict = false) => value == null ? nullable : ValueValidator.IsPhone(value, length, strict);

	public static bool HttpAddress(string? value, int length, bool nullable) => value == null ? nullable : ValueValidator.IsHttpUrl(value, length);

	public static bool EinCode(int? value) => EinCode(value, true);

	public static bool EinCode(int? value, bool nullable) => value == null ? nullable : EinCode(value.GetValueOrDefault());

	public static bool EinCode(int value) => ValueValidator.IsEin(value);

	public static bool EinCode(string? value, int length, bool nullable) => value == null ? nullable : (length <= 0 || value.Length <= length) && ValueValidator.IsEin(value);

	public static bool SsnCode(int? value) => SsnCode(value, true);

	public static bool SsnCode(int? value, bool nullable) => value == null ? nullable : SsnCode(value.GetValueOrDefault());

	public static bool SsnCode(int value) => ValueValidator.IsSsn(value);

	public static bool SsnCode(string? value, bool nullable) => value == null ? nullable : ValueValidator.IsSsn(value);

	public static bool SsnCode(string? value, int length, bool nullable) => value == null ? nullable : (length <= 0 || value.Length <= length) && ValueValidator.IsSsn(value);

	public static bool UsZipCode(string? value, int length, bool nullable) => value == null ? nullable : (length <= 0 || value.Length <= length) && ValueValidator.IsUsZipCode(value);

	public static bool UsStateCode(string? value, bool nullable) => value == null ? nullable : (value = value.Trim()).Length == 2 && UsStateCodes.Value.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
	private static readonly IValue<string> UsStateCodes = Config.Current.GetValue("Lexxys.Check.UsStateCode", "AA AK AL AP AR AS AZ CA CO CT DC DE FL FM GA GU HI IA ID IL IN KS KY LA MA MD ME MI MN MO MP MS MT NC ND NE NH NJ NM NV NY OH OK OR PA PR PW RI SC SD TN TX UT VA VI VT WA WI WV WY");

	public static bool CountryCode(string? value, bool nullable) => value == null ? nullable : (value = value.Trim()).Length == 2 && CountryCodes.Value.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
	private static readonly IValue<string> CountryCodes = Config.Current.GetValue("Lexxys.Check.CountryCode", "AD AE AF AG AI AL AM AN AO AQ AR AS AT AU AW AX AZ BA BB BD BE BF BG BH BI BJ BL BM BN BO BR BS BT BV BW BY BZ CA CC CD CF CG CH CI CK CL CM CN CO CR CU CV CX CY CZ DE DJ DK DM DO DZ EC EE EG EH ER ES ET FI FJ FK FM FO FR GA GB GD GE GF GG GH GI GL GM GN GP GQ GR GS GT GU GW GY HK HM HN HR HT HU ID IE IL IM IN IO IQ IR IS IT JE JM JO JP KE KG KH KI KM KN KP KR KW KY KZ LA LB LC LI LK LR LS LT LU LV LY MA MC MD ME MF MG MH MK ML MM MN MO MP MQ MR MS MT MU MV MW MX MY MZ NA NC NE NF NG NI NL NO NP NR NU NZ OM PA PE PF PG PH PK PL PM PN PR PS PT PW PY QA RE RO RS RU RW SA SB SC SD SE SG SH SI SJ SK SL SM SN SO SR ST SV SY SZ TC TD TF TG TH TJ TK TL TM TN TO TR TT TV TW TZ UA UG UM US UY UZ VA VC VE VG VI VN VU WF WS YE YT ZA ZM ZW");

	public static bool PostalCode(string? value, int length, bool nullable = false) => value == null ? nullable : (length <= 0 || value.Length <= length) && ValueValidator.IsPostalCode(value);

	/// <summary>
	/// Check that field has unique value within the table scope.
	/// </summary>
	/// <param name="dc">Data context</param>
	/// <param name="value">field value to verify</param>
	/// <param name="table">the table where the field should have unique value</param>
	/// <param name="field">name of the field to verify</param>
	/// <param name="keyField">primary key field</param>
	/// <param name="keyValue">value of primary key to skip</param>
	/// <returns>true if the value is unique</returns>
	public static bool UniqueField(IDataContext dc, string value, string table, string field, string? keyField = null, int keyValue = 0)
	{
		if (table is not { Length: > 0 })
			throw new ArgumentNullException(nameof(table));
		if (field is not { Length: > 0 })
			throw new ArgumentNullException(nameof(field));

		var query = new SqlPart($"select top 1 1 from {Dc.Name(table)} where {Dc.Name(field)}{Dc.Equal(value)}");
		if (keyField != null)
			query += (SqlPart)" and " + Dc.Name(keyField) + Dc.NotEqual(keyValue);

		return 0 == dc.GetValue<int>(query);
	}

	public static bool UniqueField(IDataContext dc, int value, string table, string field, string? keyField = null, int keyValue = 0)
	{
		if (table is not { Length: > 0 })
			throw new ArgumentNullException(nameof(table));
		if (field is not { Length: > 0 })
			throw new ArgumentNullException(nameof(field));

		var query = new SqlPart($"select top 1 1 from {Dc.Name(table)} where {Dc.Name(field)}{Dc.Equal(value)}");
		if (keyField != null)
			query += (SqlPart)" and " + Dc.Name(keyField) + Dc.NotEqual(keyValue);

		return 0 == dc.GetValue<int>(query);
	}

	public static bool IsId([NotNullWhen(true)] string? value)
		=> value is not null &&
		(Char.IsDigit(value, 0) && Int32.TryParse(value, out int id) && id > 0) ||
		(Guid.TryParse(value, out var sid) && sid != default);

	public static bool IsId([NotNullWhen(true)] int? value) => value.GetValueOrDefault() > 0;

	public static bool IsId([NotNullWhen(true)] Guid? value) => value.GetValueOrDefault() != default;

	public static bool IsId(int value) => value > 0;

	public static bool Name(string? value, string? specialChars = null, bool nullable = false)
	{
		if (value == null || (value = value.Trim()).Length == 0)
			return nullable;
		if (!Char.IsLetter(value, 0))
			return false;
		specialChars ??= "";
		for (int i = 1; i < value.Length; i++)
		{
			if (!Char.IsLetter(value, i) && specialChars.IndexOf(value[i]) < 0)
				return false;
		}
		return true;
	}
}


