using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Data;

#if NET

public static partial class DataContextExtensions
{
	/// <summary>
	/// Executes the specified SQL <paramref name="query"/> and returns first column of the first row as a value of type <typeparamref name="T"/> or <paramref name="defaultValue"/>.
	/// </summary>
	/// <typeparam name="T">Type of the returning value</typeparam>
	/// <param name="context"><see cref="IDataContext"/></param>
	/// <param name="defaultValue">Default value.</param>
	/// <param name="query">The SQL query to execute.</param>
	/// <param name="parameters">Parameters to be used in the query.</param>
	/// <returns></returns>
	public static T GetValueOrDefault<T>(this IDataContext context, T defaultValue, ref SqlInterpolatedHandler query, params DataParameter[] parameters) where T: class
		=> Map(context ?? throw new ArgumentNullException(nameof(context)), o => Dc.ValueMapper<T>(o, defaultValue), query.ToSqlQuery(), parameters);

	/// <summary>
	/// Executes the specified SQL <paramref name="query"/> and returns first column of the first row as a value of type <typeparamref name="T"/> or <paramref name="defaultValue"/>.
	/// </summary>
	/// <typeparam name="T">Type of the returning value</typeparam>
	/// <param name="context"><see cref="IDataContext"/></param>
	/// <param name="defaultValue">Default value.</param>
	/// <param name="query">The SQL query to execute.</param>
	/// <param name="parameters">Parameters to be used in the query.</param>
	/// <returns></returns>
	public static Task<T> GetValueOrDefaultAsync<T>(this IDataContext context, T defaultValue, ref SqlInterpolatedHandler query, params DataParameter[] parameters) where T: class
		=> MapAsync(context ?? throw new ArgumentNullException(nameof(context)), o => Dc.ValueMapperAsync<T>(o, defaultValue), query.ToSqlQuery(), parameters);

	/// <summary>
	/// Executes the specified SQL <paramref name="query"/> and evaluates the specified <paramref name="mapper"/> for each row.
	/// </summary>
	/// <param name="context"><see cref="IDataContext"/>.</param>
	/// <param name="limit">Maximum number of rows to return (-1 - unlimited)</param>
	/// <param name="mapper">Action to be executed for each <see cref="IDataRecord"/>.</param>
	/// <param name="query">SQL query to execute.</param>
	/// <param name="parameters">Parameters to be used in the query.</param>
	/// <returns>Number of processed rows</returns>
	/// <exception cref="ArgumentNullException">Query, context, or mapper is null.</exception>
	public static int Map(this IDataContext context, int limit, Action<IDataRecord> mapper, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		if (mapper == null)
			throw new ArgumentNullException(nameof(mapper));
		return context.Map(o => ActionMapper(o, limit, mapper), query.ToSqlQuery(), parameters);

		static int ActionMapper(DbCommand cmd, int limit, Action<IDataRecord> mapper)
		{
			using DbDataReader reader = cmd.ExecuteReader();
			int count = 0;
			while (count != limit && reader.Read())
			{
				++count;
				mapper(reader);
			}
			return count;
		}
	}

	/// <summary>
	/// Executes the specified SQL <paramref name="query"/> and evaluates the specified <paramref name="mapper"/> for each row.
	/// </summary>
	/// <param name="context"><see cref="IDataContext"/>.</param>
	/// <param name="mapper">Action to be executed for each <see cref="IDataRecord"/>.</param>
	/// <param name="query">SQL query to execute.</param>
	/// <param name="parameters">Parameters to be used in the query.</param>
	/// <returns>Number of processed rows</returns>
	/// <exception cref="ArgumentNullException">Query, context, or mapper is null.</exception>
	public static int Map(this IDataContext context, Action<IDataRecord> mapper, ref SqlInterpolatedHandler query, params DataParameter[] parameters) => Map(context, -1, mapper, ref query, parameters);

	public static T Map<T>(this IDataContext context, Func<DbCommand, T> mapper, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		if (mapper == null)
			throw new ArgumentNullException(nameof(mapper));
		return context.Map(mapper, context.Command(query.ToSqlQuery(), parameters));
	}

	public static Task<int> MapAsync(this IDataContext context, int limit, Action<IDataRecord> mapper, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		if (mapper == null)
			throw new ArgumentNullException(nameof(mapper));
		return context.MapAsync(o => ActionMapper(o, limit, mapper), query.ToSqlQuery(), parameters);

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

	public static Task<int> MapAsync(this IDataContext context, Action<IDataRecord> mapper, SqlInterpolatedHandler query, params DataParameter[] parameters) => MapAsync(context, -1, mapper, ref query, parameters);

	public static Task<T> MapAsync<T>(this IDataContext context, Func<DbCommand, Task<T>> mapper, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		if (mapper == null)
			throw new ArgumentNullException(nameof(mapper));
		return context.MapAsync(mapper, context.Command(query.ToSqlQuery(), parameters));
	}

	public static T? GetValue<T>(this IDataContext context, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
		=> Map(context ?? throw new ArgumentNullException(nameof(context)), Dc.ValueMapper<T>, ref query, parameters);

	public static Task<T?> GetValueAsync<T>(this IDataContext context, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
		=> MapAsync(context ?? throw new ArgumentNullException(nameof(context)), Dc.ValueMapperAsync<T>, ref query, parameters);

	public static List<T> GetList<T>(this IDataContext context, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
		=> Map(context ?? throw new ArgumentNullException(nameof(context)), Dc.ListMapper<T>, ref query, parameters);

	public static Task<List<T>> GetListAsync<T>(this IDataContext context, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
		=> MapAsync(context ?? throw new ArgumentNullException(nameof(context)), Dc.ListMapperAsync<T>, ref query, parameters);

	public static bool ReadXmlText(this IDataContext context, TextWriter text, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
		=> Map(context ?? throw new ArgumentNullException(nameof(context)), o => Dc.XmlTextMapper(text, o), ref query, parameters);

	public static Task<bool> ReadXmlTextAsync(this IDataContext context, TextWriter text, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
		=> MapAsync(context ?? throw new ArgumentNullException(nameof(context)), o => Dc.XmlTextMapperAsync(text, o), ref query, parameters);

	public static List<Xml.IXmlReadOnlyNode> ReadXml(this IDataContext context, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
		=> Map(context ?? throw new ArgumentNullException(nameof(context)), Dc.XmlMapper, ref query, parameters);

	public static Task<List<Xml.IXmlReadOnlyNode>> ReadXmlAsync(this IDataContext context, ref SqlInterpolatedHandler query, params DataParameter[] parameters)
		=> MapAsync(context ?? throw new ArgumentNullException(nameof(context)), Dc.XmlMapperAsync, ref query, parameters);
}

#endif