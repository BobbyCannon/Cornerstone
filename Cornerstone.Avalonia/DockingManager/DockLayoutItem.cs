#region References

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[SourceReflection]
public partial class DockLayoutItem : Notifiable
{
	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial ISpeedyList<DockLayoutItem> Children { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string ControlType { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Data { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string DataModelType { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Dock Dock { get; private set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial SplitFractions Fractions { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Header { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial double Height { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Id { get; set; }

	public int Left { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Name { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Orientation Orientation { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Guid SelectedTab { get; set; }

	public int Top { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial double Width { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial ISpeedyList<DockLayoutItem> Windows { get; set; }

	#endregion

	#region Methods

	public static DockLayoutItem From(DockingManager manager)
	{
		var response = From((DockSplitPanel) manager);
		response.Windows = new SpeedyList<DockLayoutItem>(manager.Windows.Select(From).ToArray());
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
			response.Children ??= new SpeedyList<DockLayoutItem>();
			var childResponse = ProcessChild(child);
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
				//response.Data = model.GetLayoutData();
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

	private static DockLayoutItem From(DockSplitPanel panel)
	{
		var response = new DockLayoutItem
		{
			ControlType = nameof(DockSplitPanel)
		};

		foreach (var child in panel.Children)
		{
			response.Children ??= new SpeedyList<DockLayoutItem>();
			var childResponse = ProcessChild(child);
			childResponse.Dock = DockPanel.GetDock(child);
			response.Children.Add(childResponse);
		}

		return response;
	}

	private static DockLayoutItem From(SplitPanel panel)
	{
		var response = new DockLayoutItem
		{
			Orientation = panel.Orientation,
			Fractions = panel.Fractions,
			ControlType = nameof(SplitPanel)
		};

		foreach (var child in panel.Children)
		{
			response.Children ??= new SpeedyList<DockLayoutItem>();
			var childResponse = ProcessChild(child);
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

	private static DockLayoutItem ProcessChild(object child)
	{
		return child switch
		{
			DockableTabView sValue => From(sValue),
			DockingTabControl sValue => From(sValue),
			SplitPanel sValue => From(sValue),
			DockSplitPanel sValue => From(sValue),
			_ => new DockLayoutItem { ControlType = $"Unknown {child.GetType().FullName}" }
		};
	}

	#endregion
}