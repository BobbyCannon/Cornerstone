#region References

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.DockingManager;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia;

#pragma warning disable IL2072

public class ViewLocator : IDataTemplate
{
	#region Methods

	public Control Build(object data)
	{
		return data switch
		{
			DockableTabModel dockableTabModel => Build(dockableTabModel),
			PopupViewModel popup => Build(popup),
			TabItemReferenceViewModel tabItem => Build(tabItem),
			_ => new TextBlock { Text = $"Failed to find control for [{data}]." }
		};
	}

	public bool Match(object data)
	{
		return data
			is DockableTabModel
			or PopupViewModel
			or TabItemReferenceViewModel;
	}

	private Control Build(TabItemReferenceViewModel tabItem)
	{
		tabItem.Control = SourceReflector.CreateInstance(tabItem.TabTypeName) as Control
			?? new TextBlock { Text = $"Failed to find control for [{tabItem.TabName}]." };

		tabItem.Control.Tag = tabItem;
		tabItem.Control.Unloaded += ControlOnUnloaded;

		return tabItem.Control;
	}

	private Control Build(PopupViewModel popupViewModel)
	{
		return new TextBlock { Text = popupViewModel.ProgressDescription };
	}

	private Control Build(DockableTabModel data)
	{
		var modelType = data.GetType();
		var modelName = modelType.Name;
		var modelAssemblyName = modelType.ToAssemblyName();

		// todo: should this be attribute driven? [AssociatedView(nameof(TextEditor))]

		// For "Cornerstone.Models.TestViewModel" => "TestView"
		var viewAssemblyName = modelAssemblyName?.Replace(modelName, modelName.Replace("Model", ""));
		if (SourceReflector.CreateInstance(viewAssemblyName) is Control view)
		{
			view.DataContext = data;
			return view;
		}

		// For "Cornerstone.Models.TextEditorViewModel" => "TextEditor"
		viewAssemblyName = modelAssemblyName?.Replace(modelName, modelName.Replace("ViewModel", ""));
		if (SourceReflector.CreateInstance(viewAssemblyName) is Control view2)
		{
			view2.DataContext = data;
			return view2;
		}

		return new TextBlock { Text = $"Failed to find control for [{viewAssemblyName}]..." };
	}

	private void ControlOnUnloaded(object sender, RoutedEventArgs e)
	{
		if (sender is not Control { Tag: TabItemReferenceViewModel tabItem } control)
		{
			return;
		}

		tabItem.Control.Unloaded -= ControlOnUnloaded;
		tabItem.Control = null;
		control.Tag = null;
	}

	#endregion
}