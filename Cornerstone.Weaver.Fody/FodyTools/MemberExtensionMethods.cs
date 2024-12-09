// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

internal static class MemberExtensionMethods
{
	#region Methods

	public static SequencePoint GetEntryPoint(this MethodReference method, ISymbolReader symbolReader)
	{
		return method?.Resolve()?.ReadSequencePoints(symbolReader)?.FirstOrDefault();
	}

	public static GenericInstanceMethod MakeGenericInstanceMethod(this MethodReference method, params TypeReference[] arguments)
	{
		var newMethod = new GenericInstanceMethod(method);

		if (method.GenericParameters.Count != arguments.Length)
		{
			throw new InvalidOperationException("Generic argument mismatch");
		}

		newMethod.GenericParameters.AddRange(method.Resolve().GenericParameters.Select(p => new GenericParameter(p.Name, p.Owner)));
		newMethod.GenericArguments.AddRange(arguments);

		return newMethod;
	}

	/// <summary>
	/// Returns a method reference bound to the generic parameters of the owning class.
	/// </summary>
	/// <param name="method"> The method to bind. </param>
	/// <param name="genericType"> The generic type. </param>
	/// <returns> The bound method. </returns>
	/// <exception cref="InvalidOperationException">
	/// Need a generic type as the target.
	/// or
	/// Generic type must resolve to the same type as the methods current type.
	/// or
	/// method is already a generic instance
	/// </exception>
	public static MethodReference OnGenericType(this MethodReference method, TypeReference genericType)
	{
		if (!genericType.IsGenericInstance)
		{
			throw new InvalidOperationException("Need a generic type as the target.");
		}
		if (method.DeclaringType.GetElementType().FullName != genericType.GetElementType().FullName)
		{
			throw new InvalidOperationException("Generic type must resolve to the same type as the methods current type.");
		}
		if (method.IsGenericInstance)
		{
			throw new InvalidOperationException("method is already a generic instance");
		}

		var newMethod = new MethodReference(method.Name, method.ReturnType, genericType)
		{
			CallingConvention = method.CallingConvention,
			ExplicitThis = method.ExplicitThis,
			HasThis = method.HasThis
		};

		newMethod.Parameters.AddRange(method.Parameters);
		newMethod.GenericParameters.AddRange(method.Resolve().GenericParameters.Select(p => new GenericParameter(p.Name, p.Owner)));
		return newMethod;
	}

	/// <summary>
	/// Returns a method reference bound to the generic parameters of the owning class.
	/// If the owning class is not a generic class, the method is returned unchanged.
	/// </summary>
	/// <param name="method"> The method. </param>
	/// <param name="genericType"> Type of the generic. </param>
	/// <returns> The bound or the original method. </returns>
	public static MethodReference OnGenericTypeOrSelf(this MethodReference method, TypeReference genericType)
	{
		if (!genericType.IsGenericInstance || method.IsGenericInstance)
		{
			return method;
		}

		return method.OnGenericType(genericType);
	}

	// ReSharper disable once ReturnTypeCanBeEnumerable.Global
	public static IList<SequencePoint> ReadSequencePoints(this MethodDefinition method, ISymbolReader symbolReader)
	{
		return method?.DebugInformation?.HasSequencePoints == true
			? symbolReader?.Read(method)?.SequencePoints
			: null;
	}

	#endregion
}