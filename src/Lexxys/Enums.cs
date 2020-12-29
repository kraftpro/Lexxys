// Lexxys Infrastructural library.
// file: Enums.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lexxys
{
	using Data;
	using Xml;

	public class EnumsCategory: IReadOnlyCollection<EnumsRecord>, IEnum
	{
		private static Dictionary<int, string> _categoryById;
		private static Dictionary<string, int> _categoryByPath;
		private static readonly ConcurrentDictionary<int, EnumsCategory> _categoryCache = new ConcurrentDictionary<int, EnumsCategory>();
		private static readonly object SyncObj = new object();

		private EnumsCategory()
		{
		}

		public int Id { get; private set; }

		public string Path { get; private set; }

		public IReadOnlyList<EnumsRecord> Items { get; private set; }

		public int Count => Items.Count;

		public EnumsRecord this[int itemId]
		{
			get { return Items.FirstOrDefault(o => o.ItemId == itemId) ?? EnumsRecord.Empty; }
		}

		public EnumsRecord this[string itemName]
		{
			get
			{
				if (itemName == null || itemName.Length == 0)
					return EnumsRecord.Empty;
				int id;
				if (int.TryParse(itemName, out id))
					return this[id];

				return Items.FirstOrDefault(o => String.Equals(o.Abbrev, itemName, StringComparison.OrdinalIgnoreCase) || String.Equals(o.Name, itemName, StringComparison.OrdinalIgnoreCase)) ?? EnumsRecord.Empty;
			}
		}

		public EnumsRecord Find(Func<EnumsRecord, bool> predicate)
		{
			return Items.FirstOrDefault(predicate) ?? EnumsRecord.Empty;
		}

		int IEnum.Value => Id;

		string IEnum.Name => Path;

		public IEnumerator<EnumsRecord> GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		public EnumsRecord[] CreateArray(EnumsRecord empty, int offset = 0)
		{
			int maxId = Items.Aggregate(Int32.MinValue, (s, o) => Math.Max(s, o.ItemId));
			var array = new EnumsRecord[offset + maxId + 1];
			foreach (var item in Items)
			{
				array[offset + item.ItemId] = item;
			}
			if (empty != null)
			{
				for (int i = 0; i < array.Length; ++i)
				{
					if (array[i] == null)
						array[i] = empty;
				}
			}
			return array;
		}


		public override string ToString()
		{
			return Id.ToString();
		}

		public static EnumsCategory Empty => __empty;

		private static readonly EnumsCategory __empty = new EnumsCategory { Id = 0, Path = "", Items = ReadOnly.Empty<EnumsRecord>() };

		public static EnumsCategory GetCategory(int categoryId)
		{
			if (categoryId <= 0)
				throw EX.ArgumentOutOfRange("categoryId", categoryId);

			return _categoryCache.GetOrAdd(categoryId, RetreiveCategory);
		}

		public static int GetCategoryId(string categoryPath)
		{
			if (categoryPath == null)
				throw EX.ArgumentNull("categoryPath");

			int id;
			if (int.TryParse(categoryPath, out id))
			{
				if (id <= 0)
					throw EX.ArgumentOutOfRange("id", id);
				return id;
			}

			CollectCategoryItems();
			int categoryId;
			_categoryByPath.TryGetValue(CleanCategoryPath(categoryPath), out categoryId);
			return categoryId;
		}

		public static explicit operator EnumsCategory (int value)
		{
			EnumsCategory result = GetCategory(value);
			if (result.Id != value)
				throw EX.ArgumentOutOfRange(nameof(value), value);
			return result;
		}

		private static EnumsCategory RetreiveCategory(int categoryId)
		{
			if (categoryId <= 0)
				throw EX.ArgumentOutOfRange("categoryId", categoryId);

			CollectCategoryItems();
			string categoryPath;
			return _categoryById.TryGetValue(categoryId, out categoryPath) ? EnumsCategory.Create(categoryId, categoryPath): EnumsCategory.Empty;
		}

		private static void CollectCategoryItems()
		{
			if (_categoryById == null)
			{
				lock (SyncObj)
				{
					if (_categoryById == null)
					{
						var items = Dc.GetList<CategoryItem>("select ID, Path from EnumsCategories");
						var byId = items.ToDictionary(o => o.Id, o=> o.Path);
						var byPath = items.ToDictionary(o => CleanCategoryPath(o.Path), o => o.Id, StringComparer.Ordinal);
						_categoryByPath = byPath;
						Thread.MemoryBarrier();
						_categoryById = byId;
					}
				}
			}
		}

		private static string CleanCategoryPath(string value)
		{
			value = value.Replace(" ", "").Replace("\\", "/").Trim('/');
			int k;
			do
			{
				k = value.Length;
				value = value.Replace("//", "/");
			} while (value.Length != k);
			return value.ToUpperInvariant();
		}

		private static EnumsCategory Create(int id, string path)
		{
			if (id <= 0)
				return Empty;
			if (path == null || path.Length == 0)
				throw EX.ArgumentNull("path");

			var category = new EnumsCategory();
			var items = new List<EnumsRecord>();
			Dc.Map(o => items.Add(EnumsRecord.Load(o, category)),
				"select ID, ItemID, Name, Text, Abbrev, Tag, TheValue, GroupID, SortOrder, Category from Enums" +
				" where Category=@C" +
				" order by SortOrder,ItemId;",
				Dc.Parameter("@C", id)
				);
			if (items.Count == 0)
				return Empty;

			category.Id = id;
			category.Path = path;
			category.Items = ReadOnly.Wrap(items);
			return category;
		}

		private class CategoryItem
		{
			public readonly int Id;
			public readonly string Path;

			public CategoryItem(int id, string path)
			{
				Id = id;
				Path = path;
			}
		}
	}

	public class EnumsRecord: IOrderedEnum
	{
		private EnumsRecord()
		{
		}

		public int Id { get; private set; }

		public int ItemId { get; private set; }

		public string Name { get; private set; }

		public string Text { get; private set; }

		public string Abbrev { get; private set; }

		public string Tag { get; private set; }

		public string Value { get; private set; }

		public int? Group { get; private set; }

		public int? SortOrder { get; private set; }

		public EnumsCategory Category { get; private set; }

		public IReadOnlySet<string> FlagsSet => __flags ?? (__flags = ReadOnly.Wrap(new FlagSet(Value)));
		private IReadOnlySet<string> __flags;

		public static EnumsRecord Empty { get; } = new EnumsRecord { Id = 0, ItemId = 0, Name = "", Text = "", Abbrev = "", Tag = "", Value = "", Category = EnumsCategory.Empty, Group = null, SortOrder = null };

		int IEnum.Value => ItemId;
		string IEnum.Name => Name;
		int IOrderedEnum.Order => SortOrder ?? 0;

		internal static EnumsRecord Load(IDataRecord record, EnumsCategory category)
		{
			if (record == null)
				throw EX.ArgumentNull("record");
			if (record.FieldCount < 8)
				throw EX.ArgumentOutOfRange("record.FieldCount", record.FieldCount);

			return new EnumsRecord
			{
				Id = (int)record[0],
				ItemId = (int)record[1],
				Name = record.IsDBNull(2) ? null: (string)record[2],
				Text = record.IsDBNull(3) ? null: (string)record[3],
				Abbrev = record.IsDBNull(4) ? null: (string)record[4],
				Tag = record.IsDBNull(5) ? null: (string)record[5],
				Value = record.IsDBNull(6) ? null: (string)record[6],
				Group = record.IsDBNull(7) ? null: (int?)record[7],
				SortOrder = record.IsDBNull(8) ? null: (int?)record[8],
				Category = category,
			};
		}

		public static EnumsRecord FromXml(XmlLiteNode node, EnumsCategory category)
		{
			if (node == null || node.IsEmpty)
				return null;
			return new EnumsRecord
			{
				Category = category,
				Id = node["Id"].AsInt32(0),
				ItemId = node["ItemId"].AsInt32(0),
				Name = node["Name"],
				Text = node["Text"],
				Tag = node["Tag"],
				Abbrev = node["Abbrev"],
				Group = node["Group"].AsInt32(null),
				Value = node["Value"],
				SortOrder = node["SortOrder"].AsInt32(null)
			};
		}
	}
}


