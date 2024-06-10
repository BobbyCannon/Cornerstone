#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;
using PropertyChanged;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a manager for a category of settings.
/// </summary>
/// <typeparam name="T"> The type of the setting. </typeparam>
/// <typeparam name="T2"> The type of the setting ID. </typeparam>
public abstract class SettingsManager<T, T2> : Manager
	where T : Setting<T2>
{
	#region Fields

	private readonly string _category;
	private IList<PropertyInfo> _properties;
	private readonly UpdateableOptions _settingOptions;
	private readonly IDictionary<string, Setting<T2>> _trackedSettings;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the settings manager.
	/// </summary>
	/// <param name="category"> The category name. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected SettingsManager(string category, IDispatcher dispatcher) : base(dispatcher)
	{
		_category = category;
		_trackedSettings = new Dictionary<string, Setting<T2>>();
		_settingOptions = new UpdateableOptions(UpdateableAction.Updateable, [nameof(Setting<T>.Value)], null, true);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The predicate to load settings from storage mechanism.
	/// </summary>
	protected virtual Func<T, bool> LoadPredicate => x => !x.IsDeleted && (x.Category == _category);

	#endregion

	#region Methods

	/// <summary>
	/// Get the repository to store the settings.
	/// </summary>
	/// <returns> The repository to store the settings. </returns>
	public abstract ISettingsRepository<T, T2> GetRepository();

	/// <inheritdoc />
	public override bool HasChanges(params string[] exclusions)
	{
		return exclusions.Any()
			? _trackedSettings
				.Values
				.Where(x => !exclusions.Contains(x.Name))
				.Any(x => x.HasChanges(exclusions))
			: _trackedSettings
				.Values
				.Any(x => x.HasChanges());
	}

	/// <summary>
	/// Load the settings from storage.
	/// </summary>
	public void Load()
	{
		using var repository = GetRepository();
		var entities = repository.Load(LoadPredicate);

		foreach (var entity in entities)
		{
			Load(entity);
		}

		//base.Load();
		ResetHasChanges();
	}

	/// <summary>
	/// Reset the settings back to default value.
	/// </summary>
	public override void Uninitialize()
	{
		_trackedSettings.ForEach(x => x.Value.ResetToDefault());
	}

	/// <inheritdoc />
	public override void ResetHasChanges()
	{
		_trackedSettings.ForEach(x => x.Value.ResetHasChanges());
		base.ResetHasChanges();
	}

	/// <summary>
	/// Save the settings to the repository.
	/// </summary>
	public void Save()
	{
		using var repository = GetRepository();

		foreach (var setting in _trackedSettings)
		{
			repository.AddOrUpdate(setting.Value);
		}

		repository.SaveChanges();

		ResetHasChanges();
	}

	/// <summary>
	/// Add or update a track setting (local).
	/// </summary>
	/// <param name="name"> The name of the settings. </param>
	/// <param name="defaultValue"> The default value for the setting. </param>
	/// <param name="update"> An optional update action. </param>
	/// <typeparam name="TData"> The type of the data for the setting. </typeparam>
	/// <returns> The new setting. </returns>
	protected Setting<TData, T2> AddOrUpdateTrackedSetting<TData>(string name, TData defaultValue = default, Action<Setting<TData, T2>> update = null)
	{
		Setting<TData, T2> response;
		if (_trackedSettings.TryGetValue(name, out var setting))
		{
			response = (Setting<TData, T2>) setting;
			response.CanSync = CanSync(name);
			response.Data = defaultValue;
			update?.Invoke((Setting<TData, T2>) setting);
		}
		else
		{
			response = new Setting<TData, T2>(name, defaultValue) { CanSync = CanSync(name) };
			update?.Invoke(response);
			_trackedSettings.Add(name, response);
		}
		return response;
	}

	/// <summary>
	/// Add or update a track setting (local).
	/// </summary>
	/// <param name="name"> The name of the settings. </param>
	/// <param name="type"> The type of the data value. </param>
	/// <param name="defaultValue"> The default value for the setting. </param>
	/// <param name="update"> An optional update action. </param>
	/// <returns> The new setting. </returns>
	protected Setting<T2> AddOrUpdateTrackedSetting(string name, Type type, object defaultValue = default, Action<Setting<T2>> update = null)
	{
		Setting<T2> response;
		if (_trackedSettings.TryGetValue(name, out var setting))
		{
			response = setting;
			response.Value = defaultValue.ToJson();
			update?.Invoke(setting);
		}
		else
		{
			response = (Setting<T2>) typeof(Setting<,>).CreateInstanceOfGeneric([type, typeof(T2)], name, defaultValue, GetDispatcher());
			update?.Invoke(response);
			_trackedSettings.Add(name, response);
		}
		return response;
	}

	/// <summary>
	/// Check to see if the setting should be a local only setting. Any local setting cannot be synced.
	/// </summary>
	/// <param name="name"> The setting name to be tested. </param>
	/// <returns> True if the setting is local otherwise false. </returns>
	protected virtual bool CanSync(string name)
	{
		return false;
	}

	/// <summary>
	/// Get the value of a tracked setting.
	/// </summary>
	/// <typeparam name="TData"> The type of the Data value. </typeparam>
	/// <param name="name"> The name of the setting. </param>
	/// <param name="defaultValue"> The default value to return if not found. </param>
	/// <returns> The value read or the default value. </returns>
	protected TData Get<TData>(string name, TData defaultValue = default)
	{
		if (!_trackedSettings.TryGetValue(name, out var setting))
		{
			return AddOrUpdateTrackedSetting(name, defaultValue, x => x.ResetHasChanges()).Data;
		}

		if (setting is Setting<TData, T2> typedSetting)
		{
			return typedSetting.Data;
		}

		return setting.Value.FromJson<TData>();
	}

	/// <summary>
	/// Triggered when set is called for a setting.
	/// </summary>
	/// <param name="name"> The name of setting that changed. </param>
	[SuppressPropertyChangedWarnings]
	protected virtual void OnSettingChanged(string name)
	{
		SettingChanged?.Invoke(this, name);
	}

	/// <summary>
	/// Get the value of a setting.
	/// </summary>
	/// <typeparam name="TData"> The type of the Data value. </typeparam>
	/// <param name="name"> The name of the setting. </param>
	/// <param name="value"> The value to be set. </param>
	/// <returns> The value set. </returns>
	protected TData Set<TData>(string name, TData value)
	{
		try
		{
			if (!_trackedSettings.TryGetValue(name, out var setting))
			{
				return AddOrUpdateTrackedSetting(name, value).Data;
			}

			if (setting is Setting<TData, T2> typeSetting)
			{
				typeSetting.Data = value;
				return typeSetting.Data;
			}

			setting.Value = value.ToJson();
			return value;
		}
		finally
		{
			OnSettingChanged(name);
		}
	}

	private PropertyInfo GetPropertyByName(string propertyName)
	{
		_properties ??= GetType().GetCachedProperties();
		return _properties?.FirstOrDefault(x => x.Name == propertyName);
	}

	private void Load(T entity)
	{
		var property = GetPropertyByName(entity.Name);
		if (property == null)
		{
			return;
		}

		if (property.PropertyType.ImplementsType<ISetting>())
		{
			var instance = (ISetting) property.PropertyType.CreateInstance();
			var settingType = Type.GetType(instance.ValueType);
			AddOrUpdateTrackedSetting(entity.Name,
				settingType,
				entity.Value.FromJson(settingType),
				x => x.ResetHasChanges()
			);
			return;
		}

		AddOrUpdateTrackedSetting(entity.Name,
			property.PropertyType,
			entity.Value.FromJson(property.PropertyType),
			x => x.ResetHasChanges()
		);
	}

	#endregion

	#region Events

	/// <summary>
	/// Fires when a setting changed.
	/// </summary>
	public event EventHandler<string> SettingChanged;

	#endregion
}