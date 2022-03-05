// Lexxys Infrastructural library.
// file: DcLocal.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys.Data
{
	public static class DataContextExtensions
	{
		/// <summary>
		/// Sets operation to be committed or rolled back along with the database transaction.
		/// </summary>
		/// <typeparam name="T">Unique type of the operation.</typeparam>
		/// <param name="context">IDataContext</param>
		/// <param name="factory">Function to produce the operation.</param>
		/// <returns></returns>
		public static T SetCommitAction<T>(this IDataContext context, Func<T> factory) where T: ICommitAction
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			var value = context.SetCommitAction(typeof(T), () => factory());
			return value is T t ? t: throw new InvalidOperationException();
		}

		public static T GetValueOrDefault<T>(this IDataContext context, T @default, string query, params DataParameter[] parameters) where T: class
		{
			return context.GetValue<T>(query, parameters) ?? @default;
		}

		public static async Task<T> GetValueOrDefaultAsync<T>(this IDataContext context, T @default, string query, params DataParameter[] parameters) where T: class
		{
			return await context.GetValueAsync<T>(query, parameters).ConfigureAwait(false) ?? @default;
		}

		public static int Map(this IDataContext context, Action<IDataRecord> mapper, string query, params DataParameter[] parameters)
		{
			return Map(context, Int32.MaxValue, mapper, query, parameters);
		}

		public static Task<int> MapAsync(this IDataContext context, Action<IDataRecord> mapper, string query, params DataParameter[] parameters)
		{
			return context.MapAsync(Int32.MaxValue, mapper, query, parameters);
		}

		public static int Map(this IDataContext context, int limit, Action<IDataRecord> mapper, string query, params DataParameter[] parameters)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			return context.Map(o => ActionMapper(o, limit, mapper), query, parameters);

			static int ActionMapper(DbCommand cmd, int limit, Action<IDataRecord> mapper)
			{
				using DbDataReader reader = cmd.ExecuteReader();
				int count = 0;
				while (count < limit && reader.Read())
				{
					++count;
					mapper(reader);
				}
				return count;
			}
		}

		public static Task<int> MapAsync(this IDataContext context, int limit, Action<IDataRecord> mapper, string query, params DataParameter[] parameters)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			return context.MapAsync(o => ActionMapper(o, limit, mapper), query, parameters);

			static async Task<int> ActionMapper(DbCommand cmd, int limit, Action<IDataRecord> mapper)
			{
				using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
				int count = 0;
				while (count < limit && await reader.ReadAsync().ConfigureAwait(false))
				{
					++count;
					mapper(reader);
				}
				return count;
			}
		}

		[return: MaybeNull]
		public static T GetValue<T>(this IDataContext context, string query, params DataParameter[] parameters) => context.Map(Dc.ValueMapper<T>, query, parameters);

		public static Task<T> GetValueAsync<T>(this IDataContext context, string query, params DataParameter[] parameters) => context.MapAsync(Dc.ValueMapperAsync<T>, query, parameters);

		public static List<T> GetList<T>(this IDataContext context, string query, params DataParameter[] parameters) => context.Map(Dc.ListMapper<T>, query, parameters)!;

		public static Task<List<T>> GetListAsync<T>(this IDataContext context, string query, params DataParameter[] parameters) => context.MapAsync(Dc.ListMapperAsync<T>, query, parameters);

		public static bool ReadXmlText(this IDataContext context, TextWriter text, string query, params DataParameter[] parameters) => context.Map(o => Dc.XmlTextMapper(text, o), query, parameters);

		public static Task<bool> ReadXmlTextAsync(this IDataContext context, TextWriter text, string query, params DataParameter[] parameters) => context.MapAsync(o => Dc.XmlTextMapperAsync(text, o), query, parameters);

		public static List<Xml.XmlLiteNode> ReadXml(this IDataContext context, string query, params DataParameter[] parameters) => context.Map(Dc.XmlMapper, query, parameters)!;

		public static Task<List<Xml.XmlLiteNode>> ReadXmlAsync(this IDataContext context, string query, params DataParameter[] parameters) => context.MapAsync(Dc.XmlMapperAsync, query, parameters);

		public static List<RowsCollection> Records(this IDataContext context, int count, string query, params DataParameter[] parameters) => context.Map(Dc.RecordsMapper, query, parameters)!;
	}
}