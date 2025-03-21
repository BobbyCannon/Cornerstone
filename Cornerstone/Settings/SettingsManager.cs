#region References

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;
using Cornerstone.Runtime;
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
	where TEntity : Setting<TKey>, IClientEntity, new()
	where TDatabase : ISyncableDatabase
{
	#region Fields

	private readonly string _category;

	#endregion

	#region Constructors

	public SettingsManager(string category,
		IDatabaseProvider<TDatabase> databaseProvider,
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher
	) : base(databaseProvider, dateTimeProvider, dependencyProvider, dispatcher,
		(model, entity) => model.Name == entity.Name)
	{
		_category = category;
	}

	#endregion

	#region Properties

	protected override Func<PartialUpdateValue, TEntity, bool> LookupPredicate => (m, e) => m.Name == e.Name;

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

	/// <inheritdoc />
	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return base.HasChanges(settings)
			|| this.Any(x => x.HasChanges(settings));
	}

	/// <summary>
	/// Reset the settings.
	/// </summary>
	public override void Reset()
	{
		ResetHasChanges();
		base.Reset();
	}

	/// <inheritdoc />
	public override void ResetHasChanges()
	{
		this.ForEach(x => x.ResetHasChanges());
		base.ResetHasChanges();
	}

	/// <summary>
	/// Save the settings to the repository.
	/// </summary>
	public void Save(bool force = false)
	{
		if (!force && !HasChanges())
		{
			return;
		}

		using var database = DatabaseProvider.GetDatabase();
		var repository = database.GetRepository<TEntity, TKey>();

		foreach (var model in this.Where(x => x.HasChanges()))
		{
			var entity = repository.FirstOrDefault(x => x.Name == model.Name);
			if (entity == null)
			{
				entity = new TEntity();
				repository.Add(entity);
			}

			entity.CanSync = CanSync(model.Name);
			entity.Category = _category;
			entity.Name = model.Name;
			entity.Value = model.Value.ToRawJson();
			entity.ValueType = model.Type.ToAssemblyName();

			OnEntityUpdated(entity);
		}

		database.SaveChanges();

		OnSettingSaved();
		ResetHasChanges();
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
		try
		{
			AddValue(name, value);
			return value;
		}
		finally
		{
			OnSettingChanged(name);
		}
	}

	public string ToJson()
	{
		var partialUpdate = new PartialUpdate();
		partialUpdate.Load(this.ToArray());
		return partialUpdate.ToRawJson();
	}

	/// <summary>
	/// Reset the settings back to default value.
	/// </summary>
	public override void Uninitialize()
	{
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

	protected virtual void OnEntityUpdated(TEntity entity)
	{
	}

	/// <summary>
	/// Triggered when set is called for a setting.
	/// </summary>
	/// <param name="name"> The name of setting that changed. </param>
	protected virtual void OnSettingChanged(string name)
	{
		SettingChanged?.Invoke(this, name);
	}

	protected virtual void OnSettingSaved()
	{
		SettingSaved?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnViewUpdated(PartialUpdateValue view)
	{
		OnPropertyChanged(view.Name);
		base.OnViewUpdated(view);
	}

	protected override bool UpdateView(PartialUpdateValue view, TEntity update)
	{
		view.Type = update.GetValueType();
		view.Value = update.Value.FromJson(view.Type);
		view.Name = update.Name;
		return true;
	}

	private void AddValue<T>(string name, T value)
	{
		var model = new PartialUpdateValue(name, typeof(T), value);
		AddOrUpdate(model);
	}

	#endregion

	#region Events

	public event EventHandler<string> SettingChanged;
	public event EventHandler SettingSaved;

	#endregion
}