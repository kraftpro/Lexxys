using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public static class SynchronizedCollection
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SynchronizedCollection{T}"/> class that contains elements copied from the specified collection.
		/// </summary>
		/// <param name="items">The collection whose elements are copied to the new <see cref="SynchronizedCollection{T}"/>.</param>
		/// <param name="areNew">Indicates the all the items are new in the collection</param>
		/// <param name="comparer">Optional equality comparer for the collection items</param>
		/// <param name="readOnly">Indicated the the collection is read only</param>
		public static SynchronizedCollection<T> Create<T>(IEnumerable<T> items, bool areNew = false, IEqualityComparer<T> comparer = null, bool readOnly = false)
		{
			return new SynchronizedCollection<T>(items, areNew, comparer, readOnly);
		}
	}

	/// <summary>
	/// Collection of the entities with tracking insertion and deletion operations.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SynchronizedCollection<T>: IList<T>, IReadOnlyList<T>
	{
		private readonly List<MarkedItem> _items;
		private readonly List<MarkedItem> _deletedItems;
		private readonly IEqualityComparer<T> _comparer;

		/// <summary>Collection item</summary>
		private readonly struct MarkedItem
		{
			/// <summary>The item value</summary>
			public readonly T Value;
			/// <summary>The item is new in the collection and will be added into the collection during Update.</summary>
			public readonly bool IsNew;

			public MarkedItem(T value)
			{
				Value = value;
				IsNew = false;
			}

			public MarkedItem(T value, bool isNew)
			{
				Value = value;
				IsNew = isNew;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedCollection{T}"/> that is empty.
		/// </summary>
		/// <param name="comparer">Optional equality comparer for the collection items</param>
		public SynchronizedCollection(IEqualityComparer<T> comparer = null)
		{
			_comparer = comparer ?? EqualityComparer<T>.Default;
			_items = new List<MarkedItem>();
			_deletedItems = new List<MarkedItem>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedCollection{T}"/> class that contains elements copied from the specified collection.
		/// </summary>
		/// <param name="items">The collection whose elements are copied to the new <see cref="SynchronizedCollection{T}"/>.</param>
		/// <param name="areNew">Indicates the all the items are new in the collection</param>
		/// <param name="comparer">Optional equality comparer for the collection items</param>
		/// <param name="readOnly">Indicated the the collection is read only</param>
		public SynchronizedCollection(IEnumerable<T> items, bool areNew = false, IEqualityComparer<T> comparer = null, bool readOnly = false)
		{
			_comparer = comparer ?? EqualityComparer<T>.Default;
			_items = items == null ? new List<MarkedItem>() : new List<MarkedItem>(items.AsEnumerable().Select(o => new MarkedItem(o, areNew)));
			_deletedItems = new List<MarkedItem>();
			IsReadOnly = readOnly;
		}

		/// <summary>
		/// Get element by specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T this[int index]
		{
			get
			{
				return _items[index].Value;
			}
			set
			{
				if (IsReadOnly)
					throw CollectionReadOnlyException();
				if (!_items[index].IsNew)
					_deletedItems.Add(_items[index]);
				_items[index] = new MarkedItem(value, true);
			}
		}

		/// <summary>
		/// Indicates that the collection has items marked as new.
		/// </summary>
		public bool HasInsertedItems => _items.Any(o => o.IsNew);
		/// <summary>
		/// Indicates that the collection has items marked for deletion.
		/// </summary>
		public bool HasDeletedItems => _deletedItems.Count > 0;

		/// <summary>
		/// Synchronizes the collection items. Actual work to insert, update and delete will be done by the specified delegates.
		/// </summary>
		/// <param name="create">Action to insert the item to the collection.</param>
		/// <param name="delete">Action to delete the item from the collection.</param>
		/// <param name="update">Action to update the item of the collection.</param>
		public void Synchronize(Action<T> create, Action<T> delete, Action<T> update = null)
		{
			if (IsReadOnly)
				return;
			if (create == null)
				throw new ArgumentNullException(nameof(create));
			if (delete == null)
				throw new ArgumentNullException(nameof(delete));

			//Debug.Assert(original.Count == _items.Count + _deletedItems.Count - _newItems.Count);
			foreach (var item in _deletedItems)
			{
				delete(item.Value);
			}

			foreach (var item in _items)
			{
				if (item.IsNew)
					create(item.Value);
				else
					update?.Invoke(item.Value);
			}
			MarkSynced();
		}

		/// <summary>
		/// Synchronizes the collection items. Actual work to insert, update and delete will be done by the specified delegates.
		/// </summary>
		/// <param name="create">Action to insert the item to the collection.</param>
		/// <param name="delete">Action to delete the item from the collection.</param>
		/// <param name="update">Action to update the item of the collection.</param>
		public async Task SynchronizeAsync(Func<T, Task> create, Func<T, Task> delete, Func<T, Task> update = null)
		{
			if (IsReadOnly)
				return;
			if (create == null)
				throw new ArgumentNullException(nameof(create));
			if (delete == null)
				throw new ArgumentNullException(nameof(delete));

			//Debug.Assert(original.Count == _items.Count + _deletedItems.Count - _newItems.Count);
			foreach (var item in _deletedItems)
			{
				await delete(item.Value);
			}

			foreach (var item in _items)
			{
				if (item.IsNew)
					await create(item.Value);
				else if (update != null)
					await update.Invoke(item.Value);
			}
			MarkSynced();
		}

		/// <summary>
		/// Marks all items in the collection as updated (no any inserts or deletion will be executed in the next <see cref="Synchronize"/> call.
		/// </summary>
		public void MarkSynced()
		{
			if (IsReadOnly)
				return;
			_deletedItems.Clear();
			for (int i = _items.Count - 1; i >= 0; i--)
			{
				_items[i] = new MarkedItem(_items[i].Value);
			}
		}

		/// <summary>
		/// Synchronizes <see cref="SynchronizedCollection{T}"/> with the specified list of <paramref name="values"/>.
		/// </summary>
		/// <param name="values">Desired content of the collection</param>
		/// <param name="replace">Optional operator used to replace element in the resulted collection items with correspondent item from <paramref name = "values" />.</param>
		public void SynchronizeWith(IEnumerable<T> values, Action<T, T> replace = null)
		{
			SynchronizeWith(values, (p, q) => _comparer.Equals(p, q), o => o, replace);
		}

		/// <summary>
		/// Synchronizes <see cref="SynchronizedCollection{T}"/> with the specified list of <paramref name="values"/>.
		/// </summary>
		/// <typeparam name="T2">Type of the values item</typeparam>
		/// <param name="values">Desired content of the collection</param>
		/// <param name="equals">A predicate to indicate that two elements from the source and target collections are equal.</param>
		/// <param name="create">An operator used to conver value from <typeparamref name="T"/> to <typeparamref name="T2"/>.</param>
		/// <param name="replace">Optional operator used to replace element in the resulted collection items with correspondent item from <paramref name = "values" />.</param>
		public void SynchronizeWith<T2>(IEnumerable<T2> values, Func<T, T2, bool> equals, Func<T2, T> create, Action<T, T2> replace = null)
		{
			if (IsReadOnly)
				throw CollectionReadOnlyException();

			_items.AddRange(_deletedItems);
			_deletedItems.Clear();

			var used = new BitArray(_items.Count);
			foreach (var item in values ?? Array.Empty<T2>())
			{
				int i = _items.FindIndex(o => equals(o.Value, item));
				if (i >= 0 && i < used.Count)
				{
					used[i] = true;
					replace?.Invoke(_items[i].Value, item);
				}
				else
				{
					Add(create(item));
				}
			}

			for (int i = 0, j = 0; i < used.Count; ++i)
			{
				if (!used[i])
				{
					if (!_items[i + j].IsNew)
						_deletedItems.Add(_items[i + j]);
					_items.RemoveAt(i + j);
					--j;
				}
			}
		}

		#region IList<T> interface

		/// <inheritdoc cref="ICollection{T}.IsReadOnly"/>
		public bool IsReadOnly { get; }

		/// <inheritdoc cref="IList{T}.IndexOf(T)"/>
		public int IndexOf(T item)
		{
			return _items.FindIndex(o => _comparer.Equals(item, o.Value));
		}

		/// <inheritdoc cref="ICollection{T}.Add"/>
		public virtual void Add(T item)
		{
			if (IsReadOnly)
				throw CollectionReadOnlyException();
			_items.Add(new MarkedItem(item, true));
		}

		/// <inheritdoc cref="IList{T}.Insert(int, T)"/>
		public void Insert(int index, T item)
		{
			if (IsReadOnly)
				throw CollectionReadOnlyException();
			_items.Insert(index, new MarkedItem(item, true));
		}

		/// <inheritdoc cref="ICollection{T}.Clear"/>
		public void Clear()
		{
			if (IsReadOnly)
				throw CollectionReadOnlyException();
			_deletedItems.AddRange(_items.Where(o => !o.IsNew));
			_items.Clear();
		}

		/// <inheritdoc cref="ICollection{T}.Contains"/>
		public bool Contains(T item) => _items.FindIndex(o => _comparer.Equals(o.Value, item)) >= 0;

		/// <inheritdoc cref="ICollection{T}.CopyTo"/>
		public void CopyTo(T[] array, int arrayIndex)
		{
			_items.Select(o => o.Value).ToList().CopyTo(array, arrayIndex);
		}

		/// <inheritdoc cref="ICollection{T}.Remove"/>
		public bool Remove(T item)
		{
			if (IsReadOnly)
				throw CollectionReadOnlyException();
			int i = _items.FindIndex(o => _comparer.Equals(o.Value, item));
			if (i < 0)
				return false;
			if (!_items[i].IsNew)
				_deletedItems.Add(_items[i]);
			_items.RemoveAt(i);
			return true;
		}

		/// <inheritdoc cref="IList{T}.RemoveAt(int)"/>
		public void RemoveAt(int index)
		{
			if (IsReadOnly)
				throw CollectionReadOnlyException();
			var item = _items[index];
			_items.RemoveAt(index);
			if (!item.IsNew)
				_deletedItems.Add(item);
		}

		/// <inheritdoc cref="ICollection{T}.Count"/>
		public int Count => _items.Count;

		/// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
		public IEnumerator<T> GetEnumerator()
		{
			foreach (var item in _items)
			{
				yield return item.Value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		private Exception CollectionReadOnlyException()
		{
			return new NotSupportedException("The collection is readonly.");
		}
	}
}
