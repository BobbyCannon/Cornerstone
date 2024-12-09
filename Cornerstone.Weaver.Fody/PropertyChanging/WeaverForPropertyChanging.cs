#region References

using Cornerstone.Weaver.Fody.PropertyShared;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging : WeaverForPropertyChange
{
	#region Constructors

	public WeaverForPropertyChanging(ModuleWeaver moduleWeaver) : base(moduleWeaver)
	{
	}

	#endregion

	#region Methods

	public override void Weave()
	{
		ResolveOnPropertyNameChangingConfig();
		ResolveEventInvokerName();
		FindCoreReferences();
		FindInterceptor();
		BuildTypeNodes();
		CleanDoNotNotifyTypes();
		CleanCodeGenedTypes();
		FindMethodsForNodes();
		FindAllProperties();
		FindMappings();
		DetectIlGeneratedByDependency();
		ProcessDependsOnAttributes();
		WalkPropertyData();
		CheckForWarnings();
		ProcessOnChangingMethods();
		CheckForStackOverflow();
		FindComparisonMethods();
		ProcessTypes();
		CleanAttributes();
	}

	#endregion
}