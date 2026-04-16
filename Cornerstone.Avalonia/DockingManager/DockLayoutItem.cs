#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class DockLayoutItem : Notifiable
{
	#region Properties

	public partial string[] AllowedDockTypeNames { get; set; }
	public partial List<DockLayoutItem> Children { get; set; }
	public partial string ControlType { get; set; }
	public partial string Data { get; set; }
	public partial string DataModelType { get; set; }
	public partial Dock Dock { get; private set; }
	public partial SplitFractions Fractions { get; set; }
	public partial string Header { get; set; }
	public partial double Height { get; set; }
	public partial bool IsRootTabControl { get; set; }
	public partial int Left { get; set; }
	public partial string Name { get; set; }
	public partial Orientation Orientation { get; set; }
	public partial Guid SelectedTab { get; set; }
	public partial int Top { get; set; }
	public partial double Width { get; set; }
	public partial DockLayoutItem[] Windows { get; set; }

	#endregion

	#region Methods

	public static DockLayoutItem From(DockingManager manager)
	{
		var response = From(manager, manager);
		response.Windows = manager.Windows.Select(From).ToArray();
		return response;
	}

	private static DockLayoutItem From(DockingManager rootDockingManager, DockingTabControl tabControl)
	{
		var response = new DockLayoutItem
		{
			AllowedDockTypeNames = tabControl.AllowedDockTypes.Select(x => x.ToAssemblyName()).ToArray(),
			ControlType = nameof(DockingTabControl),
			IsRootTabControl = rootDockingManager.RootTabControl == tabControl,
			Height = tabControl.Height,
			Width = tabControl.Width
		};

		foreach (var child in tabControl.Items)
		{
			response.Children ??= [];
			var childResponse = ProcessChild(rootDockingManager, child);
			response.Children.Add(childResponse);

			if ((child == tabControl.SelectedItem)
				&& child is DockableTabView tabItem)
			{
				response.SelectedTab = tabItem.TabModel.Id;
			}
		}

		return response;
	}

	private static DockLayoutItem From(DockableTabView tabView)
	{
		var response = new DockLayoutItem
		{
			Header = tabView.Header?.ToString(),
			ControlType = nameof(DockableTabView)
		};

		switch (tabView.Content)
		{
			case DockableTabModel model:
			{
				response.Data = model.ReadLayoutData();
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

	private static DockLayoutItem From(DockingManager rootDockingManager, DockSplitPanel panel)
	{
		var response = new DockLayoutItem
		{
			ControlType = nameof(DockSplitPanel)
		};

		foreach (var child in panel.Children)
		{
			response.Children ??= [];
			var childResponse = ProcessChild(rootDockingManager, child);
			childResponse.Dock = DockPanel.GetDock(child);
			response.Children.Add(childResponse);
		}

		return response;
	}

	private static DockLayoutItem From(DockingManager rootDockingManager, SplitPanel panel)
	{
		var response = new DockLayoutItem
		{
			Orientation = panel.Orientation,
			Fractions = panel.Fractions,
			ControlType = nameof(SplitPanel)
		};

		foreach (var child in panel.Children)
		{
			response.Children ??= [];
			var childResponse = ProcessChild(rootDockingManager, child);
			response.Children.Add(childResponse);
		}

		return response;
	}

	private static DockLayoutItem From(DockingWindow window)
	{
		var response = From(window.DockingManager);
		response.ControlType = nameof(DockingWindow);
		response.Left = window.Position.X;
		response.Top = window.Position.Y;
		response.Height = window.Height;
		response.Width = window.Width;
		return response;
	}

	private static DockLayoutItem ProcessChild(DockingManager rootDockingManager, object child)
	{
		return child switch
		{
			DockableTabView sValue => From(sValue),
			DockingTabControl sValue => From(rootDockingManager, sValue),
			SplitPanel sValue => From(rootDockingManager, sValue),
			DockSplitPanel sValue => From(rootDockingManager, sValue),
			_ => new DockLayoutItem { ControlType = $"Unknown {child.GetType().FullName}" }
		};
	}

	#endregion
}