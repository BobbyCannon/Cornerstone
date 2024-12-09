// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fody;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

internal static class TypeSystemExtensionMethods
{
	#region Methods

	public static Type GetDeclaringType(this MemberInfo memberInfo)
	{
		return memberInfo.DeclaringType ?? throw new InvalidOperationException("Invalid expression, MemberInfo does not have a declaring type.");
	}

	public static string GetFullName(this Type type)
	{
		// type.FullName may contain extra generic info!
		return type.Namespace + "." + type.Name;
	}

	public static void GetMemberInfo(this LambdaExpression expression, out Type declaringType, out string memberName)
	{
		switch (expression.Body)
		{
			case MemberExpression memberExpression:
				memberName = memberExpression.Member.Name;
				declaringType = memberExpression.Member.GetDeclaringType();
				break;

			default:
				throw new ArgumentException("Only member expression is supported.", nameof(expression));
		}
	}

	public static void GetMethodInfo(this LambdaExpression expression, out Type declaringType, out string methodName, out IReadOnlyList<Type> argumentTypes)
	{
		switch (expression.Body)
		{
			case NewExpression newExpression:
				methodName = ".ctor";
				declaringType = newExpression.Type;
				argumentTypes = newExpression.Arguments.Select(a => a.Type).ToReadOnlyList();
				break;

			case MethodCallExpression methodCall:
				methodName = methodCall.Method.Name;
				declaringType = methodCall.Method.GetDeclaringType();
				argumentTypes = methodCall.Arguments.Select(a => a.Type).ToReadOnlyList();
				break;

			default:
				throw new ArgumentException("Only method call or new expression is supported.", nameof(expression));
		}
	}

	public static MethodReference ImportEventAdd(this ITypeSystem typeSystem, Type declaringType, string name)
	{
		return typeSystem.TryImportEventAdd(declaringType, name) ?? throw new WeavingException($"Can't find add method for event {name} on type {declaringType}");
	}

	public static MethodReference ImportEventAdd<T>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.ImportEventAdd(typeof(T), name);
	}

	public static MethodReference ImportEventAdd<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMemberInfo(expression, out var declaringType, out var name);

		return typeSystem.ImportEventAdd(declaringType, name);
	}

	public static MethodReference ImportEventRemove(this ITypeSystem typeSystem, Type declaringType, string name)
	{
		return typeSystem.TryImportEventRemove(declaringType, name) ?? throw new WeavingException($"Can't find remove for event {name} on type {declaringType}");
	}

	public static MethodReference ImportEventRemove<T>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.ImportEventRemove(typeof(T), name);
	}

	public static MethodReference ImportEventRemove<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMemberInfo(expression, out var declaringType, out var name);

		return typeSystem.ImportEventRemove(declaringType, name);
	}

	public static MethodReference ImportMethod<T>(this ITypeSystem typeSystem, string name, params Type[] argumentTypes)
	{
		return typeSystem.ImportMethod(typeof(T), name, argumentTypes);
	}

	public static MethodReference ImportMethod<T, TP1>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.ImportMethod<T>(name, typeof(TP1));
	}

	public static MethodReference ImportMethod<T, TP1, TP2>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.ImportMethod<T>(name, typeof(TP1), typeof(TP2));
	}

	public static MethodReference ImportMethod<T, TP1, TP2, TP3>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.ImportMethod<T>(name, typeof(TP1), typeof(TP2), typeof(TP3));
	}

	public static MethodReference ImportMethod<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMethodInfo(expression, out var declaringType, out var methodName, out var argumentTypes);

		return typeSystem.ImportMethod(declaringType, methodName, argumentTypes);
	}

	public static MethodReference ImportMethod(this ITypeSystem typeSystem, Expression<Action> expression)
	{
		GetMethodInfo(expression, out var declaringType, out var methodName, out var argumentTypes);

		return typeSystem.ImportMethod(declaringType, methodName, argumentTypes);
	}

	public static MethodReference ImportPropertyGet(this ITypeSystem typeSystem, Type declaringType, string name)
	{
		return typeSystem.TryImportPropertyGet(declaringType, name) ?? throw new WeavingException($"Can't find getter for property {name} on type {declaringType}");
	}

	public static MethodReference ImportPropertyGet<T>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.ImportPropertyGet(typeof(T), name);
	}

	public static MethodReference ImportPropertyGet<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMemberInfo(expression, out var declaringType, out var name);

		return typeSystem.ImportPropertyGet(declaringType, name);
	}

	public static MethodReference ImportPropertySet(this ITypeSystem typeSystem, Type declaringType, string name)
	{
		return typeSystem.TryImportPropertySet(declaringType, name) ?? throw new WeavingException($"Can't find setter for property {name} on type {declaringType}");
	}

	public static MethodReference ImportPropertySet<T>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.ImportPropertySet(typeof(T), name);
	}

	public static MethodReference ImportPropertySet<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMemberInfo(expression, out var declaringType, out var name);

		return typeSystem.ImportPropertySet(declaringType, name);
	}

	public static TypeReference ImportType(this ITypeSystem typeSystem, Type type)
	{
		return typeSystem.ModuleDefinition.ImportReference(typeSystem.FindType(GetFullName(type)));
	}

	public static TypeReference ImportType<T>(this ITypeSystem typeSystem)
	{
		return typeSystem.ImportType(typeof(T));
	}

	public static bool ParametersMatch(this IList<ParameterDefinition> parameters, IReadOnlyList<Type> argumentTypes)
	{
		if (parameters.Count != argumentTypes.Count)
		{
			return false;
		}

		var genericParameterMap = new Dictionary<string, string>();

		for (var i = 0; i < parameters.Count; i++)
		{
			var parameterType = parameters[i].ParameterType;
			var argumentType = argumentTypes[i].GetFullName();

			if (parameterType.ContainsGenericParameter)
			{
				// for generic parameters just verify that every generic type matches to the same placeholder type.
				var elementTypeName = parameterType.GetElementType().FullName;

				if (genericParameterMap.TryGetValue(elementTypeName, out var mappedType))
				{
					if (mappedType != argumentType)
					{
						return false;
					}
				}
				else
				{
					genericParameterMap.Add(elementTypeName, argumentType);
				}
			}
			else if (parameterType.GetElementType().FullName != argumentType)
			{
				return false;
			}
		}

		return true;
	}

	public static MethodReference TryImportEventAdd(this ITypeSystem typeSystem, Type declaringType, string name)
	{
		return TryImport(typeSystem, declaringType, name, t => t.Events, e => e.AddMethod);
	}

	public static MethodReference TryImportEventAdd<T>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.TryImportEventAdd(typeof(T), name);
	}

	public static MethodReference TryImportEventAdd<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMemberInfo(expression, out var declaringType, out var name);

		return typeSystem.TryImportEventAdd(declaringType, name);
	}

	public static MethodReference TryImportEventRemove(this ITypeSystem typeSystem, Type declaringType, string name)
	{
		return TryImport(typeSystem, declaringType, name, t => t.Events, e => e.RemoveMethod);
	}

	public static MethodReference TryImportEventRemove<T>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.TryImportEventRemove(typeof(T), name);
	}

	public static MethodReference TryImportEventRemove<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMemberInfo(expression, out var declaringType, out var name);

		return typeSystem.TryImportEventRemove(declaringType, name);
	}

	public static MethodReference TryImportMethod<T>(this ITypeSystem typeSystem, string name, params Type[] argumentTypes)
	{
		return typeSystem.TryImportMethod(typeof(T), name, argumentTypes);
	}

	public static MethodReference TryImportMethod<T, TP1>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.TryImportMethod<T>(name, typeof(TP1));
	}

	public static MethodReference TryImportMethod<T, TP1, TP2>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.TryImportMethod<T>(name, typeof(TP1), typeof(TP2));
	}

	public static MethodReference TryImportMethod<T, TP1, TP2, TP3>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.TryImportMethod<T>(name, typeof(TP1), typeof(TP2), typeof(TP3));
	}

	public static MethodReference TryImportMethod<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMethodInfo(expression, out var declaringType, out var methodName, out var argumentTypes);

		return typeSystem.TryImportMethod(declaringType, methodName, argumentTypes);
	}

	public static MethodReference TryImportMethod(this ITypeSystem typeSystem, Expression<Action> expression)
	{
		GetMethodInfo(expression, out var declaringType, out var methodName, out var argumentTypes);

		return typeSystem.TryImportMethod(declaringType, methodName, argumentTypes);
	}

	public static MethodReference TryImportPropertyGet(this ITypeSystem typeSystem, Type declaringType, string name)
	{
		return TryImport(typeSystem, declaringType, name, t => t.Properties, p => p.GetMethod);
	}

	public static MethodReference TryImportPropertyGet<T>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.TryImportPropertyGet(typeof(T), name);
	}

	public static MethodReference TryImportPropertyGet<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMemberInfo(expression, out var declaringType, out var name);

		return typeSystem.TryImportPropertyGet(declaringType, name);
	}

	public static MethodReference TryImportPropertySet(this ITypeSystem typeSystem, Type declaringType, string name)
	{
		return TryImport(typeSystem, declaringType, name, t => t.Properties, p => p.SetMethod);
	}

	public static MethodReference TryImportPropertySet<T>(this ITypeSystem typeSystem, string name)
	{
		return typeSystem.TryImportPropertySet(typeof(T), name);
	}

	public static MethodReference TryImportPropertySet<TResult>(this ITypeSystem typeSystem, Expression<Func<TResult>> expression)
	{
		GetMemberInfo(expression, out var declaringType, out var name);

		return typeSystem.TryImportPropertySet(declaringType, name);
	}

	public static TypeReference TryImportType(this ITypeSystem typeSystem, Type type)
	{
		if (!typeSystem.TryFindType(GetFullName(type), out var typeDefinition))
		{
			return null;
		}

		return typeSystem.ModuleDefinition.ImportReference(typeDefinition);
	}

	public static TypeReference TryImportType<T>(this ITypeSystem typeSystem)
	{
		return typeSystem.TryImportType(typeof(T));
	}

	private static MethodReference ImportMethod(this ITypeSystem typeSystem, Type declaringType, string name, IReadOnlyList<Type> argumentTypes)
	{
		return TryImport(typeSystem, declaringType, name, t => t.Methods, m => ParametersMatch(m.Parameters, argumentTypes), m => m)
			?? throw new WeavingException($"Can't find method {name}({string.Join(", ", argumentTypes)}) on type {declaringType}");
	}

	private static MethodReference TryImport<T>(this ITypeSystem typeSystem, Type declaringType, string name, Func<TypeDefinition, IEnumerable<T>> elementLookup, Func<T, MethodDefinition> selector)
		where T : class, IMemberDefinition
	{
		return TryImport(typeSystem, declaringType, name, elementLookup, _ => true, selector);
	}

	private static MethodReference TryImport<T>(this ITypeSystem typeSystem, Type declaringType, string name, Func<TypeDefinition, IEnumerable<T>> elementLookup, Func<T, bool> constraints, Func<T, MethodDefinition> selector)
		where T : class, IMemberDefinition
	{
		if (!typeSystem.TryFindType(GetFullName(declaringType), out var typeDefinition))
		{
			return null;
		}

		var method = elementLookup(typeDefinition).Where(p => p.Name == name).Where(constraints).Select(selector).SingleOrDefault();

		if (method == null)
		{
			return null;
		}

		return typeSystem.ModuleDefinition.ImportReference(method);
	}

	private static MethodReference TryImportMethod(this ITypeSystem typeSystem, Type declaringType, string name, IReadOnlyList<Type> argumentTypes)
	{
		return TryImport(typeSystem, declaringType, name, t => t.Methods, m => ParametersMatch(m.Parameters, argumentTypes), m => m);
	}

	#endregion
}