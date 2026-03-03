#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[SourceReflection]
public partial class DockableTabModel : PopupManager
{
	#region Constructors

	public DockableTabModel()
		: this(Guid.Empty, string.Empty, string.Empty, 24, new Thickness())
	{
	}

	protected DockableTabModel(Guid id, string header, string iconName)
		: this(id, header, iconName, 24, new Thickness())
	{
	}

	protected DockableTabModel(Guid id, string header, string iconName, Thickness iconMargin)
		: this(id, header, iconName, 24, iconMargin)
	{
	}

	protected DockableTabModel(Guid id, string header, string iconName, int? iconSize, Thickness iconMargin)
	{
		Id = id;
		Header = header;
		IconMargin = iconMargin;
		IconName = iconName ?? "FontAwesome.Smile.Solid";
		IconSize = iconSize ?? 24;
	}

	#endregion

	#region Properties

	public ContextMenu ContextMenu => GetContextMenu();

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Header { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial ContextMenu HeaderMenu { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string HeaderToolTip { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial byte[] IconImage { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Thickness IconMargin { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string IconName { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int IconSize { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public Guid Id { get; protected set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsSelected { get; set; }

	#endregion

	#region Methods

	public virtual bool CanCloseTab()
	{
		return true;
	}

	[RelayCommand(CanExecuteMethod = nameof(CanCloseTab))]
	public virtual void Close(object parameter)
	{
		CloseRequested?.Invoke(this, parameter is true);
	}

	public override string ToString()
	{
		return Header;
	}

	protected internal virtual void OnClosing()
	{
	}

	protected virtual ContextMenu GetContextMenu()
	{
		return null;
	}

	#endregion

	#region Events

	public event EventHandler<bool> CloseRequested;

	#endregion
}