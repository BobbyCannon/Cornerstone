#nullable enable

#pragma warning disable MA0048 // File name must match type name

// https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.CodeAnalysis;
#if !NET6_0_OR_GREATER
[AttributeUsage(
	AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
	AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
	AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
	Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute : Attribute
{
	#region Constructors

	public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
	{
		MemberTypes = memberTypes;
	}

	#endregion

	#region Properties

	public DynamicallyAccessedMemberTypes MemberTypes { get; }

	#endregion
}

[Flags]
internal enum DynamicallyAccessedMemberTypes
{
	None = 0,
	PublicParameterlessConstructor = 0x0001,
	PublicConstructors = 0x0002 | PublicParameterlessConstructor,
	NonPublicConstructors = 0x0004,
	PublicMethods = 0x0008,
	NonPublicMethods = 0x0010,
	PublicFields = 0x0020,
	NonPublicFields = 0x0040,
	PublicNestedTypes = 0x0080,
	NonPublicNestedTypes = 0x0100,
	PublicProperties = 0x0200,
	NonPublicProperties = 0x0400,
	PublicEvents = 0x0800,
	NonPublicEvents = 0x1000,
	Interfaces = 0x2000,
	All = ~None
}

[AttributeUsage(
	AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Method,
	AllowMultiple = true, Inherited = false)]
internal sealed class DynamicDependencyAttribute : Attribute
{
	#region Constructors

	public DynamicDependencyAttribute(string memberSignature)
	{
		MemberSignature = memberSignature;
	}

	public DynamicDependencyAttribute(string memberSignature, Type type)
	{
		MemberSignature = memberSignature;
		Type = type;
	}

	public DynamicDependencyAttribute(string memberSignature, string typeName, string assemblyName)
	{
		MemberSignature = memberSignature;
		TypeName = typeName;
		AssemblyName = assemblyName;
	}

	public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, Type type)
	{
		MemberTypes = memberTypes;
		Type = type;
	}

	public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, string typeName, string assemblyName)
	{
		MemberTypes = memberTypes;
		TypeName = typeName;
		AssemblyName = assemblyName;
	}

	#endregion

	#region Properties

	public string? AssemblyName { get; }
	public string? Condition { get; set; }

	public string? MemberSignature { get; }
	public DynamicallyAccessedMemberTypes MemberTypes { get; }
	public Type? Type { get; }
	public string? TypeName { get; }

	#endregion
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
internal sealed class RequiresUnreferencedCodeAttribute : Attribute
{
	#region Constructors

	public RequiresUnreferencedCodeAttribute(string message)
	{
		Message = message;
	}

	#endregion

	#region Properties

	public string Message { get; }
	public string? Url { get; set; }

	#endregion
}

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class UnconditionalSuppressMessageAttribute : Attribute
{
	#region Constructors

	public UnconditionalSuppressMessageAttribute(string category, string checkId)
	{
		Category = category;
		CheckId = checkId;
	}

	#endregion

	#region Properties

	public string Category { get; }
	public string CheckId { get; }
	public string? Justification { get; set; }
	public string? MessageId { get; set; }
	public string? Scope { get; set; }
	public string? Target { get; set; }

	#endregion
}
#endif