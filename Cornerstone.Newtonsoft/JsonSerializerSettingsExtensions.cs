#region References

using Cornerstone.Convert;
using Cornerstone.Newtonsoft.Converters;
using Cornerstone.Serialization;
using Cornerstone.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using IsoDateTimeConverter = Cornerstone.Newtonsoft.Converters.IsoDateTimeConverter;
using NewtonsoftIsoDateTimeConverter = Newtonsoft.Json.Converters.IsoDateTimeConverter;

#endregion

namespace Cornerstone.Newtonsoft;

/// <summary>
/// Extensions for the JsonSerializerSettings.
/// </summary>
public static class JsonSerializerSettingsExtensions
{
	#region Properties

	/// <summary>
	/// The default settings. This value is set by <see cref="InitializeSettings" />.
	/// </summary>
	public static JsonSerializerSettings DefaultSettings { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Initialize the Newtonsoft settings with Cornerstone settings and built in converters.
	/// </summary>
	/// <param name="settings"> The Newtonsoft settings to update. </param>
	/// <param name="updates"> The desired Cornerstone settings. </param>
	/// <param name="setAsDefault"> Use the updated settings as default. </param>
	public static void InitializeSettings(this JsonSerializerSettings settings, ISerializationSettings updates, bool setAsDefault)
	{
		settings.UpdateSettings(updates);

		settings.Converters.Add(new NewtonsoftIsoDateTimeConverter());
		settings.Converters.Add(new PartialUpdateConverter());
		settings.Converters.Add(new IsoDateTimeConverter());
		settings.Converters.Add(new OscTimeTagConverter());
		settings.Converters.Add(new IntPtrTimeConverter());
		settings.Converters.Add(new UIntPtrTimeConverter());
		settings.Converters.Add(new ShortGuidConverter());
		settings.Converters.Add(new VersionStringConverter());

		#if NET7_0_OR_GREATER
		settings.Converters.Add(new Int128TimeConverter());
		settings.Converters.Add(new UInt128TimeConverter());
		#endif

		if (updates.EnumFormat == EnumFormat.Name)
		{
			settings.Converters.Add(new StringEnumConverter());
		}

		if (setAsDefault)
		{
			DefaultSettings = settings;
		}
	}

	/// <summary>
	/// Update Newtonsoft settings with Cornerstone settings.
	/// </summary>
	/// <param name="settings"> The Newtonsoft settings to update. </param>
	/// <param name="updates"> The desired Cornerstone settings. </param>
	/// <remarks>
	/// https://www.newtonsoft.com/json/help/html/Properties_T_Newtonsoft_Json_JsonSerializerSettings.htm
	/// </remarks>
	public static void UpdateSettings(this JsonSerializerSettings settings, ISerializationSettings updates)
	{
		settings.ContractResolver = new JsonContractResolver(updates.NamingConvention);
		settings.DefaultValueHandling = updates.IgnoreDefaultValues ? DefaultValueHandling.Ignore : DefaultValueHandling.Populate;
		settings.Formatting = updates.TextFormat == TextFormat.Indented ? Formatting.Indented : Formatting.None;
		settings.MaxDepth = updates.MaxDepth is <= 0 or int.MaxValue ? null : updates.MaxDepth;
		settings.NullValueHandling = updates.IgnoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include;

		settings.DateParseHandling = DateParseHandling.DateTime;
		settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
		settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
		settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
		settings.PreserveReferencesHandling = PreserveReferencesHandling.None;
		settings.TypeNameHandling = TypeNameHandling.None;
	}

	#endregion
}