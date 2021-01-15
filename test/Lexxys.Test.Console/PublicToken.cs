using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
//using System.Web.Script.Serialization;
using Lexxys;
using Lexxys.Crypting;

namespace Lexxys.Test.Con
{
	public static class TokenTest
	{
		public static void Go()
		{
			var a = new PublicToken(1, SixBitsCoder.GenerateSessionId(), PublicTokenScope.Private, TimeSpan.FromMinutes(10), "view.documents", 94, @"c:\documents\11\22\101122345-wiouyer.pdf");
			var token = a.ToTokenValue();
			var b = PublicToken.FromTokenValue(token);
			var d = new Document {Id = 123, Name = "Name of the doc;;sw;tr", Extension = null, StorageLocation = @"c:\documents\11\22\101122345-wiouyer.pdf", OwnerId = 123};
			//var t1 = d.GetAccessToken(PublicTokenScope.Protected, "found/me");
			var p1 = d.GetAccessToken2(PublicTokenScope.Protected, "found/me");
			d.Extension = "pdf";
			d.Name = null;
			//var t2 = d.GetAccessToken(PublicTokenScope.Protected, "found/me", TimeSpan.FromSeconds(120));
			var p2 = d.GetAccessToken2(PublicTokenScope.Protected, "found/me", TimeSpan.FromSeconds(120));
			//var tt1 = t1.ToTokenValue();
			//var tt2 = t2.ToTokenValue();
			var tp1 = p1.ToTokenValue();
			var tp2 = p2.ToTokenValue();
			//var st1 = Document.SplitAccessToken(t1);
			//var st2 = Document.SplitAccessToken(t2);
			var sp1 = Document.SplitAccessToken2(p1);
			var sp2 = Document.SplitAccessToken2(p2);

			return;
		}
	}

	class Document
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Extension { get; set; }
		public string StorageLocation { get; set; }
		public int OwnerId { get; set; }

		//public PublicToken GetAccessToken(PublicTokenScope scope, string authorizationKey = null, TimeSpan timeout = default)
		//{
		//	var jss = new JavaScriptSerializer();
		//	var data = jss.Serialize(new object[] { Id, Name, Extension, StorageLocation });
		//	return new PublicToken(scope, timeout, authorizationKey, OwnerId, data);
		//}

		public PublicToken GetAccessToken2(PublicTokenScope scope, string authorizationKey = null, TimeSpan timeout = default)
		{
			return new PublicToken(scope, timeout, authorizationKey, OwnerId, $"{Id};{Pack(Name)};{Pack(Extension)};{Pack(StorageLocation)}");
		}

		private static string Pack(string value)
		{
			return String.IsNullOrEmpty(value) ? "." : value.Replace(";", ";;");
		}

		//public static (int Id, string Name, string Extension, string Location) SplitAccessToken(PublicToken token)
		//{
		//	if (String.IsNullOrEmpty(token?.Data))
		//		return default;
		//	try
		//	{
		//		var jss = new JavaScriptSerializer();
		//		var tt = jss.Deserialize<object[]>(token.Data);
		//		return tt.Length != 4 ? default: ((int?)tt[0] ?? 0, (string)tt[1], (string)tt[2], (string)tt[3]);
		//	}
		//	catch
		//	{
		//		return default;
		//	}
		//}

		public static (int Id, string Name, string Extension, string Location) SplitAccessToken2(PublicToken token)
		{
			if (String.IsNullOrEmpty(token?.Data))
				return default;

			var tt = token.Data.Split(';');
			int id = tt[0].AsInt32(0);
			if (tt.Length < 2)
				return (id, null, null, null);
			int i = 1;
			string name = tt[i];
			while (++i < tt.Length && tt[i].Length == 0)
			{
				name += ";";
				if (++i < tt.Length)
					name += tt[i];
			}
			if (name == ".")
				name = null;
			if (i >= tt.Length)
				return (id, name, null, null);

			string extension = tt[i];
			while (++i < tt.Length && tt[i].Length == 0)
			{
				extension += ";";
				if (++i < tt.Length)
					extension += tt[i];
			}
			if (extension == ".")
				extension = null;
			if (i >= tt.Length)
				return (id, name, extension, null);

			string location = tt[i];
			while (++i < tt.Length && tt[i].Length == 0)
			{
				location += ";";
				if (++i < tt.Length)
					location += tt[i];
			}
			if (location == ".")
				location = null;
			return (id, name, extension, location);
		}

	}


	public enum PublicTokenScope
	{
		Private,
		Protected,
		Public,
	}

	public class PublicToken
	{
		public int UserId { get; }
		public string SessionId { get; }
		public PublicTokenScope Scope { get; }
		public DateTime DateCreated { get; }
		public TimeSpan Timeout { get; }
		public DateTime? DateExpired => Timeout == default ? (DateTime?)null : DateCreated + Timeout;
		/// <summary>AKA Authorization Key / Resource Code</summary>
		public string Key { get; }
		/// <summary>Client ID</summary>
		public int? ClientId { get; }
		public string Data { get; }

		public PublicToken(int userId, string sessionId, PublicTokenScope scope, TimeSpan timeOut = default, string authorizationKey = default, int? clientId = default, string data = default)
			: this(userId, sessionId, scope, DateTime.UtcNow, timeOut, authorizationKey, clientId, data)
		{
		}

		public PublicToken(PublicTokenScope scope, TimeSpan timeOut = default, string authorizationKey = default, int? clientId = default, string data = default)
			: this(1, "Hz.qNJ9OhmognJCvOtlQZduK", scope, DateTime.UtcNow, timeOut, authorizationKey, clientId, data)
		{
		}

		private PublicToken(int userId, string sessionId, PublicTokenScope scope, DateTime dateCreated, TimeSpan timeOut, string authorizationKey = default, int? clientId = default, string data = default)
		{
			if (sessionId != null && sessionId.IndexOf(';') >= 0)
				throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, null);
			if (authorizationKey != null && authorizationKey.IndexOf(';') >= 0)
				throw new ArgumentOutOfRangeException(nameof(authorizationKey), authorizationKey, null);
			UserId = userId;
			SessionId = sessionId;
			DateCreated = dateCreated;
			Scope = scope;
			Timeout = timeOut;
			Key = authorizationKey;
			ClientId = clientId;
			Data = data;
		}

		public override string ToString()
		{
			return new StringBuilder()
				.Append(UserId).Append(';')
				.Append(SessionId).Append(';')
				.Append((int)Scope).Append(';')
				.Append(DateCreated.ToUnixTime()).Append(';')
				.Append(Timeout.Ticks / TimeSpan.TicksPerSecond).Append(';')
				.Append(Key).Append(';')
				.Append(ClientId).Append(';')
				.Append(Data)
				.ToString();
		}

		private static PublicToken FromString(string value)
		{
			var items = value.Split(_semic, 8);
			return items.Length != 8 ? null: new PublicToken(
				userId: items[0].AsInt32(0),
				sessionId: items[1].TrimToNull(),
				scope: items[2].AsEnum<PublicTokenScope>(default),
				dateCreated: items[3].AsInt64(0).FromUnixTime(),
				timeOut: TimeSpan.FromSeconds(items[4].AsInt64(0)),
				authorizationKey: items[5].TrimToNull(),
				clientId: items[6].AsInt32(null),
				data: items[7].TrimToNull()
				);
		}
		private static readonly char[] _semic = new[] { ';' };

		private static readonly byte[] TokenKey = new MD5CryptoServiceProvider() // 16 bytes
			.ComputeHash(Encoding.UTF8.GetBytes("One Man had 5 tabby gabby black cats!"));

		public string ToTokenValue()
		{
			var s = ToString();
			var des = Crypto.Encryptor("TripleDes", TokenKey);
			return SixBitsCoder.Encode(des.EncryptString(s, Encoding.UTF8));
		}

		public static PublicToken FromTokenValue(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			var bits = SixBitsCoder.Decode(value);
			if (bits == null)
				return null;
			try
			{
				var des = Crypto.Decryptor("TripleDes", TokenKey);
				var s = des.DecryptString(bits, Encoding.UTF8);
				return FromString(s);
			}
			catch (CryptographicException)
			{
				return null;
			}
		}
	}
}
