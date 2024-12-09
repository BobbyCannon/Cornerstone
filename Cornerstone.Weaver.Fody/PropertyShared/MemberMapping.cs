#region References

using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public class MemberMapping
{
	#region Fields

	public FieldDefinition FieldDefinition;
	public PropertyDefinition PropertyDefinition;

	#endregion
}