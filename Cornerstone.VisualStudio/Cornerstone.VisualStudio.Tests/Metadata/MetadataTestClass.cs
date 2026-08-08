#region References

using System;
using Avalonia.Interactivity;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;

#endregion

namespace Cornerstone.VisualStudio.Tests.Metadata;

/// <summary>
/// This class should not have Event in name
/// as it is used to check if discovery of attached events is working,
/// see <see cref="FieldWrapper" />
/// </summary>
public class MetadataTestClass : RoutedEvent
{
	#region Fields

	/// <summary>
	/// Field which should be recognized as attached event,
	/// as its declaring type is subclass of <see cref="RoutedEvent" />
	/// </summary>
	public static MetadataTestClass Field;

	#endregion

	#region Constructors

	public MetadataTestClass(string name, RoutingStrategies routingStrategies, Type eventArgsType, Type ownerType) : base(name, routingStrategies, eventArgsType, ownerType)
	{
	}

	#endregion
}