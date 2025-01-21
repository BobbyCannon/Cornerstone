#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid;

public static class PropertyGridExtensions
{
	#region Methods

	/// <summary>
	/// Gets the category.
	/// </summary>
	/// <param name="property"> The property. </param>
	/// <param name="optionalDefault"> An optional default for [Misc] category. </param>
	/// <returns> System.String. </returns>
	public static string GetCategory(this PropertyDescriptor property, string optionalDefault = null)
	{
		var category = string.IsNullOrEmpty(property.Category) || (property.Category == "Misc")
			? optionalDefault ?? property.Category
			: property.Category;

		return category;
	}

	/// <summary>
	/// Gets the custom attribute.
	/// </summary>
	/// <typeparam name="T"> The type of attribute. </typeparam>
	/// <param name="propertyDescriptor"> The property descriptor. </param>
	/// <returns> The custom attribute if found otherwise null. </returns>
	public static T GetCustomAttribute<T>(this PropertyDescriptor propertyDescriptor) where T : Attribute
	{
		foreach (var attr in propertyDescriptor.Attributes)
		{
			if (attr is T t)
			{
				return t;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the custom attributes.
	/// </summary>
	/// <typeparam name="T"> The type of attribute. </typeparam>
	/// <param name="propertyDescriptor"> The property descriptor. </param>
	/// <returns> The array of attributes. </returns>
	public static T[] GetCustomAttributes<T>(this PropertyDescriptor propertyDescriptor) where T : Attribute
	{
		var list = new List<T>();
		foreach (var attr in propertyDescriptor.Attributes)
		{
			if (attr is T t)
			{
				list.Add(t);
			}
		}

		return list.ToArray();
	}

	/// <summary>
	/// Gets the display message.
	/// </summary>
	/// <param name="validationResult"> The validation result. </param>
	/// <returns> The display message. </returns>
	public static string GetDisplayMessage(this ValidationResult validationResult)
	{
		if (validationResult == null)
		{
			return "Success";
		}

		if (validationResult.MemberNames.Any())
		{
			return $"{validationResult.ErrorMessage}:{string.Join(",", validationResult.MemberNames)}";
		}

		return validationResult.ErrorMessage ?? "";
	}

	/// <summary>
	/// if it has defined an Attribute
	/// </summary>
	/// <typeparam name="T"> </typeparam>
	/// <param name="memberInfo"> The member information. </param>
	/// <param name="inherit"> if set to true [inherit]. </param>
	/// <returns> True if the specified inherit is defined otherwise false. </returns>
	public static bool IsDefined<T>(this MemberInfo memberInfo, bool inherit = true)
		where T : Attribute
	{
		return memberInfo.IsDefined(typeof(T), inherit);
	}

	/// <summary>
	/// Determines whether the specified property descriptor is defined an attribute.
	/// </summary>
	/// <typeparam name="T"> </typeparam>
	/// <param name="propertyDescriptor"> The property descriptor. </param>
	/// <returns> True if the specified property descriptor is defined an attribute otherwise false. </returns>
	public static bool IsDefined<T>(this PropertyDescriptor propertyDescriptor) where T : Attribute
	{
		return propertyDescriptor.Attributes.OfType<T>().Any();
	}

	#endregion
}