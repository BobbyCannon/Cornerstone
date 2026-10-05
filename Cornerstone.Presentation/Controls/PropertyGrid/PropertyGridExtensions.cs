#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid;

public static class PropertyGridExtensions
{
	#region Methods

	public static string GetCategory(this PropertyDescriptor property, string optionalDefault = null)
	{
		var category = string.IsNullOrEmpty(property.Category) || (property.Category == "Misc")
			? optionalDefault ?? property.Category
			: property.Category;

		return category;
	}

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

	public static bool IsDefined<T>(this MemberInfo memberInfo, bool inherit = true)
		where T : Attribute
	{
		return memberInfo.IsDefined(typeof(T), inherit);
	}

	public static bool IsDefined<T>(this PropertyDescriptor propertyDescriptor) where T : Attribute
	{
		return propertyDescriptor.Attributes.OfType<T>().Any();
	}

	#endregion
}