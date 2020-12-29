using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Json
{
	[TestClass]
	public class JsonParserTest
	{
		[TestMethod]
		public void ParseMesssage()
		{
			var message = @"{""data"":{""type"":""User"",""id"":""423050"",""attributes"":{""emails"":[{""id"":423050,""status"":1,""lastactive"":1566925012270,""prevstatus"":null,""category"":""Primary"",""address"":""nathan22@abc.com"",""version"":40}]},""included"":[{""type"":""user"",""id"":""423050"",""attributes"":{""addresses"":[{""address1"":""9994 Post Road"",""address2"":""Suite 1452"",""category"":""Primary"",""city"":""Portland"",""contextdata"":null,""country"":""US"",""id"":423050,""postal"":""04101"",""state"":""ME"",""version"":41}],""authid"":75521,""contextdata"":[{""one"":""Contact"",""two"":""423050""},{""one"":""User"",""two"":""423050""},{""one"":""Foundation"",""two"":""36094""}],""created"":""02/09/2011"",""emails"":[{""address"":""nathan22@abc.com"",""category"":""Primary"",""contextdata"":null,""id"":423050,""lastactive"":""08/27/2019"",""prevstatus"":null,""status"":1,""version"":41}],""extid"":2234,""first"":""Nathan"",""gaid"":-1824208785,""last"":""Smith.FJFA"",""lastactive"":""08/27/2019"",""middle"":null,""organization"":null,""phones"":[{""category"":""Mobile"",""contextdata"":null,""id"":423050,""lastactive"":""08/27/2019"",""number"":""2032225555"",""prevstatus"":null,""status"":1,""version"":41}],""prevstatus"":null,""salutation"":""Mr."",""status"":1,""suffix"":null,""title"":null,""types"":""foundation"",""version"":41}}]},""header"":{""action"":""update"",""origin"":""management-1"",""millis"":1566925013995,""session"":{""id"":""0grcqjb090"",""userid"":423050,""roleid"":204549,""name"":""Nathan Smith.FJFA"",""type"":[""foundation""],""adminuserid"":2234,""adminsessionid"":""ORrQZwtvgyJW7AbIPR9nmFYn"",""agentuserid"":null,""agentname"":"""",""foundation"":""The MFS Foundation 232        "",""foundationid"":36094,""account"":232}}}";
			var j = JsonParser.Parse(message);
			Assert.AreEqual(message, j.ToString());
		}
	}
}
