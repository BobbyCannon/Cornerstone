#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Weaver.Fody;

public static class Constants
{
	#region Constants

	public const string AddINotifyPropertyChangedInterfaceAttribute = $"{WeaverNamespace}.AddINotifyPropertyChangedInterfaceAttribute";
	public const string AlsoNotifyForAttribute = $"{WeaverNamespace}.AlsoNotifyForAttribute";
	public const string AttributeOfDoNotNotify = $"{WeaverNamespace}.AttributeOfDoNotNotify";
	public const string CompilerGeneratedAttribute = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
	public const string DependsOnAttribute = $"{WeaverNamespace}.DependsOnAttribute";
	public const string DoNotCheckEqualityAttribute = $"{WeaverNamespace}.DoNotCheckEqualityAttribute";
	public const string DoNotNotifyAttribute = $"{WeaverNamespace}.DoNotNotifyAttribute";
	public const string DoNotSetChangedAttribute = $"{WeaverNamespace}.DoNotSetChangedAttribute";
	public const string FilterTypeAttribute = $"{WeaverNamespace}.FilterTypeAttribute";
	public const string ImplementPropertyChangingAttribute = $"{WeaverNamespace}.ImplementPropertyChangingAttribute";
	public const string OnChangedMethodAttribute = $"{WeaverNamespace}.OnChangedMethodAttribute";
	public const string SuppressPropertyChangedWarningsAttribute = $"{WeaverNamespace}.SuppressPropertyChangedWarningsAttribute";
	public const string WeakEventAttributeName = $"{WeaverNamespace}.WeakEventAttribute";
	public const string WeaverNamespace = "Cornerstone.Weaver";

	#endregion

	#region Fields

	public static readonly List<string> AttributeNames;

	#endregion

	#region Constructors

	static Constants()
	{
		AttributeNames =
		[
			DoNotNotifyAttribute,
			AlsoNotifyForAttribute,
			DependsOnAttribute,
			ImplementPropertyChangingAttribute
		];
	}

	#endregion
}