#region References

using System;

#endregion

namespace Cornerstone.Presentation.SourceGenerator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
internal sealed class SubtypesFactoryAttribute : Attribute
{
	#region Constructors

	public SubtypesFactoryAttribute(Type baseType, string @namespace)
	{
		BaseType = baseType;
		Namespace = @namespace;
	}

	#endregion

	#region Properties

	public Type BaseType { get; }

	public string Namespace { get; }

	#endregion
}