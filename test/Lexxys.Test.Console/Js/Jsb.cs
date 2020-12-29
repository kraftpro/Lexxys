using Lexxys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con.Js
{
	using static ZenJson;

	class Jsb
	{
		//public delegate JsonScalar Ji(string value);
		//public delegate JsonMap Joi(string name, JsonItem value);
		//public delegate JsonMap Jos(string name, string value);
		//public delegate JsonArray Jai(string value, params JsonItem[] items);
		//public delegate JsonArray Jas(string value, params string[] items);
		//public delegate JsonArray Jaei(string value, IEnumerable<JsonItem> items);
		//public delegate JsonArray Jaes(string value, IEnumerable<string> items);

		public static void Test()
		{
			string user = "юзер123";
			string password = "р123";
			var js = J("data",
				J("type", J("session")),
				J("attribures",
					J("auth-name", J(user)),
					J("password", J(password))
				)
			);
			Console.WriteLine(js.ToString(true));
			var mem = new MemoryStream();
			js.Write(mem);
			var buffer = mem.GetBuffer();
			var text = Encoding.UTF8.GetString(buffer, 0, (int)mem.Length);
			Console.WriteLine(text);
		}
	}

}
