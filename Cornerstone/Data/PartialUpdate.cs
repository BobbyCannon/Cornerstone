#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cornerstone.Convert;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// This class contains updates for another object. JSON is deserialized into the
/// provided type.
/// </summary>
/// <typeparam name="T"> The type of object to be updated. </typeparam>
/// <remarks>
/// Meaning, if you create an "AccountUpdate" that inherits
/// "PartialUpdate[Account]" the updates are for an "Account".
/// You can also build an "Account" that inherits "PartialUpdate[Account]" to
/// updates the account itself.
/// </remarks>
public class PartialUpdate<T> : PartialUpdate
{
	#region Constructors

	/// <summary>
	/// Initializes a partial update.
	/// </summary>
	public PartialUpdate() : this(null)
	{
	}

	/// <summary>
	/// Initializes a partial update.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public PartialUpdate(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Add the model values to the partial update.
	/// </summary>
	public void FromModel(T model)
	{
		var properties = GetTargetProperties();

		foreach (var property in properties)
		{
			Set(property.Value.GetValue(model), property.Key);
		}
	}

	/// <summary>
	/// Get the property value.
	/// </summary>
	/// <typeparam name="TProperty"> The type to cast the value to. </typeparam>
	/// <param name="expression"> The expression of the member to set. </param>
	/// <param name="defaultValue"> A default value if update not available. </param>
	/// <returns> The value if it was found otherwise default(T). </returns>
	public TProperty Get<TProperty>(Expression<Func<T, TProperty>> expression, TProperty defaultValue)
	{
		var propertyExpression = (MemberExpression) expression.Body;
		return Get(defaultValue, propertyExpression.Member.Name);
	}

	/// <summary>
	/// Set a property for the update.
	/// </summary>
	/// <param name="expression"> The expression of the member to set. </param>
	/// <param name="value"> The value of the member. </param>
	public void Set<TProperty>(Expression<Func<T, TProperty>> expression, TProperty value)
	{
		var propertyExpression = (MemberExpression) expression.Body;
		Set(value, propertyExpression.Member.Name);
	}

	/// <summary>
	/// Get the model from the partial updates.
	/// </summary>
	/// <returns> The model. </returns>
	public T ToModel()
	{
		var response = Activator.CreateInstance<T>();
		var properties = GetTargetProperties();

		foreach (var property in properties)
		{
			if (TryGet(property.Value, out var value))
			{
				property.Value.SetValue(response, value);
			}
		}

		return response;
	}

	/// <summary>
	/// Gets a list of property information for provided type.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <returns> The list of properties for the provided type. </returns>
	protected internal override IDictionary<string, PropertyInfo> GetTargetProperties()
	{
		return typeof(T).GetCachedPropertyDictionary();
	}

	#endregion
}

/// <summary>
/// This class contains updates for itself. JSON is deserialized into the declaring type.
/// Meaning, if you create an "AccountUpdate" that inherits from "PartialUpdate" then the
/// updates are for the "AccountUpdate" itself.
/// </summary>
public class PartialUpdate : Bindable
{
	#region Fields

	/// <summary>
	/// A list of updates for this partial update.
	/// </summary>
	private readonly IDictionary<string, PartialUpdateValue> _updates;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a partial update.
	/// </summary>
	public PartialUpdate() : this(null)
	{
	}

	/// <summary>
	/// Initializes a partial update.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public PartialUpdate(IDispatcher dispatcher) : base(dispatcher)
	{
		_updates = new SortedDictionary<string, PartialUpdateValue>(StringComparer.OrdinalIgnoreCase);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Add or update the value with the type.
	/// </summary>
	/// <param name="name"> The name of the update to add. </param>
	/// <param name="value"> The value of the update. </param>
	public void AddOrUpdate(string name, object value)
	{
		var properties = GetRealType().GetCachedProperties();
		var property = properties.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
		var type = value == null ? property?.PropertyType ?? typeof(object) : value.GetType();
		AddOrUpdate(name, type, value);
	}

	/// <summary>
	/// Add or update the value with the type.
	/// </summary>
	/// <param name="name"> The name of the update to add. </param>
	/// <param name="type"> The type of the value. </param>
	/// <param name="value"> The value of the update. </param>
	public void AddOrUpdate(string name, Type type, object value)
	{
		if (!_updates.TryGetValue(name, out var update))
		{
			// todo: double check property value type?
			_updates.Add(name, new PartialUpdateValue(name, type, value));
			return;
		}

		if (!string.Equals(update.Name, name, StringComparison.Ordinal))
		{
			// Rename the key...
			_updates.Remove(update.Name);
			update.Name = name;
			_updates.Add(update.Name, update);
		}

		if (value == null)
		{
			// todo: do we need to check for nullable type here
			// bug: should we throw exceptions for non-nullable types 
			update.Value = null;
			return;
		}

		// todo: do not convert if the ToType is a subtype
		var valueType = value.GetType();
		var isType = update.Type == valueType;
		var inheritsType = valueType.ImplementsType(update.Type);

		update.Value = isType || inheritsType
			? value
			: value.ConvertTo(update.Type);
	}

	/// <summary>
	/// True if the partial update contains an update value.
	/// </summary>
	/// <param name="name"> The update name. </param>
	/// <returns> True if the update is available otherwise false. </returns>
	public bool ContainsUpdate(string name)
	{
		return _updates.ContainsKey(name);
	}

	/// <summary>
	/// Get the update for the provided name with a fallback default value if not found.
	/// </summary>
	/// <typeparam name="T"> The type to cast the value to. </typeparam>
	/// <param name="name"> The name of the update. </param>
	/// <returns> The value if it was found otherwise default(T). </returns>
	public T Get<T>(string name)
	{
		return Get(default(T), name);
	}

	/// <summary>
	/// Get the update for the provided name with a fallback default value if not found.
	/// </summary>
	/// <typeparam name="T"> The type to cast the value to. </typeparam>
	/// <param name="defaultValue"> A default value if update not available. </param>
	/// <param name="name"> The name of the update. </param>
	/// <returns> The value if it was found otherwise default(T). </returns>
	public T Get<T>(T defaultValue = default, [CallerMemberName] string name = "")
	{
		return !_updates.TryGetValue(name, out var update)
			? defaultValue
			: ConvertOrDefault(update, defaultValue);
	}

	/// <summary>
	/// Gets a list of updates.
	/// </summary>
	/// <returns> The updates for the partial update. </returns>
	public IEnumerable<PartialUpdateValue> GetUpdates()
	{
		// todo: should we clone?
		return _updates.Values;
	}

	/// <summary>
	/// Remove an update from the partial update.
	/// </summary>
	/// <param name="name"> The name of the update to remove. </param>
	public void Remove(string name)
	{
		_updates.Remove(name);
	}

	/// <summary>
	/// Set a property for the update. The name must be available of the target value.
	/// </summary>
	/// <param name="value"> The value of the member. </param>
	/// <param name="name"> The name of the member to set. </param>
	public void Set<T>(T value, [CallerMemberName] string name = "")
	{
		var property = GetTargetProperty(name);
		Set(name, value, property);
	}

	/// <summary>
	/// Create a dynamic object of the partial update.
	/// </summary>
	/// <returns> The dynamic version of the partial update. </returns>
	public virtual IDictionary<string, object> ToDictionary()
	{
		var dictionary = new Dictionary<string, object>();

		RefreshUpdates();

		foreach (var update in _updates)
		{
			dictionary.AddOrUpdate(update.Key, update.Value.Value);
		}

		return dictionary;
	}

	/// <summary>
	/// Try to get the update for the provided name.
	/// </summary>
	/// <typeparam name="T"> The type to cast the value to. </typeparam>
	/// <param name="value"> The value read if successful. </param>
	/// <param name="name"> The name of the update. </param>
	/// <returns> True if the value was read otherwise false. </returns>
	public bool TryGet<T>(out T value, [CallerMemberName] string name = "")
	{
		if (_updates.TryGetValue(name, out var update))
		{
			return update.Value.TryConvertTo(out value);
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Try to get the value for the provided name then call the provided update.
	/// </summary>
	/// <typeparam name="T"> The type to cast the value to. </typeparam>
	/// <param name="name"> The name of the update. </param>
	/// <param name="update"> The update to invoke if read successful. </param>
	/// <returns> True if the value was read otherwise false. </returns>
	public bool TryGet<T>(string name, Action<T> update)
	{
		if (!TryGet<T>(out var value, name))
		{
			return false;
		}

		update.Invoke(value);
		return true;
	}

	/// <summary>
	/// Try to get the update for the provided name.
	/// </summary>
	/// <param name="propertyInfo"> The property type to cast the value to. </param>
	/// <param name="value"> The value read if successful. </param>
	/// <returns> True if the value was read otherwise false. </returns>
	public bool TryGet(PropertyInfo propertyInfo, out object value)
	{
		if (_updates.TryGetValue(propertyInfo.Name, out var update))
		{
			return update.Value.TryConvertTo(propertyInfo.PropertyType, out value);
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Get the property value.
	/// </summary>
	/// <param name="value"> The value that was retrieve or default value if not found. </param>
	/// <param name="name"> The name of the update. </param>
	/// <returns> True if the update was found otherwise false. </returns>
	public bool TryGet(out object value, [CallerMemberName] string name = "")
	{
		try
		{
			if (_updates.TryGetValue(name, out var update))
			{
				value = update.Value;
				return true;
			}

			value = default;
			return false;
		}
		catch
		{
			value = default;
			return false;
		}
	}

	/// <summary>
	/// Update the PartialUpdate with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(PartialUpdate update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <summary>
	/// Update the PartialUpdate with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(PartialUpdate update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		foreach (var item in update._updates)
		{
			_updates.AddOrUpdate(item.Key, item.Value);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			PartialUpdate value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <summary>
	/// Gets a list of property information for this type.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <returns> The list of properties for this type. </returns>
	protected internal virtual IDictionary<string, PropertyInfo> GetTargetProperties()
	{
		return GetType().GetCachedPropertyDictionary();
	}

	/// <summary>
	/// Refresh the update collection for this partial update.
	/// </summary>
	protected internal virtual void RefreshUpdates()
	{
		var options = GetDefaultIncludedProperties(UpdateableAction.PartialUpdate);
		var properties = GetRealType().GetCachedProperties();

		foreach (var option in options)
		{
			var property = properties.FirstOrDefault(x => x.Name == option);
			if ((property == null) || !property.CanRead)
			{
				continue;
			}

			var value = property.GetValue(this);
			AddOrUpdate(property.Name, property.PropertyType, value);
		}
	}

	/// <summary>
	/// Gets a property information for this type.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <returns> The property for the target type or null if not found. </returns>
	protected virtual PropertyInfo GetTargetProperty(string name)
	{
		if (GetTargetProperties().TryGetValue(name, out var value))
		{
			return value;
		}

		throw new NotSupportedException();
	}

	/// <summary>
	/// Reconcile the update collections.
	/// </summary>
	/// <param name="partialUpdate"> The partial update to reconcile with. </param>
	protected void Reconcile(PartialUpdate partialUpdate)
	{
		_updates.Reconcile(partialUpdate._updates);
	}

	private static T ConvertOrDefault<T>(PartialUpdateValue update, T defaultValue)
	{
		return (T) ConvertOrDefault(typeof(T), update, defaultValue);
	}

	private static object ConvertOrDefault(Type type, PartialUpdateValue update, object defaultValue)
	{
		return update.Value.TryConvertTo(type, out var result) ? result : defaultValue;
	}

	private void Set(string name, object value, PropertyInfo property)
	{
		//if (!property.CanWrite)
		//{
		//	throw new CornerstoneException("The property cannot be set because it's not writable.");
		//}

		if (property.PropertyType.IsNullable() && (value == null))
		{
			_updates.AddOrUpdate(name, new PartialUpdateValue
			{
				Name = name,
				Type = property.PropertyType,
				Value = null
			});
			return;
		}

		var valueType = value.GetType();
		if (!property.PropertyType.IsAssignableFrom(valueType))
		{
			throw new CornerstoneException("The property type does not match the values type.");
		}

		AddOrUpdate(name, property.PropertyType, value);
	}

	#endregion
}