#region References

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public class DockLayoutItem
{
	#region Properties

	public IList<DockLayoutItem> Children { get; set; }

	public string ControlType { get; set; }

	public string Data { get; set; }

	public string DataModelType { get; set; }

	public Dock Dock { get; private set; }

	public SplitFractions Fractions { get; set; }

	public string Header { get; set; }

	public double Height { get; set; }

	public string Id { get; set; }

	public string Name { get; set; }

	public Orientation Orientation { get; set; }

	public string SelectedTab { get; set; }

	public double Width { get; set; }

	public IList<DockLayoutItem> Windows { get; set; }

	#endregion

	#region Methods

	public static DockLayoutItem From(DockingManager manager)
	{
		var response = From((DockSplitPanel) manager);
		response.Windows = manager.TabWindows.Select(From).ToList();
		return response;
	}

	public static DockLayoutItem From(DockSplitPanel panel)
	{
		var response = new DockLayoutItem
		{
			ControlType = nameof(DockSplitPanel)
		};

		foreach (var child in panel.Children)
		{
			response.Children ??= new List<DockLayoutItem>();
			var childResponse = ProcessChild(child);
			childResponse.Dock = DockPanel.GetDock(child);
			response.Children.Add(childResponse);
		}

		return response;
	}

	public static DockLayoutItem From(SplitPanel panel)
	{
		var response = new DockLayoutItem
		{
			Orientation = panel.Orientation,
			Fractions = panel.Fractions,
			ControlType = nameof(SplitPanel)
		};

		foreach (var child in panel.Children)
		{
			response.Children ??= new List<DockLayoutItem>();
			var childResponse = ProcessChild(child);
			response.Children.Add(childResponse);
		}

		return response;
	}

	private static DockLayoutItem From(DockTabWindow window)
	{
		var response = new DockLayoutItem
		{
			ControlType = nameof(DockTabWindow),
			Height = window.Height,
			Width = window.Width
		};

		response.Children ??= new List<DockLayoutItem>();
		response.Children.Add(From(window.TabItem));

		return response;
	}

	private static DockLayoutItem From(DockingTabControl tabControl)
	{
		var response = new DockLayoutItem
		{
			Height = tabControl.Height,
			Width = tabControl.Width,
			ControlType = nameof(DockingTabControl)
		};

		foreach (var child in tabControl.Items)
		{
			response.Children ??= new List<DockLayoutItem>();
			var childResponse = ProcessChild(child);
			response.Children.Add(childResponse);

			if ((child == tabControl.SelectedItem)
				&& child is DockableTabItem tabItem)
			{
				response.SelectedTab = tabItem.TabModel.Id;
			}
		}

		return response;
	}

	private static DockLayoutItem From(DockableTabItem tabItem)
	{
		var response = new DockLayoutItem
		{
			Header = tabItem.Header?.ToString(),
			ControlType = nameof(DockableTabItem)
		};

		switch (tabItem.Content)
		{
			case DockableTabModel model:
			{
				response.Data = model.GetLayoutData();
				response.DataModelType = model.GetType().ToAssemblyName();
				break;
			}
			case string sValue:
			{
				response.Data = sValue;
				break;
			}
		}

		return response;
	}

	private static DockLayoutItem ProcessChild(object child)
	{
		return child switch
		{
			DockableTabItem sValue => From(sValue),
			DockingTabControl sValue => From(sValue),
			SplitPanel sValue => From(sValue),
			DockSplitPanel sValue => From(sValue),
			_ => new DockLayoutItem { ControlType = $"Unknown {child.GetType().FullName}" }
		};
	}

	#endregion
}