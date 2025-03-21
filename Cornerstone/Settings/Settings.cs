#region References

using System.Collections.Generic;
using System.ComponentModel;
using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Serialization;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a set of settings to load and save to a file in the JSON format.
/// </summary>
public abstract class Settings<T> : PartialUpdate<T>
{
	#region Fields

	private readonly SerializationSettings _serializationSettings;

	#endregion

	#region Constructors

	/// <summary>
	/// For serialization, do not use.
	/// </summary>
	protected Settings() : this(null)
	{
	}

	/// <summary>
	/// Create an instance of the settings file.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected Settings(IDispatcher dispatcher) : base(dispatcher)
	{
		_serializationSettings = new SerializationSettings
		{
			EnumFormat = EnumFormat.Value,
			IgnoreDefaultValues = true,
			IgnoreNullValues = true,
			IgnoreReadOnly = true,
			MaxDepth = int.MaxValue,
			NamingConvention = NamingConvention.PascalCase,
			TextFormat = TextFormat.None,
			UpdateableAction = UpdateableAction.PartialUpdate
		};
	}

	#endregion

	#region Properties

	/// <summary>
	/// True if loaded.
	/// </summary>
	[Browsable(false)]
	public bool IsLoaded { get; private set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
	{
		var response = GetType().GetDefaultIncludedProperties(action);
		if (action == UpdateableAction.PartialUpdate)
		{
			response.Remove(nameof(IsLoaded));
		}
		return response;
	}

	/// <summary>
	/// Reset the settings.
	/// </summary>
	public override void Reset()
	{
		base.Reset();
		ResetToDefaults();
		ResetHasChanges();
	}

	/// <summary>
	/// Serialize the settings file to JSON.
	/// </summary>
	/// <returns> The settings in JSON format. </returns>
	public string ToJson()
	{
		RefreshUpdates();
		var json = Serializer.Instance.ToJson(this, _serializationSettings);
		return json;
	}

	/// <summary>
	/// Finalize the load.
	/// </summary>
	protected virtual void FinalizeLoad()
	{
		IsLoaded = true;
	}

	/// <summary>
	/// Reset all values to the default state.
	/// </summary>
	protected virtual void ResetToDefaults()
	{
		IsLoaded = false;
	}

	#endregion
}