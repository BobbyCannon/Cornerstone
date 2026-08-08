#region References

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.EntityFramework.Sql;

/// <summary>
/// Extension to support SQL only functionality
/// </summary>
public static class SqlExtensions
{
	#region Methods

	/// <summary>
	/// Convert the Queryable to a SQL query.
	/// </summary>
	/// <typeparam name="TEntity"> The entity type. </typeparam>
	/// <param name="query"> The query for the entity. </param>
	/// <returns> The SQL query for the queryable. </returns>
	public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
	{
		#if NETSTANDARD2_0
		using var enumerator = query.Provider.Execute<IEnumerable<TEntity>>(query.Expression).GetEnumerator();
		var relationalCommandCache = enumerator.Private("_relationalCommandCache");
		var selectExpression = relationalCommandCache.Private<SelectExpression>("_selectExpression");
		var factory = relationalCommandCache.Private<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");
		var sqlGenerator = factory.Create();
		var command = sqlGenerator.GetCommand(selectExpression);
		return command.CommandText;
		#else
		return query.ToQueryString();
		#endif
	}

	internal static IDictionary<string, IList<object>> AddIfMissing(this IDictionary<string, IList<object>> dictionary, string key, object value)
	{
		if (dictionary.ContainsKey(key))
		{
			if (!dictionary[key].Contains(value))
			{
				dictionary[key].Add(value);
			}
		}
		else
		{
			var list = new List<object> { value };
			dictionary.Add(key, list);
		}

		return dictionary;
	}

	internal static IDictionary<string, IList<string>> AddIfMissing(this IDictionary<string, IList<string>> dictionary, string key, string value)
	{
		if (dictionary.ContainsKey(key))
		{
			if (!dictionary[key].Contains(value))
			{
				dictionary[key].Add(value);
			}
		}
		else
		{
			var list = new List<string> { value };
			dictionary.Add(key, list);
		}

		return dictionary;
	}

	#endregion
}