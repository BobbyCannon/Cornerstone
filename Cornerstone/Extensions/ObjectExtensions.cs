#region References

using System;
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
	/// <returns> The clone of the item. </returns>
	public static T DeepCloneUsingSerializer<T>(this T item, int? maxDepth = null, IncludeExcludeOptions options = null)
	{
		if (maxDepth.HasValue)
		{
			var settings = new SerializationOptions
			{
				MaxDepth = maxDepth.Value
			};
			var json = item.ToJson(settings);
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
			var settings = new SerializationOptions { MaxDepth = maxDepth.Value };
			var deepResponse = FromJson(item.ToJson(settings), item.GetRealTypeUsingReflection());
			(deepResponse as ITrackPropertyChanges)?.ResetHasChanges();
			return deepResponse;
		}

		var response = FromJson(item.ToJson(), item.GetRealTypeUsingReflection());
		(response as ITrackPropertyChanges)?.ResetHasChanges();
		return response;
	}

	public static T DeepCloneUsingUpdateWith<T>(this T value, int? maxDepth = null, IncludeExcludeOptions options = null)
	{
		return (T) DeepCloneUsingUpdateWith(value, typeof(T), maxDepth, options);
	}

	public static object DeepCloneUsingUpdateWith(this object value, int? maxDepth = null, IncludeExcludeOptions options = null)
	{
		if (value == null)
		{
			return null;
		}

		var type = value.GetType();
		return DeepCloneUsingUpdateWith(value, type, maxDepth, options);
	}

	public static object DeepCloneUsingUpdateWith(this object value, Type type, int? maxDepth = null, IncludeExcludeOptions options = null)
	{
		var response = type.CreateInstance();

		switch (response)
		{
			case IUpdateable updateable:
			{
				var allOptions = Cache.GetOptions(type, UpdateableAction.Updateable).WithMoreOptions(options);
				updateable.UpdateWith(value, allOptions);
				break;
			}
			default:
			{
				response.UpdateWithUsingReflection(value, options);
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
	public static T FromJson<T>(this string value, ISerializationOptions settings = null)
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
	public static object FromJson(this string value, Type type, ISerializationOptions settings = null)
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
	/// <param name="options"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned value. </returns>
	public static T ShallowClone<T>(this T value, IncludeExcludeOptions options = null)
	{
		var response = value switch
		{
			ICloneable cloneable => (T) cloneable.ShallowCloneObject(options),
			_ => DeepCloneUsingSerializer(value, 0)
		};

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
	/// <param name="options"> The option for converting. </param>
	/// <returns> The object in CSharp code format. </returns>
	public static string ToCSharp<T>(this T value, ICodeWriterOptions options = null)
	{
		return CSharpCodeWriter.GenerateCode(value, options);
	}

	/// <summary>
	/// Serialize an object to a string.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="value"> The value to serialize. </param>
	/// <param name="settings"> The settings for serializing. </param>
	/// <returns> The object in string format. </returns>
	public static string ToJson<T>(this T value, ISerializationOptions settings = null)
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
	public static string ToJson(this object value, Type valueType, ISerializationOptions settings = null)
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
	public static string ToRawJson<T>(this T value, ISerializationOptions settings = null)
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

	#endregion
}