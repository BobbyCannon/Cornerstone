#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[SourceReflection]
public partial class ToolbarTabModel : DockableTabModel
{
	#region Constructors

	public ToolbarTabModel()
		: base(Guid.Empty, string.Empty, string.Empty, 24, new Thickness())
	{
	}

	protected ToolbarTabModel(Guid id, string header, string iconName)
		: base(id, header, iconName, 24, new Thickness())
	{
	}

	protected ToolbarTabModel(Guid id, string header, string iconName, Thickness iconMargin)
		: base(id, header, iconName, 24, iconMargin)
	{
	}

	#endregion
}

[SourceReflection]
public partial class DocumentTabModel : DockableTabModel
{
	#region Constructors

	public DocumentTabModel()
		: base(Guid.Empty, string.Empty, string.Empty, 24, new Thickness())
	{
	}

	protected DocumentTabModel(Guid id, string header, string iconName)
		: base(id, header, iconName, 24, new Thickness())
	{
	}

	protected DocumentTabModel(Guid id, string header, string iconName, Thickness iconMargin)
		: base(id, header, iconName, 24, iconMargin)
	{
	}

	#endregion
}

[SourceReflection]
public abstract partial class DockableTabModel : PopupManager
{
	#region Constructors

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

	public string ReadLayoutData()
	{
		var response = new PartialUpdate();
		response.AddOrUpdate(nameof(Id), Id.ToString());
		response.AddOrUpdate(nameof(Header), Header);
		ReadLayoutData(response);
		return response.ToJson();
	}

	public void RestoreLayoutData(string data)
	{
		var update = data.FromJson<PartialUpdate>();
		update.TryUpdate<Guid>(x => Id = x, nameof(Id));
		update.TryUpdate<string>(x => Header = x, nameof(Header));
		RestoreLayoutData(update);
	}

	public override string ToString()
	{
		return Header;
	}

	protected internal virtual void OnClosing()
	{
	}

	protected virtual void ReadLayoutData(PartialUpdate update)
	{
	}

	protected virtual void RestoreLayoutData(PartialUpdate update)
	{
	}

	#endregion

	#region Events

	public event EventHandler<bool> CloseRequested;

	#endregion
}