// Lexxys Infrastructural library.
// file: Cnstrctr.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using Lexxys;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con
{
	class Cnstrctr
	{
		enum Abc
		{
			A = 1,
			B = 2,
			C = 3,
		}

		public static void Go()
		{
			var x = new { a = default(int?), b = default(Abc?) };
			var y = Construct(x, 1, 1);
		}

		static T Construct<T>(T template, params object[] values)
		{
			Type type = typeof(T);
			ConstructorInfo[] cc = type.GetConstructors();
			Dictionary<int, Func<object[], object>> constructors = new Dictionary<int, Func<object[], object>>();
			for (int i = 0; i < cc.Length; ++i)
			{
				Type[] parameters = Array.ConvertAll(cc[i].GetParameters(), o => o.ParameterType);
				if (!constructors.ContainsKey(parameters.Length))
					constructors[parameters.Length] = Factory.TryGetConstructor(type, true, parameters);
			}

			Func<object[], object> constructor = constructors[values.Length];
			object result = constructor(values);
			return (T)result;
		}
	}
}
