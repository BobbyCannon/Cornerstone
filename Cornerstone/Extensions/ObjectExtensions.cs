﻿#region References

using System;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Internal;
using Cornerstone.Serialization;
using Cornerstone.Storage;
using ICloneable = Cornerstone.Data.ICloneable;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for <see cref="object" />.
/// </summary>
public static class ObjectExtensions
{
	#region Methods

	/// <summary>
	/// Deep clone the item.
	/// </summary>
	/// <typeparam name="T"> The type to clone. </typeparam>
	/// <param name="item"> The item to clone. </param>
	/// <param name="maxDepth"> The max depth to clone. Defaults to null. </param>
	/// <param name="settings"> The include / exclude settings. </param>
	/// <returns> The clone of the item. </returns>
	public static T DeepCloneUsingSerializer<T>(this T item, int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		if (maxDepth.HasValue)
		{
			var serializationSettings = new SerializationSettings
			{
				MaxDepth = maxDepth.Value
			};
			var json = item.ToJson(serializationSettings);
			var realType = item.GetRealTypeUsingReflection();
			var response = FromJson(json, realType);
			(response as ITrackPropertyChanges)?.ResetHasChanges();
			return (T) response;
		}
		else
		{
			var json = item.ToJson();
			var realType = item.GetRealTypeUsingReflection();
			var response = FromJson(json, realType);
			(response as ITrackPropertyChanges)?.ResetHasChanges();
			return (T) response;
		}
	}

	/// <summary>
	/// Deep clone the item.
	/// </summary>
	/// <param name="item"> The item to clone. </param>
	/// <param name="maxDepth"> The max depth to clone. Defaults to null. </param>
	/// <returns> The clone of the item. </returns>
	public static object DeepCloneUsingSerializer(this object item, int? maxDepth = null)
	{
		if (maxDepth.HasValue)
		{
			var settings = new SerializationSettings { MaxDepth = maxDepth.Value };
			var deepResponse = FromJson(item.ToJson(settings), item.GetRealTypeUsingReflection());
			(deepResponse as ITrackPropertyChanges)?.ResetHasChanges();
			return deepResponse;
		}

		var response = FromJson(item.ToJson(), item.GetRealTypeUsingReflection());
		(response as ITrackPropertyChanges)?.ResetHasChanges();
		return response;
	}

	public static T DeepCloneUsingUpdateWith<T>(this T value, int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return (T) DeepCloneUsingUpdateWith(value, typeof(T), maxDepth, settings);
	}

	public static object DeepCloneUsingUpdateWith(this object value, int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		if (value == null)
		{
			return null;
		}

		var type = value.GetType();
		return DeepCloneUsingUpdateWith(value, type, maxDepth, settings);
	}

	public static object DeepCloneUsingUpdateWith(this object value, Type type, int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		var response = type.CreateInstance();

		switch (response)
		{
			case IUpdateable updateable:
			{
				var allOptions = Cache.GetSettings(type, UpdateableAction.Updateable).WithMoreSettings(settings);
				updateable.UpdateWith(value, allOptions);
				break;
			}
			default:
			{
				response.UpdateWithUsingReflection(value, settings);
				break;
			}
		}

		if (response is ITrackPropertyChanges changeable)
		{
			changeable.ResetHasChanges();
		}

		return response;
	}

	/// <summary>
	/// Convert the string into an object.
	/// </summary>
	/// <typeparam name="T"> The type to convert into. </typeparam>
	/// <param name="value"> The JSON data to deserialize. </param>
	/// <param name="settings"> The settings for deserializing. </param>
	/// <returns> The deserialized object. </returns>
	public static T FromJson<T>(this string value, ISerializationSettings settings = null)
	{
		return Serializer.Instance.FromJson<T>(value, settings);
	}

	/// <summary>
	/// Convert the string into an object.
	/// </summary>
	/// <param name="value"> The JSON data to deserialize. </param>
	/// <param name="type"> The type to convert into. </param>
	/// <param name="settings"> The settings for deserializing. </param>
	/// <returns> The deserialized object. </returns>
	public static object FromJson(this string value, Type type, ISerializationSettings settings = null)
	{
		return Serializer.Instance.FromJson(value, type, settings);
	}

	/// <summary>
	/// Executes a provided action if the test is successful.
	/// </summary>
	/// <param name="test"> The test to determine action to take. </param>
	/// <param name="action1"> The action to perform if the test is true. </param>
	/// <param name="action2"> The action to perform if the test is false. </param>
	public static void IfThenElse(Func<bool> test, Action action1, Action action2)
	{
		if (test())
		{
			action1();
		}
		else
		{
			action2();
		}
	}

	/// <summary>
	/// Check to see if the value is default value.
	/// </summary>
	/// <param name="value"> The value to test. </param>
	/// <returns> True if the value is default value otherwise false. </returns>
	public static bool IsDefaultValue(this object value)
	{
		if (value == null)
		{
			return true;
		}

		var t = value.GetType();
		if (!t.IsValueType)
		{
			return false;
		}

		var i = t.CreateInstance();
		return Equals(i, value);
	}

	/// <summary>
	/// Global shallow clone. If the object is ICloneable then the interface implementation will be used.
	/// </summary>
	/// <typeparam name="T"> The type of the object </typeparam>
	/// <param name="value"> The value to clone. </param>
	/// <param name="settings"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned value. </returns>
	public static T ShallowClone<T>(this T value, IncludeExcludeSettings settings = null)
	{
		T response;

		switch (value)
		{
			case ICloneable cloneable:
			{
				response = (T) cloneable.ShallowCloneObject();
				break;
			}
			case IUpdateable:
			{
				response = Activator.CreateInstance<T>();
				response.UpdateWith(value, settings);
				break;
			}
			default:
			{
				response = Activator.CreateInstance<T>();
				response.UpdateWithUsingReflection(value, settings);
				break;
			}
		}

		if (response is ITrackPropertyChanges trackable)
		{
			trackable.ResetHasChanges();
		}

		return response;
	}

	/// <summary>
	/// Global shallow clone from one type to another.
	/// If the object is ICloneable then the interface implementation will be used.
	/// </summary>
	/// <typeparam name="T"> The type to convert from. </typeparam>
	/// <typeparam name="T2"> The type to convert to. </typeparam>
	/// <param name="value"> The value to clone. </param>
	/// <param name="settings"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned value. </returns>
	public static T2 ShallowClone<T, T2>(this T value, IncludeExcludeSettings settings = null)
	{
		T2 response;

		switch (value)
		{
			case IUpdateable:
			{
				response = Activator.CreateInstance<T2>();
				response.UpdateWith(value, settings);
				break;
			}
			default:
			{
				response = Activator.CreateInstance<T2>();
				response.UpdateWithUsingReflection(value, settings);
				break;
			}
		}

		if (response is ITrackPropertyChanges trackable)
		{
			trackable.ResetHasChanges();
		}

		return response;
	}

	/// <summary>
	/// Convert an option to CSharp code.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="value"> The value to convert. </param>
	/// <param name="settings"> The option for converting. </param>
	/// <returns> The object in CSharp code format. </returns>
	public static string ToCSharp<T>(this T value, ICodeWriterSettings settings = null)
	{
		return CSharpCodeWriter.GenerateCode(value, settings);
	}

	/// <summary>
	/// Serialize an object to a string.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="value"> The value to serialize. </param>
	/// <param name="settings"> The settings for serializing. </param>
	/// <returns> The object in string format. </returns>
	public static string ToJson<T>(this T value, ISerializationSettings settings = null)
	{
		return Serializer.Instance.ToJson(value, settings);
	}

	/// <summary>
	/// Serialize an object to a string.
	/// </summary>
	/// <param name="value"> The value to serialize. </param>
	/// <param name="valueType"> The type of the value. </param>
	/// <param name="settings"> The settings for serializing. </param>
	/// <returns> The object in string format. </returns>
	public static string ToJson(this object value, Type valueType, ISerializationSettings settings = null)
	{
		return Serializer.Instance.ToJson(value, valueType, settings);
	}

	/// <summary>
	/// Serialize an object to a string.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="value"> The value to serialize. </param>
	/// <param name="settings"> The settings for serializing. </param>
	/// <returns> The object in string format. </returns>
	public static string ToRawJson<T>(this T value, ISerializationSettings settings = null)
	{
		// todo: disable reference values
		return Serializer.Instance.ToJson(value, settings);
	}

	/// <summary>
	/// Unwrap the entity from the proxy.
	/// </summary>
	/// <returns>
	/// The real entity unwrapped from the Entity Framework proxy.
	/// </returns>
	public static T Unwrap<T>(this T value)
	{
		var type = value is IEntity entity
			? entity.GetRealType()
			: value.GetRealTypeUsingReflection();

		var response = (T) type.CreateInstance();
		var notifiable = response as INotifiable;

		notifiable?.DisablePropertyChangeNotifications();

		if (response is IUpdateable updateable)
		{
			updateable.UpdateWith(value, UpdateableAction.UnwrapProxyEntity);
		}
		else
		{
			response.UpdateWithUsingReflection(value);
		}

		notifiable?.EnablePropertyChangeNotifications();
		notifiable?.ResetHasChanges();

		return response;
	}

	/// <summary>
	/// Update the provided object with non default values.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="model"> The value to update all properties for. </param>
	/// <param name="settings"> The include exclude settings. </param>
	/// <param name="customTypeFactory"> An optional custom factory method to create unknown types. </param>
	/// <returns> The type updated with non default values. </returns>
	public static T UpdateWithNonDefaultValues<T>(this T model, IncludeExcludeSettings settings, Func<Type, object[], object> customTypeFactory = null)
	{
		var notifiable = model as INotifiable;
		notifiable?.DisablePropertyChangeNotifications();

		try
		{
			var properties = Cache
				.GetSettableProperties(model)
				.Where(x => settings.ShouldProcessProperty(x.Name))
				.OrderBy(x => x.Name)
				.ToList();

			foreach (var property in properties)
			{
				var currentValue = property.GetValue(model);
				if (currentValue != null)
				{
					if (property.PropertyType.IsEnumerable())
					{
						continue;
					}

					if (!currentValue.IsDefaultValue())
					{
						continue;
					}
				}

				var nonDefaultValue = property.CreateInstanceOfNonDefaultValue(customTypeFactory: customTypeFactory);
				if (nonDefaultValue == null)
				{
					continue;
				}
				property.SetValue(model, nonDefaultValue);
			}

			return model;
		}
		finally
		{
			notifiable?.EnablePropertyChangeNotifications();
		}
	}

	#endregion
}