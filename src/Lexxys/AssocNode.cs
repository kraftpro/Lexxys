// Lexxys Infrastructural library.
// file: AssocNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Globalization;

namespace Lexxys
{

	/// <summary>Element of oriented graph</summary>
	/// <remarks>
	/// </remarks>
	public class AssocNode
	{
		private List<AssocNode> _forwards;
		private bool _forwardsBuilt;
		private List<AssocNode> _backwards;
		private bool _backwardsBuilt;

		private static readonly List<AssocNode> _emptyList = new List<AssocNode>();
		private static readonly IEnumerable<AssocNode> _emptyCollection = _emptyList.AsReadOnly();

		protected static IList<AssocNode> EmptyList
		{
			get { return _emptyList; }
		}

		protected static IEnumerable<AssocNode> EmptyCollection
		{
			get { return _emptyCollection; }
		}

		public IList<AssocNode> Forwards
		{
			get
			{
				if (!_forwardsBuilt)
					BuildForwards();
				return _forwards;
			}
		}

		public IList<AssocNode> Backwards
		{
			get
			{
				if (!_backwardsBuilt)
					BuildBackwards();
				return _backwards;
			}
		}

		protected IList<AssocNode> CollectedForwards
		{
			get { return _forwards ?? _emptyList; }
		}

		protected IList<AssocNode> CollectedBackwards
		{
			get { return _backwards ?? _emptyList; }
		}

		public bool ForwardsCollected
		{
			get { return _forwardsBuilt || _forwards != null; }
		}

		public bool BackwardsCollected
		{
			get { return _backwardsBuilt || _backwards != null; }
		}

		protected virtual IEnumerable<AssocNode> ComposeForwards()
		{
			return _emptyCollection;
		}

		protected virtual IEnumerable<AssocNode> ComposeBackwards()
		{
			return _emptyCollection;
		}

		private void BuildForwards()
		{
			if (!_forwardsBuilt)
			{
				foreach (AssocNode x in ComposeForwards())
					AddForward(x);
				if (_forwards == null)
					_forwards = _emptyList;
				_forwardsBuilt = true;
			}
		}

		private void BuildBackwards()
		{
			if (!_backwardsBuilt)
			{
				foreach (AssocNode x in ComposeBackwards())
					AddBackward(x);
				if (_backwards == null)
					_backwards = _emptyList;
				_backwardsBuilt = true;
			}
		}

		public bool AddForward(AssocNode node)
		{
			if (node == null)
				throw EX.ArgumentNull(nameof(node));
			CheckMisReference(node);
			if (_forwards == null || _forwards == _emptyList)
				_forwards = new List<AssocNode>();
			else if (_forwards.Contains(node))
				return false;
			_forwards.Add(node);
			if (node._backwards == null || node._backwards == _emptyList)
				node._backwards = new List<AssocNode>();
			node._backwards.Add(this);
			return true;
		}

		public bool AddBackward(AssocNode node)
		{
			if (node == null)
				throw EX.ArgumentNull(nameof(node));
			CheckMisReference(node);
			if (_backwards == null || _backwards == _emptyList)
				_backwards = new List<AssocNode>();
			else if (_backwards.Contains(node))
				return false;
			_backwards.Add(node);
			if (node._forwards == null || node._forwards == _emptyList)
				node._forwards = new List<AssocNode>();
			node._forwards.Add(this);
			return true;
		}

		public void RemoveForward(AssocNode node)
		{
			if (node == null)
				throw EX.ArgumentNull(nameof(node));
			CheckMisReference(node);
			if (HasForward(node))
			{
				_forwards.Remove(node);
				node._backwards.Remove(this);
			}
		}

		public void RemoveBackward(AssocNode node)
		{
			if (node == null)
				throw EX.ArgumentNull(nameof(node));
			CheckMisReference(node);
			if (HasBackward(node))
			{
				_backwards.Remove(node);
				node._forwards.Remove(this);
			}
		}

		public void RemoveAllForwards()
		{
			if (_forwards != null)
			{
				foreach (AssocNode node in _forwards)
				{
					node._backwards.Remove(this);
				}
				_forwards = null;
			}
			_forwardsBuilt = false;
		}

		public void RemoveAllBackwards()
		{
			if (_backwards != null)
			{
				foreach (AssocNode node in _backwards)
				{
					node._forwards.Remove(this);
				}
				_backwards = null;
			}
			_backwardsBuilt = false;
		}

		public void Detach()
		{
			Detach(false);
		}

		public void Detach(bool temporary)
		{
			if (_forwards != null)
			{
				foreach (AssocNode node in _forwards)
				{
					node._backwards.Remove(this);
					if (temporary)
						node._backwardsBuilt = false;
				}
			}
			_forwards = null;
			_forwardsBuilt = false;

			if (_backwards != null)
			{
				foreach (AssocNode node in _backwards)
				{
					node._forwards.Remove(this);
					if (temporary)
						node._forwardsBuilt = false;
				}
			}
			_backwards = null;
			_backwardsBuilt = false;
		}

		private bool HasForward(AssocNode node)
		{
			return _forwards != null && _forwards.Contains(node);
		}

		private bool HasBackward(AssocNode node)
		{
			return _backwards != null && _backwards.Contains(node);
		}

		[Conditional("DEBUG")]
		private void CheckMisReference(AssocNode node)
		{
			if (HasForward(node) ^ node.HasBackward(this))
				throw EX.InvalidOperation(SR.AssocNodeMissReference());
			if (HasBackward(node) ^ node.HasForward(this))
				throw EX.InvalidOperation(SR.AssocNodeMissReference());
		}

		private class DumpContext
		{
			private readonly List<AssocNode> _map;

			public DumpContext(int count)
			{
				_map = new List<AssocNode>(count);
			}

			public bool Find(AssocNode node, out int id)
			{
				id = 1 + _map.IndexOf(node);
				if (id > 0)
					return true;
				_map.Add(node);
				id = _map.Count;
				return false;
			}
		}

		public void Dump(XmlWriter writer, int backwardsDepth, int forwardsDepth)
		{
			var ctx = new DumpContext(64);
			DumpNode(writer, backwardsDepth, forwardsDepth, ctx);
		}

		protected virtual void DumpInternal(XmlWriter writer)
		{
		}

		private void DumpNode(XmlWriter writer, int backwardsDepth, int forwardsDepth, DumpContext ctx)
		{
			writer.WriteStartElement("node");

			if (ctx.Find(this, out int id))
			{
				writer.WriteAttributeString("ref", id.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				writer.WriteAttributeString("id", id.ToString(CultureInfo.InvariantCulture));

				DumpInternal(writer);

				if (backwardsDepth > 0)
				{
					writer.WriteStartElement("backwards");
					foreach (AssocNode node in Backwards)
					{
						node.DumpNode(writer, backwardsDepth - 1, 0, ctx);
					}
					writer.WriteEndElement();
				}

				if (forwardsDepth > 0)
				{
					writer.WriteStartElement("forwards");
					foreach (AssocNode node in Forwards)
					{
						node.DumpNode(writer, 0, forwardsDepth - 1, ctx);
					}
					writer.WriteEndElement();
				}
			}

			writer.WriteEndElement();
		}
	}
}


