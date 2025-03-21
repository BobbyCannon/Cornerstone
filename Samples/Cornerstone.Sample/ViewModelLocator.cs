#region References

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.DockingManager;

#endregion

namespace Cornerstone.Sample;

public class ViewModelLocator : IDataTemplate
{
	#region Properties

	public static Func<DockableTabModel, Control> PlatformLookup { get; set; }

	#endregion

	#region Methods

	public Control Build(object data)
	{
		var response = PlatformLookup?.Invoke(data as DockableTabModel);
		if (response != null)
		{
			return response;
		}

		if (data is TabItemReferenceViewModel tabItem)
		{
			return CornerstoneApplication.GetInstance(tabItem.TabTypeName) as Control
				?? new TextBlock { Text = $"Failed to find control for [{tabItem.TabName}]..." };
		}

		return new TextBlock { Text = $"Failed to find control for [{data}]..." };
	}

	public bool Match(object data)
	{
		return data
			is DockableTabModel
			or TabItemReferenceViewModel;
	}

	#endregion
}