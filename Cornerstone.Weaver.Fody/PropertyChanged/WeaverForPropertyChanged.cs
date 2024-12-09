#region References

using Cornerstone.Weaver.Fody.PropertyShared;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged : WeaverForPropertyChange
{
	#region Constructors

	public WeaverForPropertyChanged(ModuleWeaver moduleWeaver) : base(moduleWeaver)
	{
		SuppressOnPropertyNameChangedWarning = true;
	}

	#endregion

	#region Methods

	public override void Weave()
	{
		ResolveOnPropertyNameChangedConfig();
		ResolveEnableIsChangedPropertyConfig();
		ResolveTriggerDependentPropertiesConfig();
		ResolveCheckForEqualityConfig();
		ResolveCheckForEqualityUsingBaseEqualsConfig();
		ResolveUseStaticEqualsFromBaseConfig();
		ResolveSuppressWarningsConfig();
		ResolveSuppressOnPropertyNameChangedWarningConfig();
		ResolveEventInvokerName();
		FindCoreReferences();
		FindInterceptor();
		ProcessFilterTypeAttributes();
		BuildTypeNodes();
		CleanDoNotNotifyTypes();
		CleanCodeGenedTypes();
		FindMethodsForNodes();
		FindIsChangedMethod();
		FindAllProperties();
		FindMappings();
		DetectIlGeneratedByDependency();
		ProcessDependsOnAttributes();
		WalkPropertyData();
		CheckForWarnings();
		ProcessOnChangedMethods();
		CheckForStackOverflow();
		FindComparisonMethods();
		InitEventArgsCache();
		ProcessTypes();
		InjectEventArgsCache();
		CleanAttributes();
	}

	#endregion
}