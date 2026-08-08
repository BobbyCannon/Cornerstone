#region References

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Cornerstone.Avalonia.Controls;
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
			PopupViewModel popup => Build(popup),
			TabItemReferenceViewModel tabItem => Build(tabItem),
			ViewModel viewModel => Build(viewModel),
			_ => new TextBlock { Text = $"Failed to find control for [{data}]." }
		};
	}

	public bool Match(object data)
	{
		return data
			is ViewModel
			or PopupViewModel
			or TabItemReferenceViewModel;
	}

	private Control Build(TabItemReferenceViewModel tabItem)
	{
		// Reuse the existing control when switching back to a tab. Nulling on Unloaded forced a
		// brand-new instance every visit; DocumentationReader could keep catalog/Current while
		// MarkdownView had not re-presented document text until Home reloaded it.
		if (tabItem.Control is not null)
		{
			return tabItem.Control;
		}

		tabItem.Control = SourceReflector.CreateInstance(tabItem.TabTypeName) as Control
			?? new TextBlock { Text = $"Failed to find control for [{tabItem.TabName}]." };

		tabItem.Control.Tag = tabItem;
		return tabItem.Control;
	}

	private Control Build(PopupViewModel viewModel)
	{
		var modelType = viewModel.GetType();
		var modelName = modelType.Name;
		var modelAssemblyName = modelType.ToAssemblyName();

		var viewAssemblyName = modelAssemblyName?.Replace(modelName, modelName.Replace("Popup", "Control"));
		if (SourceReflector.CreateInstance(viewAssemblyName) is Control view)
		{
			view.DataContext = viewModel;
			return view;
		}

		return new TextBlock { Text = $"Failed to find control for [{viewModel.GetType().Name}]." };
	}

	private Control Build(ViewModel data)
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

	#endregion
}