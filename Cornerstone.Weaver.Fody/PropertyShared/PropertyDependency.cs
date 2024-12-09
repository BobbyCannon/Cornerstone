#region References

using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public class PropertyDependency
{
	#region Fields

	public PropertyDefinition ShouldAlsoNotifyFor;
	public PropertyDefinition WhenPropertyIsSet;

	#endregion
}