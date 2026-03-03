#region References

using System;
using System.Linq;
using System.Runtime.CompilerServices;

#endregion

namespace Avalonia.Diagnostics;

public static class TypeExtensions
{
	#region Fields

	private static readonly ConditionalWeakTable<Type, string> _getTypeNameCache = new();

	#endregion

	#region Methods

	public static string GetTypeName(this Type type)
	{
		if (!_getTypeNameCache.TryGetValue(type, out var name))
		{
			name = type.Name;
			if (Nullable.GetUnderlyingType(type) is Type nullable)
			{
				name = nullable.Name + "?";
			}
			else if (type.IsGenericType)
			{
				var definition = type.GetGenericTypeDefinition();
				var arguments = type.GetGenericArguments();
				name = definition.Name.Substring(0, definition.Name.IndexOf('`'));
				name = $"{name}<{string.Join(",", arguments.Select(GetTypeName))}>";
			}
			_getTypeNameCache.Add(type, name);
		}
		return name;
	}

	#endregion
}