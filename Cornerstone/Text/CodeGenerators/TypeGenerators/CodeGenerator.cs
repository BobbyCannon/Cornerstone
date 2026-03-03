#region References

using System;
using System.Linq;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public abstract class CodeGenerator
{
	#region Fields

	private readonly Type[] _supportedTypes;

	#endregion

	#region Constructors

	protected CodeGenerator(params Type[] types)
	{
		_supportedTypes = types;
	}

	#endregion

	#region Methods

	public Type[] GetSupportedTypes()
	{
		return _supportedTypes;
	}

	public virtual bool SupportsType(Type type)
	{
		return _supportedTypes.Contains(type);
	}

	public abstract void WriteObject(CodeBuilder builder, SourceTypeInfo type, object value);

	#endregion
}