#region References

using System;
using System.IO;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json;
using Cornerstone.Text;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

#endregion

namespace Cornerstone.Newtonsoft;

/// <inheritdoc />
public class NewtonsoftJsonSerializer : IJsonSerializer
{
	#region Methods

	/// <inheritdoc />
	public T FromJson<T>(string value, ISerializationSettings settings = null)
	{
		return JsonConvert.DeserializeObject<T>(value, JsonSerializerSettingsExtensions.DefaultSettings);
	}

	/// <inheritdoc />
	public object FromJson(string value, Type type, ISerializationSettings settings = null)
	{
		return JsonConvert.DeserializeObject(value, type, JsonSerializerSettingsExtensions.DefaultSettings);
	}

	/// <inheritdoc />
	public string ToJson(object value, Type type, ISerializationSettings settings = null)
	{
		return JsonConvert.SerializeObject(value, type, JsonSerializerSettingsExtensions.DefaultSettings);
	}

	/// <inheritdoc />
	public string ToJson<T>(T value, ISerializationSettings settings = null)
	{
		if (settings == null)
		{
			return JsonConvert.SerializeObject(value, JsonSerializerSettingsExtensions.DefaultSettings);
		}

		var jsonSettings = new JsonSerializerSettings();
		// todo: support JsonSerializerSettingsExtensions.DefaultSettings
		jsonSettings.InitializeSettings(settings, false);
		return JsonConvert.SerializeObject(value, jsonSettings);
		
		//var jsonSerializer = new JsonSerializer();

		//using var fs = new MemoryStream();
		//using var sw = new StreamWriter(fs);
		//using var jtw = new JsonTextWriter(sw)
		//{
		//	Formatting = jsonSettings.Formatting,
		//	IndentChar = '\t',
		//	QuoteName = true,
		//	Indentation = 1,
		//	QuoteChar = '"',
		//	DateFormatHandling = jsonSettings.DateFormatHandling,
		//	DateTimeZoneHandling = jsonSettings.DateTimeZoneHandling,
		//	StringEscapeHandling = jsonSettings.StringEscapeHandling,
		//	FloatFormatHandling = jsonSettings.FloatFormatHandling,
		//	DateFormatString = jsonSettings.DateFormatString,
		//	Culture = jsonSettings.Culture
		//};
		//jsonSerializer.Serialize(jtw, value);
		//return fs.ToString();
	}

	#endregion
}