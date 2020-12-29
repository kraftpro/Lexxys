using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public readonly struct ErrorAttrib
	{
		public string Name { get; }
		public object Value { get; }

		public ErrorAttrib(string name, object value)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Value = value;
		}
	}
}
