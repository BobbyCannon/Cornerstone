#region References

using System.Collections.Generic;
using System.IO;

#endregion

namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

public interface IAssemblyInformation
{
	#region Properties

	string AssemblyName { get; }
	IEnumerable<ICustomAttributeInformation> CustomAttributes { get; }
	IEnumerable<string> InternalsVisibleTo { get; }
	IEnumerable<string> ManifestResourceNames { get; }
	string Name { get; }
	string PublicKey { get; }
	IEnumerable<ITypeInformation> Types { get; }

	#endregion

	#region Methods

	Stream GetManifestResourceStream(string name);

	#endregion
}

public interface ICustomAttributeInformation
{
	#region Properties

	IList<IAttributeConstructorArgumentInformation> ConstructorArguments { get; }
	string TypeFullName { get; }

	#endregion
}

public interface IAttributeConstructorArgumentInformation
{
	#region Properties

	object? Value { get; }

	#endregion
}

public interface ITypeInformation
{
	#region Properties

	string AssemblyQualifiedName { get; }
	IEnumerable<string> EnumValues { get; }
	IEnumerable<IEventInformation> Events { get; }
	IEnumerable<IFieldInformation> Fields { get; }
	string FullName { get; }
	bool IsAbstract { get; }
	bool IsEnum { get; }
	bool IsGeneric { get; }
	bool IsInterface { get; }
	bool IsInternal { get; }
	bool IsPublic { get; }
	bool IsStatic { get; }
	IEnumerable<IMethodInformation> Methods { get; }
	string Name { get; }
	string Namespace { get; }
	IEnumerable<ITypeInformation> NestedTypes { get; }
	IEnumerable<IPropertyInformation> Properties { get; }
	IEnumerable<string> Pseudoclasses { get; }
	IEnumerable<(ITypeInformation Type, string Name)> TemplateParts { get; }

	#endregion

	#region Methods

	ITypeInformation? GetBaseType();

	#endregion
}

public interface IMethodInformation
{
	#region Properties

	bool IsPublic { get; }
	bool IsStatic { get; }
	string Name { get; }
	IList<IParameterInformation> Parameters { get; }
	string QualifiedReturnTypeFullName { get; }
	string ReturnTypeFullName { get; }

	#endregion
}

public interface IFieldInformation
{
	#region Properties

	bool IsPublic { get; }
	bool IsRoutedEvent { get; }
	bool IsStatic { get; }
	string Name { get; }
	string QualifiedTypeFullName { get; }
	string ReturnTypeFullName { get; }

	#endregion
}

public interface IParameterInformation
{
	#region Properties

	string QualifiedTypeFullName { get; }
	string TypeFullName { get; }

	#endregion
}

public interface IPropertyInformation
{
	#region Properties

	bool HasPublicGetter { get; }
	bool HasPublicSetter { get; }
	bool IsInternal { get; }
	bool IsPublic { get; }
	bool IsStatic { get; }
	string Name { get; }
	string QualifiedTypeFullName { get; }
	string TypeFullName { get; }

	#endregion

	#region Methods

	bool IsVisibleTo(IAssemblyInformation assembly);

	#endregion
}

public interface IEventInformation
{
	#region Properties

	bool IsInternal { get; }
	bool IsPublic { get; }
	string Name { get; }
	string QualifiedTypeFullName { get; }
	string TypeFullName { get; }

	#endregion
}