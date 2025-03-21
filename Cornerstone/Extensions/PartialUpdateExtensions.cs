#region References

using System.Collections;
using System.Text;
using System.Web;
using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Extensions;

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
		if (partialUpdate == null)
		{
			return string.Empty;
		}

		var builder = new StringBuilder();
		var defaultValues = partialUpdate.GetType().CreateInstance() as PartialUpdate;
		
		foreach (var update in partialUpdate.GetUpdates())
		{
			if (update.Value.IsDefaultValue()
				|| update.IsDefaultValue(defaultValues))
			{
				continue;
			}

			// https://www.ietf.org/rfc/rfc2396.txt
			var name = HttpUtility.UrlEncode(update.Name);

			if (update.Value is not string
				&& update.Value is IEnumerable e)
			{
				foreach (var item in e)
				{
					var itemString = item.ToString();
					if (string.IsNullOrEmpty(itemString))
					{
						continue;
					}
					builder.Append($"&{name}[]={HttpUtility.UrlEncode(itemString)}");
				}
				continue;
			}

			// todo: should I use the converter to convert value to string?

			var valueString = update.Value.ToString();
			if (string.IsNullOrEmpty(valueString))
			{
				continue;
			}

			var value = HttpUtility.UrlEncode(valueString);
			builder.Append($"&{name}={value}");
		}

		if ((builder.Length > 0) && (builder[0] == '&'))
		{
			builder.Remove(0, 1);
		}

		return builder.ToString();
	}

	public static bool IsDefaultValue(this PartialUpdateValue update, PartialUpdate defaultValues)
	{
		if ((defaultValues != null)
			&& (defaultValues.TryGetMemberValue(update.Name.ToPascalCase(), out var memberValue))
			&& (Equals(memberValue, update.Value)))
		{
			return true;
		}

		return false;
	}

	#endregion
}