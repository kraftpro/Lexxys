using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

namespace Lexxys.Test.Con.Misc
{
	public static class ErrorCodes
	{
		public static void Go()
		{

		}
	}

	public interface IErrorMessageResource
	{
		string this[ErrorCode index] { get; }
		string this[ErrorCode index, ErrorAttrib item] { get; }
		string this[ErrorCode index, ErrorAttrib item1, ErrorAttrib item2] { get; }
		string this[ErrorCode index, params ErrorAttrib[] items] { get; }
		string this[ErrorCode index, IEnumerable<ErrorAttrib> items] { get; }
	}
}