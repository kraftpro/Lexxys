using Lexxys.Configuration;
using Lexxys.Tokenizer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Lexxys.Tests.Configuration;

[TestClass]
public class CfgParserTest
{

	[TestMethod]
	public void ParseSimpleCfgTest()
	{
		string config = """
			school
				name I.K. High School	# line comment
				address
					street 123 Main Street#2 # part of the street
					city Anytown
					state NY
					"zip" 12345 # name in quotes
				buildings
					item
						address
							street "123a Main Street #2" # string
							city Anytown +
							state NY 2
							zip "1234900 "
						area 1000
						name Main Building
			""";
		var nodes = Lexxys.Configuration.CfgParser.ParseConfig(config);
		Assert.AreEqual(1, nodes.Count);
		var node = nodes[0];
		Assert.AreEqual("school", node.Name);
		Assert.AreEqual(3, node.Items.Count);
		var name = node.Items.FirstOrDefault(x => x.Name == "name");
		var address = node.Items.FirstOrDefault(x => x.Name == "address");
		var buildings = node.Items.FirstOrDefault(x => x.Name == "buildings");
		Assert.IsNotNull(name);
		Assert.IsNotNull(address);
		Assert.IsNotNull(buildings);
		Assert.AreEqual("I.K. High School", name.Value);
		Assert.AreEqual(4, address.Items.Count);
		Assert.AreEqual(1, buildings.Items.Count);
		Assert.AreEqual("street", address.Items[0].Name);
		Assert.AreEqual("city", address.Items[1].Name);
		Assert.AreEqual("state", address.Items[2].Name);
		Assert.AreEqual("zip", address.Items[3].Name);
		Assert.AreEqual("123 Main Street#2", address.Items[0].Value);
		Assert.AreEqual("Anytown", address.Items[1].Value);
		Assert.AreEqual("NY", address.Items[2].Value);
		Assert.AreEqual("12345", address.Items[3].Value);
		var item = buildings.Items[0];
		Assert.AreEqual(3, item.Items.Count);
		Assert.AreEqual("address", item.Items[0].Name);
		Assert.AreEqual("area", item.Items[1].Name);
		Assert.AreEqual("name", item.Items[2].Name);
		var address2 = item.Items[0];
		Assert.AreEqual(4, address2.Items.Count);
		Assert.AreEqual("street", address2.Items[0].Name);
		Assert.AreEqual("city", address2.Items[1].Name);
		Assert.AreEqual("state", address2.Items[2].Name);
		Assert.AreEqual("zip", address2.Items[3].Name);
		Assert.AreEqual("123a Main Street #2", address2.Items[0].Value);
		Assert.AreEqual("Anytown +", address2.Items[1].Value);
		Assert.AreEqual("NY 2", address2.Items[2].Value);
		Assert.AreEqual("1234900 ", address2.Items[3].Value);
		Assert.AreEqual("1000", item.Items[1].Value);
		Assert.AreEqual("Main Building", item.Items[2].Value);
	}

	[TestMethod]
	public void ParseAttributedCfgTest()
	{
		string config = """
			school:
				name:I.K. High School
				`address
					street: 123 Main Street
					city: Anytown
					state: NY
					"zip": 12345
				buildings =
					item
						address:
							street: 123a Main Street
							city: Anytown +
							state:: NY
							zip: 123 400
						area: 1000
						name: Main Building
			""";
		var nodes = Lexxys.Configuration.CfgParser.ParseConfig(config);
		Assert.AreEqual(1, nodes.Count);
		var node = nodes[0];

		Assert.AreEqual("school", node.Name);
		Assert.AreEqual(3, node.Items.Count);
		var name = node.Items.FirstOrDefault(x => x.Name == "name");
		var address = node.Items.FirstOrDefault(x => x.Name == "address");
		var buildings = node.Items.FirstOrDefault(x => x.Name == "buildings");
		Assert.IsNotNull(name);
		Assert.IsNotNull(address);
		Assert.IsNotNull(buildings);
		Assert.AreEqual("I.K. High School", name.Value);
		Assert.AreEqual(4, address.Items.Count);
		Assert.AreEqual(1, buildings.Items.Count);
		Assert.AreEqual("street", address.Items[0].Name);
		Assert.AreEqual("city", address.Items[1].Name);
		Assert.AreEqual("state", address.Items[2].Name);
		Assert.AreEqual("zip", address.Items[3].Name);
		Assert.AreEqual("123 Main Street", address.Items[0].Value);
		Assert.AreEqual("Anytown", address.Items[1].Value);
		Assert.AreEqual("NY", address.Items[2].Value);
		Assert.AreEqual("12345", address.Items[3].Value);
		var item = buildings.Items[0];
		Assert.AreEqual(3, item.Items.Count);
		Assert.AreEqual("address", item.Items[0].Name);
		Assert.AreEqual("area", item.Items[1].Name);
		Assert.AreEqual("name", item.Items[2].Name);
		var address2 = item.Items[0];
		Assert.AreEqual(4, address2.Items.Count);
		Assert.AreEqual("street", address2.Items[0].Name);
		Assert.AreEqual("city", address2.Items[1].Name);
		Assert.AreEqual("state", address2.Items[2].Name);
		Assert.AreEqual("zip", address2.Items[3].Name);
		Assert.AreEqual("123a Main Street", address2.Items[0].Value);
		Assert.AreEqual("Anytown +", address2.Items[1].Value);
		Assert.AreEqual(": NY", address2.Items[2].Value);
		Assert.AreEqual("123 400", address2.Items[3].Value);
		Assert.AreEqual("1000", item.Items[1].Value);
		Assert.AreEqual("Main Building", item.Items[2].Value);

		Assert.IsTrue(node.AttributeMark);
		Assert.IsTrue(name.AttributeMark);
		Assert.IsFalse(address.AttributeMark);
		Assert.IsTrue(address.Items.All(x => x.AttributeMark));
		Assert.IsFalse(buildings.AttributeMark);
		Assert.IsFalse(item.AttributeMark);
		Assert.IsTrue(address2.AttributeMark);
		Assert.IsTrue(address2.Items.All(x => x.AttributeMark));
		Assert.IsTrue(item.Items.All(x => x.AttributeMark));
	}

	[TestMethod]
	public void ParseSimpleCfgToJsonTest()
	{
		string config = """
			school
				name I.K. High School	# line comment
				address
					street 123 Main Street#2 <# part of the street
					multi line comment
					multi line comment #>

					city Anytown
					state NY
					"zip" 12345 # name in quotes
				buildings
					-
						address
							street "123a Main Street #2" # string
							city Anytown <# comment #>plus
							state NY 2
							zip "1234900 "
						area "1000"
						name Main Building
			""";
		string js = """
			{
			  "school": {
			    "name": "I.K. High School",
			    "address": {
			      "street": "123 Main Street#2",
			      "city": "Anytown",
			      "state": "NY",
			      "zip": 12345
			    },
			    "buildings": [
			      {
			        "address": {
			          "street": "123a Main Street #2",
			          "city": "Anytown plus",
			          "state": "NY 2",
			          "zip": "1234900 "
			        },
			        "area": "1000",
			        "name": "Main Building"
			      }
			    ]
			  }
			}
			""";
		var obj = System.Text.Json.JsonSerializer.Deserialize<object>(js);
		var expected = System.Text.Json.JsonSerializer.Serialize<object>(obj);

		var json = CfgParser.ParseToJson(config).ToString();
		Assert.AreEqual(expected.ToString(), json);
	}

	[TestMethod]
	public void ParseSimpleCfgToJson2Test()
	{
		string config = """
			school
				name I.K. High School	# line comment
				buildings
					-
						address
							street "123a Main Street #2" # string
							city Anytown <# comment #>plus
							state NY 2
							zip "1234900 "
						area 1000
						name Main Building
					-	value ignored in json
						address
							state NY
							zip 1234900
						area 1000.10
						name Main Building
					-	1000.23
			""";
		var expected = new JsonStringBuilder()
			.Obj()
				.Item("school").Obj()
					.Item("name", "I.K. High School")
					.Item("buildings").Arr()
						.Obj()
							.Item("address").Obj()
								.Item("street", "123a Main Street #2")
								.Item("city", "Anytown plus")
								.Item("state", "NY 2")
								.Item("zip", "1234900 ")
							.End()
							.Item("area", 1000)
							.Item("name", "Main Building")
						.End()
						.Obj()
							.Item("address").Obj()
								.Item("state", "NY")
								.Item("zip", 1234900)
							.End()
							.Item("area", 1000.10m)
							.Item("name", "Main Building")
						.End()
						.Val(1000.23)
					.End()
				.End()
			.End();

		var json = CfgParser.ParseToJson(config).ToString();
		Assert.AreEqual(expected.ToString(), json);
	}

	[TestMethod]
	public void CanParseOptions()
	{
		string source = """
			# comment
			% $value1 = 123.01
			% $value2 = 123,01
			% $ value3 = "string value" <# comment
				#> 44
			% $value4 = [1,2, 3, <#4,#> 5]
			% $value5 = {a:1, b:2, c:3, d= [ one, two ]}
			% $value6 = (
				a = 1;
				b = 2;
				c = 3;
				d = [ one, two:three, four five ];
				)
			""";
		var parser = new CfgParser();
		var text = new CharStream(source);
		var nodes = parser.ParseNodeList(ref text);
		Assert.IsNotNull(nodes);
		Assert.AreEqual(0, nodes.Count);
		Assert.AreEqual(6, parser.Options.Variable.Count);
		string actual;
		actual = parser.Options.GetVariableText("value1");
		Assert.AreEqual("123.01", actual);
		actual = parser.Options.GetVariableText("value2");
		Assert.AreEqual("123, 01", actual);
		actual = parser.Options.GetVariableText("value3");
		Assert.AreEqual("\"string value\", 44", actual);
		actual = parser.Options.GetVariableText("value4");
		Assert.AreEqual("[1, 2, 3, 5]", actual);
		actual = parser.Options.GetVariableText("value5");
		Assert.AreEqual("{a: 1, b: 2, c: 3, d= [one, two]}", actual);
		actual = parser.Options.GetVariableText("value6");
		Assert.AreEqual("{a: 1, b: 2, c: 3, d: [one, two:three, four, five]}", actual);
	}

	[TestMethod]
	public void CanSubstituteMacro()
	{
		string source = """
			% $name = api
			% $root = https://contoso.com
			% $api = ${{root}}/api
			% $count = 15
			% $items = [1,2, 3, <#4,#> 5]

			config
				${{name}}: ${{api}}
				count "${{count}}"
				items ${{items}}
				root ${{root}}
			""";
		var parser = new CfgParser();
		var text = new CharStream(source);
		var nodes = parser.ParseNodeList(ref text);

		var actual = parser.Options.GetVariableText("root");
		Assert.AreEqual("https://contoso.com", actual);
		actual = parser.Options.GetVariableText("api");
		Assert.AreEqual("${{root}}/api", actual);
		Assert.AreEqual(1, nodes.Count);
		var config = nodes[0];
		Assert.AreEqual(4, config.Items.Count);
		var api = config.Items[0];
		Assert.AreEqual("api", api.Name);
		Assert.AreEqual("${{api}}", api.Value);
		var count = config.Items[1];
		Assert.AreEqual("count", count.Name);
		Assert.AreEqual("15", count.Value);
		var items = config.Items[2];
		Assert.AreEqual("items", items.Name);
		Assert.AreEqual("${{items}}", items.Value);
		var root = config.Items[3];
		Assert.AreEqual("root", root.Name);
		Assert.AreEqual("https://contoso.com", root.Value);
	}
}
