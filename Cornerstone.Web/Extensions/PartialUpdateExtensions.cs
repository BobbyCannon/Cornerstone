#region References

using System.Collections;
using System.Text;
using System.Web;
using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Web.Extensions;

/// <summary>
/// Extensions for PartialUpdate
/// </summary>
public static class PartialUpdateExtensions
{
	#region Methods

	/// <summary>
	/// Parse the paged request values from the query string.
	/// </summary>
	/// <param name="queryString"> The query string to process. </param>
	/// <remarks>
	/// see https://www.ietf.org/rfc/rfc2396.txt for details on url decoding
	/// </remarks>
	public static T ParseQueryString<T>(string queryString)
		where T : PartialUpdate, new()
	{
		var response = new T();
		response.ParseQueryString(queryString);
		return response;
	}

	/// <summary>
	/// Parse the query string into the partial update.
	/// </summary>
	/// <param name="partialUpdate"> The partial update to update. </param>
	/// <param name="queryString"> The query string to process. </param>
	/// <remarks>
	/// see https://www.ietf.org/rfc/rfc2396.txt for details on url decoding
	/// </remarks>
	public static T ParseQueryString<T>(this T partialUpdate, string queryString)
		where T : PartialUpdate, new()
	{
		var collection = HttpUtility.ParseQueryString(queryString);
		var properties = typeof(T).GetCachedPropertyDictionary();

		foreach (var key in collection.AllKeys)
		{
			if (key == null)
			{
				continue;
			}

			if (properties.TryGetValue(key, out var property))
			{
				if (collection.Get(key).TryConvertTo(property.PropertyType, out var result))
				{
					partialUpdate.AddOrUpdate(property.Name, property.PropertyType, result);
					continue;
				}
			}

			if (key.EndsWith("[]"))
			{
				var newKey = key.Substring(0, key.Length - 2);
				var newValue = collection.Get(key)?.Split(',');
				partialUpdate.AddOrUpdate(newKey, typeof(string[]), newValue);
				continue;
			}

			var value = collection.Get(key);
			partialUpdate.AddOrUpdate(key, typeof(string), value);
		}

		return partialUpdate;
	}

	/// <summary>
	/// Convert the request to the query string values.
	/// </summary>
	/// <returns> The request in a query string format. </returns>
	/// <remarks>
	/// see https://www.ietf.org/rfc/rfc2396.txt for details on url encoding
	/// </remarks>
	public static string ToQueryString(this PartialUpdate partialUpdate)
	{
		var builder = new StringBuilder();

		foreach (var update in partialUpdate.GetUpdates())
		{
			// https://www.ietf.org/rfc/rfc2396.txt
			var name = HttpUtility.UrlEncode(update.Name);

			if (update.Value is not string
				&& update.Value is IEnumerable e)
			{
				foreach (var item in e)
				{
					builder.Append($"&{name}[]={HttpUtility.UrlEncode(item.ToString())}");
				}
				continue;
			}

			// todo: should I use the converter to convert value to string?

			var value = HttpUtility.UrlEncode(update.Value.ToString());
			builder.Append($"&{name}={value}");
		}

		if ((builder.Length > 0) && (builder[0] == '&'))
		{
			builder.Remove(0, 1);
		}

		return builder.ToString();
	}

	#endregion
}