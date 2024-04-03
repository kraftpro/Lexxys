using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys;

public interface IObjectAccessor
{
	bool TryGetValue(string name, out object? result);
	bool TryGetValue(string name, object index, out object? result);
	bool TrySetValue(string name, object? value);
	bool TrySetValue(string name, object index, object? value);
}
