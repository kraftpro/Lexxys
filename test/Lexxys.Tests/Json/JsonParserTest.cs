using System;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Json
{
	[TestClass]
	public class JsonParserTest
	{
		[TestMethod]
		[DataRow(/*lang=json*/"""
			{
				"tokens": {
					"true": true,
					"false": false,
					"falseStr": "false",
					"null": null,
					"nullStr": "null"
				},
				"number": {
					"int.max": 2147483647,
					"int.min": -2147483648,
					"long.max": 9223372036854775807,
					"long.min":-9223372036854775808,
					"dec.max":79228162514264337593543950335,
					"dec.min":-79228162514264337593543950335,
					"dec.1":792281625142643.37593543950335,
					"dec.2":-7922816.2514264337593543950335,
					"dec.max2":7.92281625142643E+28,
					"dec.max3":7.92281625142643E+34,
					"nan": NaN
				},
				"str": {
					"name": "Name",
					"trim": " trim  ",
					"nl": " Line 1\n\tLine 2\r\n . Line 3\n\n "
				}
			}
			""")]
		[DataRow(/*lang=json,strict*/ """
			{
				"data": {
					"type": "User",
					"id": "423050",
					"attributes": {
						"emails": [
							{
								"id":423050,
								"status": 1,
								"lastactive": 1566925012270,
								"prevstatus": null,
								"category": "Primary",
								"address": "nathan22@abc.com",
								"version": 40
							}
						]
					},
					"included": [
						{
							"type": "user",
							"id": "423050",
							"attributes": {
								"addresses": [
									{
										"address1": "9994 Post Road",
										"address2": "Suite 1452",
										"category": "Primary",
										"city":	"Portland",
										"contextdata": null,
										"country": "US",
										"id": 423050,
										"postal": "04101",
										"state": "ME",
										"version": 41
									}
								],
								"authid": 75521,
								"contextdata": [
									{
										"one": "Contact",
										"two": "423050"
									},
									{
										"one": "User",
										"two": "423050"
									},
									{
										"one": "Company",
										"two": "36094"
									}
								],
								"created": "02/09/2011",
								"emails": [
									{
										"address": "nathan22@abc.com",
										"category": "Primary",
										"contextdata": null,
										"id": 423050,
										"lastactive": "08/27/2019",
										"prevstatus": null,
										"status": 1,
										"version": 41}],
										"extid": 2234,
										"first": "Nathan",
										"gaid": -1824208785,
										"last": "Smith.FJFA",
										"lastactive": "08/27/2019",
										"middle": null,
										"organization": null,
										"phones": [
											{
												"category": "Mobile",
												"contextdata": null,
												"id": 423050,
												"lastactive": "08/27/2019",
												"number": "2032225555",
												"prevstatus": null,
												"status": 1,
												"version": 41
											}
										],
										"prevstatus": null,
										"salutation": "Mr.",
										"status": 1,
										"suffix": null,
										"title": null,
										"types": "company",
										"version": 41
									}
								}
							]
						},
						"header": {
							"action": "update",
							"origin": "management-1",
							"millis": 1566925013995,
							"session": {"id": "0grcqjb090",
							"userid": 423050,
							"roleid": 204549,
							"name": "Nathan Smith.FJFA",
							"type": ["company"],
							"adminuserid": 2234,
							"adminsessionid": "ORrQZwtvgyJW7AbIPR9nmFYn",
							"agentuserid": null,
							"agentname": "",
							"application": "Application 232        ",
							"applicationid": 36094,
							"account": 232
						}
					}
				}
								
			""")]
		public void ParseString(string json)
		{
			var j = JsonParser.Parse(json);
			string actual = json;
			actual = Regex.Replace(actual, @"\s*{\s*", "{", RegexOptions.Singleline);
			actual = Regex.Replace(actual, @"\s*}\s*", "}", RegexOptions.Singleline);
			actual = Regex.Replace(actual, @"\s*:\s*", ":", RegexOptions.Singleline);
			actual = Regex.Replace(actual, @"\s*,\s*", ",", RegexOptions.Singleline);
			Assert.AreEqual(actual, j.ToString());
		}
	}
}
