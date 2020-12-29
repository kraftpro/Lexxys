using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;

using Lexxys.Testing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Lexxys.Test.Con.Core.Xml
{
	[MemoryDiagnoser]
	//[InliningDiagnoser(true, true)]
	[TailCallDiagnoser]
	//[EtwProfiler]
	public class ParseTest
	{
		static string XmlValue = GetContent();
		static XDocument XDoc = XDocument.Load(new StringReader(XmlValue));
		static Lexxys.Xml.XmlLiteNode XmlNode = Lexxys.Xml.XmlLiteNode.FromXml(XmlValue);

		public static BenchmarkDotNet.Reports.Summary Go()
		{
			return BenchmarkRunner.Run<ParseTest>();
		}

		public static int Go3(int count)
		{
			int n = 0;
			for (int i = 0; i < count; ++i)
			{
				n += (int)ParseResultsXl(XmlValue).ImageRotation;
			}
			return n;
		}

		public static void Go2()
		{
			var a = ParseResults(XmlValue);
			var b = ParseResultsXd(XmlValue);
			var c = ParseResultsXl(XmlValue);
			var d = ParseResultsXl2(XmlValue);

			Console.WriteLine($"a = a ? {Object.Equals(a, a)}");
			Console.WriteLine($"a = b ? {Object.Equals(a, b)}");
			Console.WriteLine($"a = c ? {Object.Equals(a, c)}");
			Console.WriteLine($"a = d ? {Object.Equals(a, d)}");
			Console.WriteLine();
			Console.WriteLine($"b = b ? {Object.Equals(b, b)}");
			Console.WriteLine($"b = c ? {Object.Equals(b, c)}");
			Console.WriteLine($"b = d ? {Object.Equals(b, d)}");
		}


		//[Benchmark]
		public ExtractedData UseXmlDocument() => ParseResults(XmlValue);
		[Benchmark]
		public ExtractedData UseXDocument() => ParseResultsXd(XmlValue);
		[Benchmark]
		public ExtractedData UseXmlLite() => ParseResultsXl(XmlValue);
		//[Benchmark]
		public ExtractedData UseXmlLiteLinq() => ParseResultsXl2(XmlValue);

		//[Benchmark]
		public ExtractedData UseXDocument0() => ParseResultsXd0(XDoc);
		//[Benchmark]
		public ExtractedData UseXmlLite0() => ParseResultsXl0(XmlNode);

		private static ExtractedData ParseResults(string resultUrl)
		{
			if (String.IsNullOrEmpty(resultUrl))
				return default;

			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(resultUrl);

			var data = new ExtractedData();

			#region extract image rotation

			var businessCardNode = xmlDoc.GetElementsByTagName("businessCard").Item(0);
			var imageRotation = businessCardNode.Attributes["imageRotation"]?.Value;
			switch (imageRotation)
			{
				case "noRotation":
					data.ImageRotation = ImageRotation.NoRotation;
					break;
				case "clockwise":
					data.ImageRotation = ImageRotation.Clockwise;
					break;
				case "counterclockwise":
					data.ImageRotation = ImageRotation.Counterclockwise;
					break;
				case "upsideDown":
					data.ImageRotation = ImageRotation.UpsideDown;
					break;
				default:
					break;
			}

			#endregion

			#region extract card data

			var card = new ExtractedCard();
			var phones = new List<ExtractedPhone>();
			var nodeList = xmlDoc.GetElementsByTagName("field");
			foreach (XmlNode fieldNode in nodeList)
			{
				var fieldType = fieldNode.Attributes["type"]?.Value;
				XmlNode fieldComponentsNode = null;
				if (fieldNode.HasChildNodes)
				{
					switch (fieldType)
					{
						case "Name":
							fieldComponentsNode = fieldNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(o => o.Name == "fieldComponents");
							if (fieldComponentsNode != null && fieldComponentsNode.HasChildNodes)
							{
								var name = new ExtractedName();
								foreach (XmlNode fieldComponentNode in fieldComponentsNode.ChildNodes)
								{
									var fieldComponentType = fieldComponentNode.Attributes["type"]?.Value;
									var value = fieldComponentNode["value"]?.InnerText;
									switch (fieldComponentType)
									{
										case "FirstName":
											name.FirstName = value;
											break;
										case "LastName":
											name.LastName = value;
											break;
										case "MiddleName":
											name.MiddleName = value;
											break;
										default:
											break;
									}
								}
								card.Name = name;
							}
							break;
						case "Address":
							var address = new ExtractedAddress();
							fieldComponentsNode = fieldNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(o => o.Name == "fieldComponents");
							if (fieldComponentsNode != null && fieldComponentsNode.HasChildNodes)
							{
								foreach (XmlNode fieldComponentNode in fieldComponentsNode.ChildNodes)
								{
									var fieldComponentType = fieldComponentNode.Attributes["type"]?.Value;
									var value = fieldComponentNode["value"]?.InnerText;
									switch (fieldComponentType)
									{
										case "StreetAddress":
											address.StreetAddress = value;
											break;
										case "ZipCode":
											address.ZipCode = value;
											break;
										case "City":
											address.City = value;
											break;
										case "Country":
											address.Country = value;
											break;
										case "Region":
											address.Region = value;
											break;
										default:
											break;
									}
								}
							}
							card.Address = address;
							break;
						case "Job":
							fieldComponentsNode = fieldNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(o => o.Name == "fieldComponents");
							if (fieldComponentsNode != null && fieldComponentsNode.HasChildNodes)
							{
								card.Job = fieldComponentsNode.ChildNodes.Cast<XmlNode>()
									.Where(o => o.Attributes["type"]?.Value == "JobPosition")
									.Select(o => o["value"]?.InnerText).FirstOrDefault();
							}
							break;
						case "Fax":
						case "Phone":
						case "Mobile":
							phones.Add(new ExtractedPhone() { Type = fieldType, Number = fieldNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(o => o.Name == "value")?.InnerText });
							break;
						case "Company":
							card.Company = fieldNode["value"]?.InnerText;
							break;
						case "Email":
							card.Email = fieldNode["value"]?.InnerText;
							break;
						case "Web":
							card.Web = fieldNode["value"]?.InnerText;
							break;
						default:
							break;
					}
				}
			}
			card.Phones = phones;

			#endregion

			data.Card = card;

			return data;
		}

		private static ExtractedData ParseResultsXd(string resultUrl)
		{
			var xmlDoc = XDocument.Load(new StringReader(resultUrl));
			return ParseResultsXd0(xmlDoc);
		}

		private static ExtractedData ParseResultsXd0(XDocument xmlDoc)
		{
			var root = xmlDoc.Root;
			var xcard = root.Element("businessCard");

			xcard = root.Elements().FirstOrDefault(o => o.Name.LocalName == "businessCard");

			var data = new ExtractedData();

			data.ImageRotation = xcard.Attribute("imageRotation")?.Value switch
			{
				"noRotation" => ImageRotation.NoRotation,
				"clockwise" => ImageRotation.Clockwise,
				"counterclockwise" => ImageRotation.Counterclockwise,
				"upsideDown" => ImageRotation.UpsideDown,
				_ => default
			};

			var card = new ExtractedCard { Phones = new List<ExtractedPhone>() };

			foreach (var item in xcard.Elements())
			{
				if (item.IsEmpty)
					continue;
				string value = item.Elements().FirstOrDefault(o => o.Name.LocalName == "value")?.Value;
				var components = item.Elements().FirstOrDefault(o => o.Name.LocalName == "fieldComponents");
				switch (item.Attribute("type")?.Value)
				{
					case "Name":
						if (components == null)
							continue;
						var name = new ExtractedName();
						foreach (var field in components.Elements())
						{
							switch (field.Attribute("type")?.Value)
							{
								case "FirstName":
									name.FirstName = field.Value;
									break;
								case "LastName":
									name.LastName = field.Value;
									break;
								case "MiddleName":
									name.MiddleName = field.Value;
									break;
								default:
									break;
							}
						}
						card.Name = name;
						break;
					case "Address":
						if (components == null || card.Address != null)
							continue;
						var address = new ExtractedAddress();
						foreach (var field in components.Elements())
						{
							switch (field.Attribute("type")?.Value)
							{
								case "StreetAddress":
									address.StreetAddress = field.Value;
									break;
								case "ZipCode":
									address.ZipCode = field.Value;
									break;
								case "City":
									address.City = field.Value;
									break;
								case "Country":
									address.Country = field.Value;
									break;
								case "Region":
									address.Region = field.Value;
									break;
								default:
									break;
							}
						}
						card.Address = address;
						break;

					case "Job":
						if (components == null || card.Job != null)
							continue;
						card.Job = components.Elements().FirstOrDefault(o => o.Attribute("type")?.Value == "JobPosition")?.Elements().FirstOrDefault(o => o.Name.LocalName == "value")?.Value;
						break;

					case "Fax":
					case "Phone":
					case "Mobile":
						if (!String.IsNullOrWhiteSpace(value))
							card.Phones.Add(new ExtractedPhone() { Type = item.Attribute("type").Value, Number = value });
						break;
					case "Company":
						if (card.Company == null && !String.IsNullOrWhiteSpace(value))
							card.Company = value;
						break;
					case "Email":
						if (card.Email == null && !String.IsNullOrWhiteSpace(value))
							card.Email = value;
						break;
					case "Web":
						if (card.Web == null && !String.IsNullOrWhiteSpace(value))
							card.Web = value;
						break;
				}
			}

			data.Card = card;

			return data;
		}

		private static ExtractedData ParseResultsXl(string resultUrl)
		{
			var root = Lexxys.Xml.XmlLiteNode.FromXml(resultUrl);
			return ParseResultsXl0(root);
		}

		private static ExtractedData ParseResultsXl0(Lexxys.Xml.XmlLiteNode root)
		{
			var xcard = root.Element("businessCard");

			var data = new ExtractedData();

			data.ImageRotation = xcard["imageRotation"] switch
			{
				"noRotation" => ImageRotation.NoRotation,
				"clockwise" => data.ImageRotation = ImageRotation.Clockwise,
				"counterclockwise" => data.ImageRotation = ImageRotation.Counterclockwise,
				"upsideDown" => data.ImageRotation = ImageRotation.UpsideDown,
				_ => default
			};

			var card = new ExtractedCard { Phones = new List<ExtractedPhone>() };

			foreach (var item in xcard.Elements)
			{
				if (item.IsEmpty)
					continue;
				string value = item.Element("value")?.Value;
				var components = item.Element("fieldComponents");
				switch (item["type"])
				{
					case "Name":
						if (components == null)
							continue;
						var name = new ExtractedName();
						foreach (var field in components.Elements)
						{
							switch (field["type"])
							{
								case "FirstName":
									name.FirstName = field.Element("value").Value;
									break;
								case "LastName":
									name.LastName = field.Element("value").Value;
									break;
								case "MiddleName":
									name.MiddleName = field.Element("value").Value;
									break;
								default:
									break;
							}
						}
						card.Name = name;
						break;
					case "Address":
						if (components == null || card.Address != null)
							continue;
						var address = new ExtractedAddress();
						foreach (var field in components.Elements)
						{
							switch (field["type"])
							{
								case "StreetAddress":
									address.StreetAddress = field.Element("value").Value;
									break;
								case "ZipCode":
									address.ZipCode = field.Element("value").Value;
									break;
								case "City":
									address.City = field.Element("value").Value;
									break;
								case "Country":
									address.Country = field.Element("value").Value;
									break;
								case "Region":
									address.Region = field.Element("value").Value;
									break;
								default:
									break;
							}
						}
						card.Address = address;
						break;

					case "Job":
						if (components == null || card.Job != null)
							continue;
						card.Job = components.Elements.FirstOrDefault(o => o["type"] == "JobPosition")?.Element("value")?.Value;
						break;

					case "Fax":
					case "Phone":
					case "Mobile":
						if (!String.IsNullOrWhiteSpace(value))
							card.Phones.Add(new ExtractedPhone() { Type = item["type"], Number = value });
						break;
					case "Company":
						if (card.Company == null && !String.IsNullOrWhiteSpace(value))
							card.Company = value;
						break;
					case "Email":
						if (card.Email == null && !String.IsNullOrWhiteSpace(value))
							card.Email = value;
						break;
					case "Web":
						if (card.Web == null && !String.IsNullOrWhiteSpace(value))
							card.Web = value;
						break;
				}
			}

			data.Card = card;

			return data;
		}

		private static ExtractedData ParseResultsXl2(string resultUrl)
		{
			if (String.IsNullOrEmpty(resultUrl))
				return default;

			var x = Lexxys.Xml.XmlLiteNode.FromXml(resultUrl).Element("businessCard");
			if (x == null)
				return default;

			return new ExtractedData
			{
				ImageRotation = x["imageRotation"] switch
				{
					"noRotation" => ImageRotation.NoRotation,
					"clockwise" => ImageRotation.Clockwise,
					"counterclockwise" => ImageRotation.Counterclockwise,
					"upsideDown" => ImageRotation.UpsideDown,
					_ => default
				},
				Card = new ExtractedCard
				{
					Job = x.Elements.FirstOrDefault(o => o["type"] == "Job" && !String.IsNullOrWhiteSpace(o.Element("value").Value))?.Element("value").Value,
					Company = x.Elements.FirstOrDefault(o => o["type"] == "Company" && !String.IsNullOrWhiteSpace(o.Element("value").Value))?.Element("value").Value,
					Email = x.Elements.FirstOrDefault(o => o["type"] == "Email" && !String.IsNullOrWhiteSpace(o.Element("value").Value))?.Element("value").Value,
					Web = x.Elements.FirstOrDefault(o => o["type"] == "Web" && !String.IsNullOrWhiteSpace(o.Element("value").Value))?.Element("value").Value,
					Phones = x.Elements.Where(o => (o["type"] == "Fax" || o["type"] == "Mobile" || o["type"] == "Phone") && !String.IsNullOrWhiteSpace(o.Element("value").Value)).Select(o => new ExtractedPhone { Type = o["type"], Number = o.Element("value").Value }).ToList(),
					Address = x.Elements.Where(o => o["type"] == "Address")
						.Select(o => o.Element("fieldComponents")).Where(o => !o.IsEmpty)
						.Select(o => new ExtractedAddress
						{
							StreetAddress = o.Elements.FirstOrDefault(o => o["type"] == "StreetAddress")?.Element("value").Value,
							City = o.Elements.FirstOrDefault(o => o["type"] == "City")?.Element("value").Value,
							ZipCode = o.Elements.FirstOrDefault(o => o["type"] == "ZipCode")?.Element("value").Value,
							Region = o.Elements.FirstOrDefault(o => o["type"] == "Region")?.Element("value").Value,
							Country = o.Elements.FirstOrDefault(o => o["type"] == "Country")?.Element("value").Value,
						}).FirstOrDefault(),
					Name = x.Elements.Where(o => o["type"] == "Name")
						.Select(o => o.Element("fieldComponents")).Where(o => !o.IsEmpty)
						.Select(o => new ExtractedName
						{
							FirstName = o.Elements.FirstOrDefault(o => o["type"] == "FirstName")?.Element("value").Value,
							MiddleName = o.Elements.FirstOrDefault(o => o["type"] == "MiddleName")?.Element("value").Value,
							LastName = o.Elements.FirstOrDefault(o => o["type"] == "LastName")?.Element("value").Value,
						}).FirstOrDefault(),
				}
			};
		}



		private static ExtractedData ParseResultsXlR(XmlReader reader)
		{
			var root = Lexxys.Xml.XmlLiteNode.FromXml(reader);
			var xcard = root.Element("businessCard");

			var data = new ExtractedData();

			data.ImageRotation = xcard["imageRotation"] switch
			{
				"noRotation" => ImageRotation.NoRotation,
				"clockwise" => data.ImageRotation = ImageRotation.Clockwise,
				"counterclockwise" => data.ImageRotation = ImageRotation.Counterclockwise,
				"upsideDown" => data.ImageRotation = ImageRotation.UpsideDown,
				_ => default
			};

			var card = new ExtractedCard { Phones = new List<ExtractedPhone>() };

			foreach (var item in xcard.Elements)
			{
				if (item.IsEmpty)
					continue;
				string value = item.Element("value")?.Value;
				var components = item.Element("fieldComponents");
				switch (item["type"])
				{
					case "Name":
						if (components == null)
							continue;
						var name = new ExtractedName();
						foreach (var field in components.Elements)
						{
							switch (field["type"])
							{
								case "FirstName":
									name.FirstName = field.Element("value").Value;
									break;
								case "LastName":
									name.LastName = field.Element("value").Value;
									break;
								case "MiddleName":
									name.MiddleName = field.Element("value").Value;
									break;
								default:
									break;
							}
						}
						card.Name = name;
						break;
					case "Address":
						if (components == null || card.Address != null)
							continue;
						var address = new ExtractedAddress();
						foreach (var field in components.Elements)
						{
							switch (field["type"])
							{
								case "StreetAddress":
									address.StreetAddress = field.Element("value").Value;
									break;
								case "ZipCode":
									address.ZipCode = field.Element("value").Value;
									break;
								case "City":
									address.City = field.Element("value").Value;
									break;
								case "Country":
									address.Country = field.Element("value").Value;
									break;
								case "Region":
									address.Region = field.Element("value").Value;
									break;
								default:
									break;
							}
						}
						card.Address = address;
						break;

					case "Job":
						if (components == null || card.Job != null)
							continue;
						card.Job = components.Elements.FirstOrDefault(o => o["type"] == "JobPosition")?.Element("value")?.Value;
						break;

					case "Fax":
					case "Phone":
					case "Mobile":
						if (!String.IsNullOrWhiteSpace(value))
							card.Phones.Add(new ExtractedPhone() { Type = item["type"], Number = value });
						break;
					case "Company":
						if (card.Company == null && !String.IsNullOrWhiteSpace(value))
							card.Company = value;
						break;
					case "Email":
						if (card.Email == null && !String.IsNullOrWhiteSpace(value))
							card.Email = value;
						break;
					case "Web":
						if (card.Web == null && !String.IsNullOrWhiteSpace(value))
							card.Web = value;
						break;
				}
			}

			data.Card = card;

			return data;
		}



		public class RecognitionResult: IEquatable<RecognitionResult>
		{
			public ExtractedData Data { get; set; }
			public bool Recognized { get; set; }
			public string Image { get; set; }

			public override bool Equals(object obj)
			{
				return Equals(obj as RecognitionResult);
			}

			public bool Equals(RecognitionResult other)
			{
				return other != null &&
					   EqualityComparer<ExtractedData>.Default.Equals(Data, other.Data) &&
					   Recognized == other.Recognized &&
					   Image == other.Image;
			}

			public override int GetHashCode()
			{
				return (Data, Recognized, Image).GetHashCode();
			}
		}

		public class ExtractedData: IEquatable<ExtractedData>
		{
			public ExtractedCard Card { get; set; }
			public ImageRotation ImageRotation { get; set; }

			public override bool Equals(object obj)
			{
				return Equals(obj as ExtractedData);
			}

			public bool Equals(ExtractedData other)
			{
				return other != null &&
					   EqualityComparer<ExtractedCard>.Default.Equals(Card, other.Card) &&
					   ImageRotation == other.ImageRotation;
			}

			public override int GetHashCode()
			{
				return (Card, ImageRotation).GetHashCode();
			}
		}

		public class ExtractedCard: IEquatable<ExtractedCard>
		{
			public ExtractedName Name { get; set; }
			public List<ExtractedPhone> Phones { get; set; }
			public string Mobile { get; set; }
			public string Company { get; set; }
			public ExtractedAddress Address { get; set; }
			public string Email { get; set; }
			public string Web { get; set; }
			public string Job { get; set; }

			public override bool Equals(object obj)
			{
				return Equals(obj as ExtractedCard);
			}

			public bool Equals(ExtractedCard other)
			{
				return other != null &&
					   EqualityComparer<ExtractedName>.Default.Equals(Name, other.Name) &&
					   (Phones == null ? other.Phones == null: Enumerable.SequenceEqual(Phones, other.Phones)) &&
					   Mobile == other.Mobile &&
					   Company == other.Company &&
					   EqualityComparer<ExtractedAddress>.Default.Equals(Address, other.Address) &&
					   Email == other.Email &&
					   Web == other.Web &&
					   Job == other.Job;
			}

			public override int GetHashCode()
			{
				return (
					Name,
					Phones,
					Mobile,
					Company,
					Address,
					Email,
					Web,
					Job).GetHashCode();
			}
		}

		public class ExtractedName: IEquatable<ExtractedName>
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public string MiddleName { get; set; }

			public override bool Equals(object obj)
			{
				return Equals(obj as ExtractedName);
			}

			public bool Equals(ExtractedName other)
			{
				return other != null &&
					   FirstName == other.FirstName &&
					   LastName == other.LastName &&
					   MiddleName == other.MiddleName &&
					   FirstName == other.FirstName &&
					   LastName == other.LastName &&
					   MiddleName == other.MiddleName;
			}

			public override int GetHashCode()
			{
				return (FirstName, LastName, MiddleName, FirstName, LastName, MiddleName).GetHashCode();
			}
		}

		public class ExtractedPhone: IEquatable<ExtractedPhone>
		{
			public string Type { get; set; }
			public string Number { get; set; }

			public override bool Equals(object obj)
			{
				return Equals(obj as ExtractedPhone);
			}

			public bool Equals(ExtractedPhone other)
			{
				return other != null &&
					   Type == other.Type &&
					   Number == other.Number;
			}

			public override int GetHashCode()
			{
				return (Type, Number).GetHashCode();
			}
		}

		public class ExtractedAddress: IEquatable<ExtractedAddress>
		{
			public string StreetAddress { get; set; }
			public string City { get; set; }
			public string Country { get; set; }
			public string ZipCode { get; set; }
			public string Region { get; set; }

			public override bool Equals(object obj)
			{
				return Equals(obj as ExtractedAddress);
			}

			public bool Equals(ExtractedAddress other)
			{
				return other != null &&
					   StreetAddress == other.StreetAddress &&
					   City == other.City &&
					   Country == other.Country &&
					   ZipCode == other.ZipCode &&
					   Region == other.Region;
			}

			public override int GetHashCode()
			{
				return (StreetAddress, City, Country, ZipCode, Region).GetHashCode();
			}
		}
		public enum ImageRotation
		{
			NoRotation,
			Clockwise,
			Counterclockwise,
			UpsideDown
		}

		private static string GetContent()
		{
			// return File.ReadAllText("ocr-results.xml");
			return @"<document xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xmlns=""http://ocrsdk.com/schema/recognizedBusinessCard-1.0.xsd"" xsi:schemaLocation=""http://ocrsdk.com/schema/recognizedBusinessCard-1.0.xsd http://ocrsdk.com/schema/recognizedBusinessCard-1.0.xsd"">
    <businessCard imageRotation=""noRotation"">
        <field type=""Phone"">
            <value>+52 (55) 5726 5600 Ext.5714</value>
            <fieldComponents>
                <fieldComponent type=""PhonePrefix"">
                    <value>+</value>
                </fieldComponent>
                <fieldComponent type=""PhoneCountryCode"">
                    <value>52</value>
                </fieldComponent>
                <fieldComponent type=""PhoneCode"">
                    <value>55</value>
                </fieldComponent>
                <fieldComponent type=""PhoneBody"">
                    <value>57265600</value>
                </fieldComponent>
                <fieldComponent type=""PhoneExtension"">
                    <value>5714</value>
                </fieldComponent>
            </fieldComponents>
        </field>
        <field type=""Phone"">
            <value>+52 (55) 5726 5714</value>
            <fieldComponents>
                <fieldComponent type=""PhonePrefix"">
                    <value>+</value>
                </fieldComponent>
                <fieldComponent type=""PhoneCountryCode"">
                    <value>52</value>
                </fieldComponent>
                <fieldComponent type=""PhoneCode"">
                    <value>55</value>
                </fieldComponent>
                <fieldComponent type=""PhoneBody"">
                    <value>57265714</value>
                </fieldComponent>
            </fieldComponents>
        </field>
        <field type=""Email"">
            <value>neambriz@kaltex.com.mx</value>
        </field>
        <field type=""Web"">
            <value>www.kaltex.com.mx</value>
        </field>
        <field type=""Address"">
            <value>Ingenieros Militares No. 2 Piso 9 Col. Empleado Municipal Naucalpan de Juarez 53380 Estado de Mexico</value>
            <fieldComponents>
                <fieldComponent type=""StreetAddress"">
                    <value>Ingenieros Militares No. 2 Piso 9 Col. Empleado Municipal de Juarez Estado de</value>
                </fieldComponent>
                <fieldComponent type=""City"">
                    <value>Naucalpan</value>
                </fieldComponent>
                <fieldComponent type=""ZipCode"">
                    <value>53380</value>
                </fieldComponent>
                <fieldComponent type=""Country"">
                    <value>Mexico</value>
                </fieldComponent>
            </fieldComponents>
        </field>
        <field type=""Name"">
            <value>Nora Elisa Ambriz Garcia</value>
            <fieldComponents>
                <fieldComponent type=""FirstName"">
                    <value>Nora</value>
                </fieldComponent>
                <fieldComponent type=""MiddleName"">
                    <value>Elisa Ambriz</value>
                </fieldComponent>
                <fieldComponent type=""LastName"">
                    <value>Garcia</value>
                </fieldComponent>
            </fieldComponents>
        </field>
        <field type=""Company"">
            <value>KALTEX</value>
        </field>
        <field type=""Company"">
            <value>kaltex</value>
        </field>
        <field type=""Text"">
            <value>KALTEX NORA ELISA AMBRIZ GARCIA Ingenieros Militares No. 2 Piso 9 Col. Empleado Municipal Naucalpan de Juarez 53380 Estado de Mexico Tel.: +52 (55) 5726 5600 Ext. 5714 Dir.: +52 (55) 5726 5714 neambriz@kaltex.com.mx www.kaltex.com.mx</value>
        </field>
    </businessCard>
</document>";
		}
	}

	public class StrCmp
	{
		public static string Value1 = R.Ascii(33);
		public static string Value2 = Value1.Substring(0, Value1.Length - 3) + Value1.Substring(Value1.Length - 3).ToUpperInvariant();
		public static IEqualityComparer<string> Ordinal = StringComparer.Ordinal;
		public static IEqualityComparer<string> Case = StringComparer.OrdinalIgnoreCase;

		public static BenchmarkDotNet.Reports.Summary Go()
		{
			return BenchmarkRunner.Run<StrCmp>();
		}

		[Benchmark]
		public bool EnumOrdinal() => Value1.Equals(Value2, StringComparison.Ordinal);
		[Benchmark]
		public bool EnumIgnoreCase() => Value1.Equals(Value2, StringComparison.OrdinalIgnoreCase);
		[Benchmark]
		public bool MethodOrdinal() => Ordinal.Equals(Value1, Value2);
		[Benchmark]
		public bool MethodIgnoreCase() => Case.Equals(Value1, Value2);
	}
}
