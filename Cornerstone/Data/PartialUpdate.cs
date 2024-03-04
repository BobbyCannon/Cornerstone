#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
	/// Get the property value.
	/// </summary>
	/// <typeparam name="TProperty"> The type to cast the value to. </typeparam>
	/// <param name="expression"> The expression of the member to set. </param>
	/// <param name="defaultValue"> A default value if update not available. </param>
	/// <returns> The value if it was found otherwise default(T). </returns>
	public TProperty Get<TProperty>(Expression<Func<T, TProperty>> expression, TProperty defaultValue)
	{
		var propertyExpression = (MemberExpression) expression.Body;
		return Get(propertyExpression.Member.Name, defaultValue);
	}

	/// <summary>
	/// Set a property for the update.
	/// </summary>
	/// <param name="expression"> The expression of the member to set. </param>
	/// <param name="value"> The value of the member. </param>
	public void Set<TProperty>(Expression<Func<T, TProperty>> expression, TProperty value)
	{
		var propertyExpression = (MemberExpression) expression.Body;
		Set(propertyExpression.Member.Name, value);
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
		return TryGet<T>(name, out var value) ? value : Activator.CreateInstance<T>();
	}

	/// <summary>
	/// Get the update for the provided name with a fallback default value if not found.
	/// </summary>
	/// <typeparam name="T"> The type to cast the value to. </typeparam>
	/// <param name="name"> The name of the update. </param>
	/// <param name="defaultValue"> A default value if update not available. </param>
	/// <returns> The value if it was found otherwise default(T). </returns>
	public T Get<T>(string name, T defaultValue)
	{
		if (!_updates.ContainsKey(name))
		{
			return defaultValue;
		}

		var update = _updates[name];
		return ConvertOrDefault(update, defaultValue);
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
	/// <param name="name"> The name of the member to set. </param>
	/// <param name="value"> The value of the member. </param>
	public void Set<T>(string name, T value)
	{
		var property = GetTargetProperty(name);
		Set(name, value, property);
	}

	/// <summary>
	/// Try to get the update for the provided name.
	/// </summary>
	/// <typeparam name="T"> The type to cast the value to. </typeparam>
	/// <param name="name"> The name of the update. </param>
	/// <param name="value"> The value read if successful. </param>
	/// <returns> True if the value was read otherwise false. </returns>
	public bool TryGet<T>(string name, out T value)
	{
		if (_updates.TryGetValue(name, out var update))
		{
			return update.Value.TryConvertTo(out value);
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Get the property value.
	/// </summary>
	/// <param name="name"> The name of the update. </param>
	/// <param name="value"> The value that was retrieve or default value if not found. </param>
	/// <returns> True if the update was found otherwise false. </returns>
	public bool TryGet(string name, out object value)
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
	/// Create a dynamic object of the partial update.
	/// </summary>
	/// <returns> The dynamic version of the partial update. </returns>
	protected internal virtual IDictionary<string, object> GetDictionary()
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
	/// Gets a list of property information for this type.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <returns> The list of properties for this type. </returns>
	protected internal virtual IDictionary<string, PropertyInfo> GetTargetProperties()
	{
		return GetType().GetCachedPropertyDictionary();
	}

	/// <summary>
	/// Gets a property information for this type.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <returns> The property for the target type or null if not found. </returns>
	protected internal virtual PropertyInfo GetTargetProperty(string name)
	{
		if (GetTargetProperties().TryGetValue(name, out var value))
		{
			return value;
		}

		throw new NotSupportedException();
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
		if (!property.CanWrite)
		{
			throw new CornerstoneException("The property cannot be set because it's not writable.");
		}

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