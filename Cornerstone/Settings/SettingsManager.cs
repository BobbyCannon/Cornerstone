#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a manager for a category of settings.
/// </summary>
/// <typeparam name="TSettings"> The type that contains the settings value. </typeparam>
/// <typeparam name="TEntity"> The type of the setting. </typeparam>
/// <typeparam name="TKey"> The type of the setting ID. </typeparam>
/// <typeparam name="TDatabase"> The database that stores the data. </typeparam>
public class SettingsManager<TSettings, TEntity, TKey, TDatabase>
	: ViewManagerForDatabase<PartialUpdateValue, TEntity, TKey, TDatabase>
	where TEntity : SettingSyncEntity<TKey>, IClientEntity, new()
	where TDatabase : ISyncableDatabase
{
	#region Fields

	private readonly ISettingsRepositoryProvider<TEntity, TKey, TDatabase> _settingRepositoryProvider;

	#endregion

	#region Constructors

	public SettingsManager(string category,
		ISettingsRepositoryProvider<TEntity, TKey, TDatabase> settingsRepositoryProvider,
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher
	) : base(settingsRepositoryProvider, dateTimeProvider, dependencyProvider, dispatcher,
		(model, entity) => string.Equals(model.Name, entity.Name, StringComparison.OrdinalIgnoreCase))
	{
		_settingRepositoryProvider = settingsRepositoryProvider;
		Category = category;
	}

	#endregion

	#region Properties

	public string Category { get; }

	protected override Func<TEntity, bool> LoadPredicate => x => (x.Category == Category) && !x.IsDeleted;

	protected override Func<PartialUpdateValue, TEntity, bool> LookupPredicate => (m, e) => (e.Category == Category) && (m.Name == e.Name);

	protected override Func<TEntity, bool> RefreshPredicate => x => (x.Category == Category) && !x.IsDeleted && base.RefreshPredicate(x);

	#endregion

	#region Methods

	/// <summary>
	/// Get the update for the provided name with a fallback default value if not found.
	/// </summary>
	/// <typeparam name="T"> The type to cast the value to. </typeparam>
	/// <param name="name"> The name of the update. </param>
	/// <returns> The value if it was found otherwise default(T). </returns>
	public T Get<T>([CallerMemberName] string name = "")
	{
		return Get<T>(() => default, name);
	}

	/// <summary>
	/// Get the property value.
	/// </summary>
	/// <typeparam name="TProperty"> The type to cast the value to. </typeparam>
	/// <param name="expression"> The expression of the member to set. </param>
	/// <param name="defaultValueFactory"> A default value factory if update not available. </param>
	/// <returns> The value if it was found otherwise default(T). </returns>
	public TProperty Get<TProperty>(Expression<Func<TSettings, TProperty>> expression, Func<TProperty> defaultValueFactory)
	{
		var propertyExpression = (MemberExpression) expression.Body;
		var name = propertyExpression.Member.Name;
		return Get(defaultValueFactory, name);
	}

	/// <summary>
	/// Get the update for the provided name with a fallback default value if not found.
	/// </summary>
	/// <typeparam name="T"> The type to cast the value to. </typeparam>
	/// <param name="defaultValueFactory"> A default value factory if update not available. </param>
	/// <param name="name"> The name of the update. </param>
	/// <returns> The value if it was found otherwise default(T). </returns>
	public T Get<T>(Func<T> defaultValueFactory, [CallerMemberName] string name = "")
	{
		var model = FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
		if (model != null)
		{
			return (T) model.Value;
		}

		var defaultValue = defaultValueFactory();
		AddValue(name, defaultValue);
		return defaultValue;
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return base.HasChanges(settings)
			|| this.Any(x => x.HasChanges(settings));
	}

	public override void ResetHasChanges()
	{
		this.ForEach(x => x.ResetHasChanges());
		base.ResetHasChanges();
	}

	/// <summary>
	/// Save the settings to the repository.
	/// </summary>
	public virtual void Save(bool force = false)
	{
		if (!CanSave() || (!force && !HasChanges()))
		{
			return;
		}

		var changedPropertyNames = GetChangedProperties().ToList();
		using var repository = _settingRepositoryProvider.GetSettingsRepository(Category);
		var changedProperties = this
			.Where(x => changedPropertyNames.Contains(x.Name) || x.HasChanges())
			.ToList();

		var skippedChanges = new List<string>(changedProperties.Count);

		foreach (var property in changedProperties)
		{
			if (!CanSaveSetting(property.Name))
			{
				skippedChanges.Add(property.Name);
				continue;
			}

			var entity = repository.Get(x => (x.Name == property.Name) && (x.Category == Category));
			if (entity == null)
			{
				entity = new TEntity { Name = property.Name, Category = Category };
				repository.Add(entity);
			}

			entity.CanSync = CanSyncSetting(property.Name);
			entity.Value = property.Value.ToRawJson();
			entity.ValueType = property.Type.ToAssemblyName();

			OnEntityUpdated(entity);
		}

		repository.SaveChanges();

		OnSettingSaved();
		ResetHasChanges();

		if (skippedChanges.Count > 0)
		{
			SetChangedProperties(skippedChanges);
		}
	}

	/// <summary>
	/// Set a property for the update.
	/// </summary>
	/// <param name="expression"> The expression of the member to set. </param>
	/// <param name="value"> The value of the member. </param>
	public void Set<TProperty>(Expression<Func<TSettings, TProperty>> expression, Func<TProperty> value)
	{
		Set(expression, value.Invoke());
	}

	/// <summary>
	/// Set a property for the update.
	/// </summary>
	/// <param name="expression"> The expression of the member to set. </param>
	/// <param name="value"> The value of the member. </param>
	public void Set<TProperty>(Expression<Func<TSettings, TProperty>> expression, TProperty value)
	{
		var propertyExpression = (MemberExpression) expression.Body;
		Set(value, propertyExpression.Member.Name);
	}

	/// <summary>
	/// Set a property for the update. The name must be available of the target value.
	/// </summary>
	/// <param name="value"> The value of the member. </param>
	/// <param name="name"> The name of the member to set. </param>
	public TData Set<TData>(TData value, [CallerMemberName] string name = "")
	{
		TData oldValue = default;
		try
		{
			oldValue = Get<TData>(name);
			AddValue(name, value);
			return value;
		}
		finally
		{
			OnSettingChanged(name, oldValue, value);
		}
	}

	public string ToJson()
	{
		var partialUpdate = new PartialUpdate();
		partialUpdate.Load(this.ToArray());
		return partialUpdate.ToRawJson();
	}

	/// <summary>
	/// Check to see if the settings can be saved.
	/// </summary>
	protected virtual bool CanSave()
	{
		// Only allow saving if load or refresh has been called
		return LastUpdated > DateTime.MinValue;
	}

	/// <summary>
	/// Check to see if the setting can save setting.
	/// Ex. Some device specific settings may not be ready to be saved.
	/// </summary>
	/// <param name="name"> The setting name to be tested. </param>
	/// <returns> True if the setting can be saved otherwise false. </returns>
	protected virtual bool CanSaveSetting(string name)
	{
		return true;
	}

	/// <summary>
	/// Check to see if the setting should be a local only setting. Any local setting cannot be synced.
	/// </summary>
	/// <param name="name"> The setting name to be tested. </param>
	/// <returns> True if the setting is local otherwise false. </returns>
	protected virtual bool CanSyncSetting(string name)
	{
		return false;
	}

	protected virtual void OnEntityUpdated(TEntity entity)
	{
	}

	/// <summary>
	/// Triggered when set is called for a setting.
	/// </summary>
	/// <param name="name"> The name of setting that changed. </param>
	protected virtual void OnSettingChanged<T>(string name, T oldValue, T newValue)
	{
		OnPropertyChanged(name, oldValue, newValue);
		SettingChanged?.Invoke(this, name);
	}

	protected virtual void OnSettingSaved()
	{
		SettingSaved?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnViewUpdated(PartialUpdateValue view)
	{
		NotifyComputedPropertyChanged(view.Name);
		base.OnViewUpdated(view);
	}

	protected override bool UpdateView(PartialUpdateValue view, TEntity update)
	{
		view.Name = update.Name;
		view.Type = update.GetValueType();
		TryUpdateViewValue(view, update.Value, view.Type);
		return true;
	}

	private void AddValue<T>(string name, T value)
	{
		var model = new PartialUpdateValue(name, typeof(T), value);
		AddOrUpdate(model);
	}

	private void TryUpdateViewValue(PartialUpdateValue update, string value, Type valueType)
	{
		if (value.TryFromJson(valueType, out var typeValue))
		{
			update.Value = typeValue;
		}
		else
		{
			// Default value if JSON is bad
			update.Value = SourceReflector.CreateInstance(valueType);
		}
	}

	#endregion

	#region Events

	public event EventHandler<string> SettingChanged;
	public event EventHandler SettingSaved;

	#endregion
}