#nullable enable

#region References

using System.Collections.Generic;
using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.LogicalTree;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

public class InitializationOrderTracker : Control, ISupportInitialize
{
	#region Properties

	public int InitState { get; private set; }
	public IList<string> Order { get; } = new List<string>();

	#endregion

	#region Methods

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		Order.Add("AttachedToLogicalTree");
		base.OnAttachedToLogicalTree(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		Order.Add($"Property {change.Property.Name} Changed");
		base.OnPropertyChanged(change);
	}

	void ISupportInitialize.BeginInit()
	{
		++InitState;
		base.BeginInit();
		Order.Add($"BeginInit {InitState}");
	}

	void ISupportInitialize.EndInit()
	{
		--InitState;
		base.EndInit();
		Order.Add($"EndInit {InitState}");
	}

	#endregion
}