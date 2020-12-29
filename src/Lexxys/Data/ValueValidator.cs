// Lexxys Infrastructural library.
// file: ValueValidator.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// TODO: Copied from Lexxys.Data

namespace Lexxys.Data
{
	public static class ValueValidator
	{
		#region URL Validators
		public static bool IsUrl(string value, int maxLength)
		{
			return value != null && value.Length <= maxLength && UrlValueValidator.IsLegalHttpFtpUrl(value);
		}

		public static bool IsUrl(string value)
		{
			return value != null && UrlValueValidator.IsLegalHttpFtpUrl(value);
		}

		public static bool IsHttpUrl(string value, int maxLength)
		{
			return value != null && value.Length <= maxLength && UrlValueValidator.IsLegalHttpUrl(value);
		}

		public static bool IsHttpUrl(string value)
		{
			return value != null && UrlValueValidator.IsLegalHttpUrl(value);
		}

		#endregion

		#region SSN Validators
		private static readonly Regex _validDbSsn = new Regex(@"\A\d{4}(\d{5})?\z", RegexOptions.Singleline);
		private static readonly Regex _invalidDbSsn = new Regex(@"\A((000|666|9\d\d)\d{6}|\d{3}00\d{4}|\d*0000)\z", RegexOptions.Singleline);

		public static bool IsSsn(string value)
		{
			return value != null && _validDbSsn.IsMatch(value) && !_invalidDbSsn.IsMatch(value);
		}

		public static bool IsSsn(int value)
		{
			return value > 0 && value < 999999999 && IsSsn(value.ToString("000000000"));
		}

		#endregion

		#region Zip Validators
		private static readonly Regex _validDbUsZipCode = new Regex(@"\A\d{5}(\d{4})?\z", RegexOptions.Singleline);
		private static readonly Regex _invalidDbUsZipCode = new Regex(@"\A0{5}(\d{4})?\z", RegexOptions.Singleline);
		private static readonly Regex _validPostalCode = new Regex(@"^[a-zA-Z0-9]([a-zA-Z0-9 -]*[a-zA-Z0-9])?$");

		public static bool IsUsZipCode(string value)
		{
			return (value != null) && _validDbUsZipCode.IsMatch(value) && !_invalidDbUsZipCode.IsMatch(value);
		}

		public static bool IsUsZipCode(int value)
		{
			return value >= 999 && value < 999999999 && (value > 99999 ? IsUsZipCode(value.ToString("000000000")): IsUsZipCode(value.ToString("00000")));
		}

		public static bool IsPostalCode(string value)
		{
			return value != null && _validPostalCode.IsMatch(value);
		}
		#endregion

		#region Phone Validators
		//  1234567890 format only
		private static readonly Regex _validDbPhone = new Regex(@"\A\d{10}([xX].{2,10})?\z");
		private static readonly Regex _invalidDbPhone0 = new Regex(@"\A0{10}");
		private static readonly Regex _invalidDbPhone1 = new Regex(@"\A[01]\d{9}");

		public static bool IsPhone(string value, int maxLength, bool strict = false)
		{
			return value != null && (maxLength <= 0 || value.Length <= maxLength) && _validDbPhone.IsMatch(value) && !(strict ? _invalidDbPhone1: _invalidDbPhone0).IsMatch(value);
		}

		public static bool IsPhone(string value, bool strict = false)
		{
			return value != null && _validDbPhone.IsMatch(value) && !(strict ? _invalidDbPhone1: _invalidDbPhone0).IsMatch(value);
		}

		#endregion

		#region Email Validators
		public static bool IsEmail(string value, int maxLength)
		{
			return value != null && (maxLength <= 0 || value.Length <= maxLength) && UrlValueValidator.IsLegalEmailAddress(value);
		}

		public static bool IsEmail(string value)
		{
			return value != null && UrlValueValidator.IsLegalEmailAddress(value);
		}

		#endregion

		#region EIN Validators
		private static readonly Regex _validDbEin = new Regex(@"\A(0[1-9]|[1-9]\d)\d{7}\z", RegexOptions.Singleline);

		public static bool IsEin(string value)
		{
			return value != null && _validDbEin.IsMatch(value);
		}

		public static bool IsEin(int value)
		{
			return value >= 10000000 && value <= 999999999;
		}
		#endregion

		//#region Range Validators
		//public static bool IsValueInRange<T1, T2>(T1 value, T2 min, T2 max) where T1: IComparable<T2>
		//{
		//    return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
		//}

		//public static bool IsValueInList<T1, T2>(T1 value, IEnumerable<T2> validValues) where T1: IEquatable<T2>
		//{
		//    foreach (T2 v in validValues)
		//    {
		//        if (value.Equals(v))
		//            return true;
		//    }
		//    return false;
		//}

		//public static bool IsInRange<T>(T value, T min, T max, bool allowNull) where T: IField
		//{
		//    if (value.IsNull)
		//        return allowNull;
		//    return (value.CompareTo(min) >= 0) && (value.CompareTo(max) <= 0);
		//}

		//public static bool IsInRange<T>(T value, T min, T max) where T: IField
		//{
		//    return (value.CompareTo(min) >= 0) && (value.CompareTo(max) <= 0);
		//}

		////.?
		//public static bool IsInRange(IntField value, int min, int max, bool allowNull)
		//{
		//    if (value.IsNull)
		//        return allowNull;
		//    return (value.CompareTo(min) >= 0) && (value.CompareTo(max) <= 0);
		//}

		//public static bool IsInRange(IntField value, int min, int max)
		//{
		//    return (value.CompareTo(min) >= 0) && (value.CompareTo(max) <= 0);
		//}

		//public static bool IsInList(IntField value, IEnumerable<int> validValues, bool allowNull)
		//{
		//    if (value.IsNull)
		//        return allowNull;
		//    return IsValueInList(value.GetValue(), validValues);
		//}

		//public static bool IsInList(IntField value, IEnumerable<int> validValues)
		//{
		//    return IsValueInList(value.GetValue(), validValues);
		//}
		////.?$Range = above("IntField", "int");

		//#region More Ranges
		////.#back($Range, "ByteField", "byte")
		////.=
		////.#back($Range, "ShortField", "short")
		////.=
		////.#back($Range, "LongField", "long")
		////.=
		////.#back($Range, "DoubleField", "double")
		////.=
		////.#back($Range, "FloatField", "float")
		////.=
		////.#back($Range, "DecimalField", "decimal")
		////.=
		////.#back($Range, "MoneyField", "decimal")
		////.=
		////.#back($Range, "DateTimeField", "DateTime")
		////.=
		//#endregion

		//#endregion

		//#region Miscellaneous Simple Validators
		//public static bool IsString(string value, int maxLength)
		//{
		//    return value != null && value.Length <= maxLength;
		//}

		//public static bool IsString(string value, int minLength, int maxLength)
		//{
		//    return value != null && (value.Length >= minLength) && (maxLength > 0 && value.Length <= maxLength);
		//}

		//public static bool IsString(StringField value, int maxLength, bool allowNull)
		//{
		//    return value.IsNull ? allowNull: IsString(value.GetValue(), maxLength);
		//}

		//public static bool IsString(StringField value, int minLength, int maxLength, bool allowNull)
		//{
		//    return value.IsNull ? allowNull : IsString(value.GetValue(), minLength, maxLength);
		//}

		//public static bool IsString(StringField value, int maxLength)
		//{
		//    return IsString(value.GetValue(), maxLength);
		//}

		//public static bool IsString(StringField value, int minLength, int maxLength)
		//{
		//    return IsString(value.GetValue(), minLength, maxLength);
		//}

		//public static bool IsValue<T>(T value, bool allowNull) where T: IField
		//{
		//    return allowNull || value.HasValue;
		//}

		//public static bool IsValue<T>(T value) where T: IField
		//{
		//    return value.HasValue;
		//}
		//#endregion

		//public static bool IsReference<T>(T value, string tableName, bool allowNull) where T: IField
		//{
		//    if (value.IsNull)
		//        return allowNull;
		//    return IsReference(value, tableName);
		//}

		//[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId="tableName")]
		//public static bool IsReference<T>(T value, string tableName) where T: IField
		//{
		//    // TODO: Add reference validation

		//    long id;
		//    return long.TryParse(value.ToString(), out id) && id > 0;
		//}
	}

	public static class UrlValueValidator
	{
		#region Regular Expressions
		private const string TopLevelDomainNameRex =
			"[a-z]{3,22}" +
			"|xn--[a-z0-9]{4,99}" +
			"|a[cdefgilmnoqrstuwxz]" +
			"|b[abdefghijmnoqrstvwyz]" +
			"|c[acdfghiklmnorsuvwxyz]" +
			"|d[dejkmoz]" +
			"|e[ceghrstu]" +
			"|f[ijkmor]" +
			"|g[abdefghilmnpqrstuwy]" +
			"|h[kmnrtu]" +
			"|i[delmnoqrst]" +
			"|j[emop]" +
			"|k[eghimnprwyz]" +
			"|l[abcikrstuvy]" +
			"|m[acdeghklmnopqrstuvwxyz]" +
			"|n[acefgilopruz]" +
			"|o[m]" +
			"|p[aefghklmnrstwy]" +
			"|q[a]" +
			"|r[eosuw]" +
			"|s[abcdeghijklmnorstuvxyz]" +
			"|t[cdfghjklmnoprtvwz]" +
			"|u[agksyz]" +
			"|v[aceginu]" +
			"|y[etu]" +
			"|w[fsetu]" +
			"|z[amrw]";

		private const string HostNamePart = @"[a-z0-9](?:[a-z0-9_-]*[a-z0-9])?";
		private const string HostNameRex = @"(?<hostname>(?<hosttail>" + HostNamePart + @"\.)(?<hostbody>(:?" + HostNamePart + @"\.)+)?(?<hosthead>" + TopLevelDomainNameRex + "))";
		private const string HostNumberRex = @"(?<hostip>(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4])(?:\.(?:\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-4])){3})";
		private const string DomainAddressRex = @"(?<domain>" + HostNameRex + "|" + HostNumberRex + ")";
		private const string DomainAddressNamedRex = @"(?<domain>" + HostNameRex + ")";

		private const string UserRex = @"(?:(?<user>[a-z0-9]+(?:[\._+~-][a-z0-9]+)*)(?:\:(?<password>(?:[\.a-z0-9!#$&'*+/=?^_`\{|\}~-]|%[0-9a-f][0-9a-f])+))?)";
		private const string PortRex = @"(?:\:(?<port>[1-9]\d{0,3}|[1-5]\d{4}))?";		// from 1 to 59,999

		private const string PathRex = @"(?<path>(?:/(?:[^\x00-\x1F%?#/]|%[0-9a-f][0-9a-f])*)+)?";
		private const string QuestionRex = @"(?:\?(?<query>(?:[^\x00-\x1F#%]|%[0-9a-f][0-9a-f])*))?";
		private const string FragmentRex = @"(?:#(?<fragment>(?:[^\x00-\x1F%]|%[0-9a-f][0-9a-f])*))?";

		//private const string LocalIpRex = @"\A127\.";
		//private const string PrivateIpRex = @"\A(?:10|172\.(?:1[6-9]|2\d|3[01])|192\.168)\.";
		private const string InternalIpRex = @"\A(?:10|127|172\.(?:1[6-9]|2\d|3[01])|192\.168)\.";

		private const string UnexpectedHostNameRex = @"\A(:?ftp|www)[1-9]?\.[a-z]+\z";

		private const string EmailAddressRex = "(?:(?<schema>mailto):)?" + UserRex + "@" + DomainAddressNamedRex;

		private const string BaseUrlRex = "(?:" + UserRex + "@)?" + DomainAddressRex + PortRex + PathRex + QuestionRex + FragmentRex;

		private const string HttpUrlRex = "(?:(?<schema>https?)://)?" + BaseUrlRex;
		private const string FtpUrlRex = "(?:(?<schema>ftps?)://)?" + BaseUrlRex;
		private const string HttpFtpUrlRex = "(?:(?<schema>https?|ftps?)://)?" + BaseUrlRex;

		private static readonly Regex _legalDomainName = new Regex("\\A" + HostNameRex + "\\z", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex _legalEmailAddress = new Regex("\\A" + EmailAddressRex + "\\z", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex _legalHttpUrl = new Regex("\\A" + HttpUrlRex + "\\z", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex _legalFtpUrl = new Regex("\\A" + FtpUrlRex + "\\z", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex _legalHttpFtpUrl = new Regex("\\A" + HttpFtpUrlRex + "\\z", RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private static readonly Regex _internalIp = new Regex(InternalIpRex, RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex _unexpectedHostName = new Regex(UnexpectedHostNameRex, RegexOptions.Singleline | RegexOptions.IgnoreCase);
		#endregion

		public static bool IsDomainName(string value)
		{
			return value != null && _legalDomainName.IsMatch(value);
		}

		public static bool IsEmailAddress(string value)
		{
			return value != null && _legalEmailAddress.IsMatch(value);
		}

		public static bool IsLegalEmailAddress(string value)
		{
			if (value == null)
				return false;
			Match m = _legalEmailAddress.Match(value);

			if (!m.Success)
				return false;

			GroupCollection g = m.Groups;
			return g["hostname"].Success && !_unexpectedHostName.IsMatch(g["hostname"].Value);
		}

		public static bool IsHttpUrl(string value)
		{
			return value != null && _legalHttpUrl.IsMatch(value);
		}

		public static bool IsLegalHttpUrl(string value)
		{
			if (value == null)
				return false;
			Match m = _legalHttpUrl.Match(value);

			if (!m.Success)
				return false;

			GroupCollection g = m.Groups;
			if (g["user"].Success && !g["schema"].Success)
				return false;

			return (g["hostip"].Success) ?
				!_internalIp.IsMatch(g["hostip"].ToString()):
				!_unexpectedHostName.IsMatch(g["hostname"].Value);
		}

		public static bool IsFtpUrl(string value)
		{
			return value != null && _legalFtpUrl.IsMatch(value);
		}

		public static bool IsLegalFtpUrl(string value)
		{
			if (value == null)
				return false;

			Match m = _legalFtpUrl.Match(value);
			if (!m.Success)
				return false;
			
			GroupCollection g = m.Groups;
			if (g["user"].Success && !g["schema"].Success)
				return false;

			return (g["hostip"].Success) ?
				!_internalIp.IsMatch(g["hostip"].ToString()):
				!_unexpectedHostName.IsMatch(g["hostname"].Value);
		}

		public static bool IsHttpFtpUrl(string value)
		{
			return value != null && _legalHttpFtpUrl.IsMatch(value);
		}

		public static bool IsLegalHttpFtpUrl(string value)
		{
			if (value == null)
				return false;

			Match m = _legalHttpFtpUrl.Match(value);
			if (!m.Success)
				return false;

			GroupCollection g = m.Groups;
			if (g["user"].Success && !g["schema"].Success)
				return false;

			return (g["hostip"].Success) ?
				!_internalIp.IsMatch(g["hostip"].ToString()):
				!_unexpectedHostName.IsMatch(g["hostname"].Value);
		}
	}
}


