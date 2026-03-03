#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Serialization;

public class Serializer
{
	#region Fields

	private static readonly Type _enumerableType;

	#endregion

	#region Constructors

	static Serializer()
	{
		_enumerableType = typeof(IEnumerable<>);
	}

	#endregion

	#region Methods

	internal static Type GetArrayType(Type type)
	{
		if (type.IsArray)
		{
			return type.GetElementType();
		}

		if (type.IsGenericType)
		{
			return type.GetGenericArguments()[0];
		}

		var enumerableType = type
			.GetInterfaces().FirstOrDefault(i => i.IsGenericType
				&& (i.GetGenericTypeDefinition() == _enumerableType)
		);

		return enumerableType?.GetGenericArguments()[0];
	}

	#endregion
}