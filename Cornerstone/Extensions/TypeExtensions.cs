#region References

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Cornerstone.Reflection;

#endregion

#pragma warning disable IL2070

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for the Type object.
/// </summary>
public static class TypeExtensions
{
	#region Fields

	private static readonly ConcurrentDictionary<Type, string> _typeAssemblyNames;

	#endregion

	#region Constructors

	static TypeExtensions()
	{
		_typeAssemblyNames = new ConcurrentDictionary<Type, string>();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Returns true if the object is a descendant of the provided generic type
	/// </summary>
	/// <typeparam name="T"> The parent type. </typeparam>
	/// <param name="value"> The value to check. </param>
	/// <returns> </returns>
	/// <exception cref="ArgumentNullException"> The value was null and could not be processed. </exception>
	public static bool ImplementsType<T>(this Type value)
	{
		return (value != null) && value.ImplementsType(typeof(T));
	}

	/// <summary>
	/// Returns true if the object implements the provided type.
	/// </summary>
	/// <param name="valueType"> The value type to check. </param>
	/// <param name="parentType"> The parent type. </param>
	/// <returns> </returns>
	/// <exception cref="ArgumentNullException"> The valueType was null and could not be processed. </exception>
	public static bool ImplementsType(this Type valueType, Type parentType)
	{
		if ((valueType == null) || (parentType == null))
		{
			return false;
		}

		if (parentType.IsGenericType)
		{
			return IsSubclassOfRawGeneric(valueType, parentType);
		}

		if (parentType.IsInterface)
		{
			return parentType.IsAssignableFrom(valueType);
		}

		return valueType.IsSubclassOf(parentType);
	}

	/// <summary>
	/// Converts the type to an assembly name. Does not include version. Ex. System.String,mscorlib
	/// </summary>
	/// <param name="type"> The type to get the assembly name for. </param>
	/// <returns> The assembly name for the provided type. </returns>
	public static string ToAssemblyName(this Type type)
	{
		if (type == null)
		{
			return string.Empty;
		}

		return _typeAssemblyNames.GetOrAdd(type,
			_ =>
			{
				if (!type.IsGenericType)
				{
					return $"{type.FullName},{type.Assembly.GetName().Name}";
				}

				var sb = new StringBuilder();
				appendMinimal(type, sb);
				return sb.ToString();
			});

		void appendMinimal(Type t, StringBuilder sb)
		{
			if (t.IsGenericParameter)
			{
				sb.Append(t.Name);
				return;
			}

			sb.Append(t.Namespace).Append('.').Append(t.Name.Split('`')[0]);

			if (t.IsGenericType)
			{
				sb.Append('`').Append(t.GetGenericArguments().Length);
				sb.Append("[[");
				var first = true;
				foreach (var arg in t.GetGenericArguments())
				{
					if (!first)
					{
						sb.Append("],[");
					}
					appendMinimal(arg, sb);
					first = false;
				}
				sb.Append("]]");
			}

			sb.Append($",{t.Assembly.GetName().Name}");
		}
	}

	/// <summary>
	/// Try to convert the assembly name string into a type. Does not include version. Ex. System.String,mscorlib
	/// </summary>
	/// <param name="value"> The type assembly name string. </param>
	/// <param name="type"> The type to get the assembly name for. </param>
	/// <returns> True if the value was a valid type otherwise false. </returns>
	public static bool TryGetType(this string value, out Type type)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			type = null;
			return false;
		}

		try
		{
			var sourceType = SourceReflector.GetSourceType(value)
				?? SourceReflector.GetSourceType(RemoveAssemblyVersionMetadata(value));
			type = sourceType?.Type;
			return type != null;
		}
		catch
		{
			type = null;
			return false;
		}
	}

	internal static string RemoveAssemblyVersionMetadata(string assemblyQualifiedName)
	{
		if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
		{
			return assemblyQualifiedName ?? "";
		}

		var cleaned = assemblyQualifiedName;

		cleaned = Regex.Replace(cleaned, @", Version\s*=[^,\]]+", "", RegexOptions.IgnoreCase);
		cleaned = Regex.Replace(cleaned, @", Culture\s*=[^,\]]+", "", RegexOptions.IgnoreCase);
		cleaned = Regex.Replace(cleaned, @", PublicKeyToken\s*=[^,\]]+", "", RegexOptions.IgnoreCase);
		cleaned = cleaned.Trim(',', ' ').Trim();

		return cleaned;
	}

	private static bool IsSubclassOfRawGeneric(Type valueType, Type parentGeneric)
	{
		while ((valueType != null) && (valueType != typeof(object)))
		{
			if (valueType == parentGeneric)
			{
				return true;
			}

			//
			// Convert the value to type definition if the parent is a definition
			//
			var current = valueType.IsGenericType
				&& parentGeneric.IsGenericTypeDefinition
					? valueType.GetGenericTypeDefinition()
					: valueType;

			if (parentGeneric == current)
			{
				return true;
			}

			if (parentGeneric.IsInterface)
			{
				var interfaceTypes = current.GetInterfaces();

				foreach (var interfaceType in interfaceTypes)
				{
					if (parentGeneric.IsGenericTypeDefinition
						&& interfaceType.IsGenericType
						&& !interfaceType.IsGenericTypeDefinition)
					{
						//
						// Convert the value to type definition if the parent is a definition
						//
						var interfaceTypeDefinition = interfaceType.GetGenericTypeDefinition();
						if (interfaceTypeDefinition == parentGeneric)
						{
							return true;
						}
					}

					if (interfaceType == parentGeneric)
					{
						return true;
					}
				}
			}

			valueType = valueType.BaseType;
		}

		return false;
	}

	#endregion
}