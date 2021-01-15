using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

namespace Lexxys.Test.Con
{
	class JsonTest
	{
		static string Data = @"{""data"":{
""type"":""Contact"",
""id"":""890564"",
""eto"":""null"",
""to"":"""",
""se"":null,
""attributes"":{""last"":""test""},""included"":[{""type"":""contact"",""id"":""890564"",""attributes"":{""xid"":37657,""contextdata"":null,""cid"":35588}}]},""header"":{""action"":""update"",""origin"":""application-1"",""millis"":1565289637981,""session"":{""id"":""1vkm8xrxe0"",""userid"":19,""roleid"":301,""name"":""Jhon Doe"",""type"":[""cs&s"",""contoso"",""contoso/admin""],""adminuserid"":null,""agentuserid"":null,""agentname"":"""",""company"":""Contoso Inc."",""cid"":35588,""account"":1290}}}";

		static string Data2 = @"
{""data"":{""type"":""Contact"",""id"":""890564"",""attributes"":{""addresses"":[{""id"":0,""address1"":""3344"",""address2"":"""",""category"":""Primary"",""city"":""33"",""country"":""US"",""state"":""AK"",""postal"":""33333"",""version"":0,""countryname"":"""",""address"":""3344 ""}],""version"":0},""included"":[{""type"":""contact"",""id"":""890564"",""attributes"":{""xid"":37657,""contextdata"":null,""cid"":35588}}]},""header"":{""action"":""update"",""origin"":""application-1"",""millis"":1565300875827,""session"":{""id"":""1xezzfwi90"",""userid"":19,""roleid"":301,""name"":""Jhon Doe"",""type"":[""cs&s"",""contoso"",""contoso/admin""],""adminuserid"":null,""agentuserid"":null,""agentname"":"""",""company"":""Contoso Inc."",""cid"":35588,""account"":1290}}}
";

		public static void Go()
		{
			var j = JsonParser.Parse(Data);
			var d = j["data"];
			var a = d["attributes"];
			var xIdValue = d["included"][0]["attributes"]["xId"].Text;
			Console.WriteLine(xIdValue);
			var p = PersonRequest.FromJson(JsonParser.Parse(Data2)["data"]["attributes"]);
			Console.WriteLine(p.Addresses.HasValue);
		}

		struct OptValue<T>
		{
			public bool HasValue { get; }
			public T Value { get; }

			public OptValue(bool here, T value)
			{
				HasValue = here;
				Value = value;
			}

			public OptValue(T value)
			{
				HasValue = true;
				Value = value;
			}
		}

		static class OptValue
		{
			public static OptValue<string> Get(JsonItem item)
			{
				return item.IsEmpty ? new OptValue<string>() : new OptValue<string>(item.Text);
			}
			public static OptValue<T> Get<T>(JsonItem item, Func<JsonItem, T> func)
			{
				return item.IsEmpty ? new OptValue<T>() : new OptValue<T>(func(item));
			}
			public static OptValue<List<T>> List<T>(JsonItem item, Func<JsonItem, T> func)
			{
				return item is JsonArray a ? new OptValue<List<T>>(a.Select(o => func(o)).ToList()) : new OptValue<List<T>>();
			}
		}

		private class PersonRequest
		{
			public OptValue<string> NamePrefix { get; set; }
			public OptValue<string> FirstName { get; set; }
			public OptValue<string> LastName { get; set; }
			public OptValue<string> MiddleInitial { get; set; }
			public OptValue<string> NameSuffix { get; set; }
			public OptValue<string> Email { get; set; }
			public OptValue<string> HomePhone { get; set; }
			public OptValue<string> CellPhone { get; set; }
			public OptValue<string> OtherPhone { get; set; }
			public OptValue<string> BusinessPhone { get; set; }
			public OptValue<string> BusinesFax { get; set; }
			public OptValue<List<PostalAddress>> Addresses { get; set; }

			public static PersonRequest FromJson(JsonItem j)
			{
				return new PersonRequest
				{
					NamePrefix = OptValue.Get(j["salutation"]),
					FirstName = OptValue.Get(j["first"]),
					LastName = OptValue.Get(j["last"]),
					MiddleInitial = OptValue.Get(j["middle"]),
					NameSuffix = OptValue.Get(j["suffix"]),
					Email = OptValue.Get(j["email"]),
					HomePhone = OptValue.Get(j["homephone"]),
					CellPhone = OptValue.Get(j["cellphone"]),
					OtherPhone = OptValue.Get(j["otherphone"]),
					BusinessPhone = OptValue.Get(j["businessphone"]),
					BusinesFax = OptValue.Get(j["businesfax"]),
					Addresses = OptValue.List(j["addresses"], o => new PostalAddress
					{
						Address1 = o["address1"].Text,
						Address2 = o["address2"].Text,
						City = o["city"].Text,
						State = o["state"].Text,
						Zip = o["zip"].Text,
						Country = o["country"].Text ?? PostalAddress.DefaultCountryCode,
						Company = o["company"].Text,
						Description = o["description"].Text,
					})
				};
			}
		}

		public class PostalAddress
		{
			public static string DefaultCountryCode { get; internal set; }
			public string Address1 { get; internal set; }
			public string Address2 { get; internal set; }
			public string City { get; internal set; }
			public string State { get; internal set; }
			public string Zip { get; internal set; }
			public object Country { get; internal set; }
			public string Company { get; internal set; }
			public string Description { get; internal set; }
		}

	}
}
