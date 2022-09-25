// Lexxys Infrastructural library.
// file: BooleanCube.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.ObjectModel;

namespace Lexxys.Cube
{
	public class BooleanCube
	{
		readonly Table _table;
		readonly int _dimension;

		public BooleanCube(int dimension)
		{
			_table = new Table();
			_dimension = dimension;
		}

		public BooleanCube(BitArray bits, int dimension)
		{
			if (bits == null)
				throw EX.ArgumentNull(nameof(bits));
			int n = 1 << dimension;
			if (n == 0)
				throw EX.ArgumentOutOfRange(nameof(dimension), dimension);
			_table = new Table();
			_dimension = dimension;
			for (int i = 0; i < n; ++i)
			{
				if (bits[i])
					_table.Add(new BooleanCubeNode((uint)i));
			}
		}

		class Table: HashSet<BooleanCubeNode>
		{
			public ReadOnlyCollection<BooleanCubeNode> Neighbors(BooleanCubeNode node, int width)
			{
				var xx = new List<BooleanCubeNode>();
				for (int i = 0; i < width; ++i)
				{
					if (!node.IsHole(i) && !node.IsBit(i))
					{
						node.ReverseBit(i);
						if (Contains(node))
							xx.Add(node);
						node.ReverseBit(i);
					}
				}
				return xx.AsReadOnly();
			}
		}

		public int Dimension
		{
			get { return _dimension; }
		}

		public int Count
		{
			get { return _table.Count; }
		}

		public void Add(BooleanCubeNode other)
		{
			_table.Add(other);
		}

		public ReadOnlyCollection<BooleanCubeNode> Neighbors(BooleanCubeNode node)
		{
			return _table.Neighbors(node, _dimension);
		}

		public string BuildDnf(string[] arguments)
		{
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));

			var sb = new StringBuilder();
			if (arguments.Length != Dimension)
				throw EX.ArgumentOutOfRange("arguments.Length", arguments.Length, Dimension);
			foreach (BooleanCubeNode x in _table)
			{
				char delimiter = '|';
				for (int i = 0; i < arguments.Length; ++i)
				{
					if (!x.IsHole(i))
					{
						sb.Append(delimiter);
						if (!x[i])
							sb.Append('~');
						sb.Append(arguments[i]);
						delimiter = '&';
					}
				}
			}
			if (sb.Length == 0)
				return "";
			return sb.ToString(1, sb.Length - 1);
		}

		public string BuildMinimalDnf(string[] arguments)
		{
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));
			if (arguments.Length != Dimension)
				throw EX.ArgumentOutOfRange("arguments.Length", arguments.Length, Dimension);
			if (Count == (1 << Dimension))
				return "TRUE";
			if (Count == 0)
				return "FALSE";
			var result = new BooleanCube(Dimension);
			BooleanCube cube = this;
			do
			{
				var cube2 = new BooleanCube(Dimension);
				var mark = new HashSet<uint>();
				foreach (BooleanCubeNode x in cube)
				{
					bool optimized = false;
					foreach (BooleanCubeNode y in cube.Neighbors(x))
					{
						cube2.Add(x + y);
						optimized = true;
						if (!mark.Contains(y.Bits))
							mark.Add(y.Bits);
					}
					if (!optimized & !mark.Contains(x.Bits))
						result.Add(x);
				}
				cube = cube2;
			} while (cube.Count > 0);
			string dnf = result.BuildDnf(arguments);
			return dnf.Length == 0 ? "FALSE" : dnf;
		}

		public IEnumerator<BooleanCubeNode> GetEnumerator()
		{
			return _table.GetEnumerator();
		}
	}
}


