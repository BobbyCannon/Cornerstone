#region References

using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public class DockableTabModel : ViewModel
{
	#region Constructors

	public DockableTabModel()
	{
	}

	protected DockableTabModel(Guid id, string header, string iconName, IDispatcher dispatcher) : base(dispatcher)
	{
		Id = id;
		Header = header;
		Icon = ResourceService.GetSvgImage(iconName ?? "FontAwesome.Smile.Solid");

		// Commands
		CloseCommand = new RelayCommand(_ => OnCloseRequest(), _ => CanClose);
	}

	#endregion

	#region Properties

	public bool CanClose => CanCloseTab();

	public ICommand CloseCommand { get; }

	public string Header { get; set; }

	public ContextMenu HeaderMenu { get; set; }

	public Geometry Icon { get; set; }

	public byte[] IconImage { get; set; }

	public Guid Id { get; protected set; }

	public bool IsSelected { get; set; }

	public TabPopup Popup { get; set; }

	public DockingState State { get; protected set; }

	#endregion

	#region Methods

	public void CancelPopup()
	{
		var popup = Popup;
		if (popup == null)
		{
			return;
		}
		if (popup.InProgress)
		{
			return;
		}
		Popup = null;
	}

	public virtual bool CanCloseTab()
	{
		return true;
	}

	public bool CanCreatePopup()
	{
		return this is { Popup: not { InProgress: true } };
	}

	public string GetLayoutData()
	{
		var response = new PartialUpdate();
		response.AddOrUpdate(nameof(Id), Id);
		response.AddOrUpdate(nameof(Header), Header);
		GetLayoutData(response);
		return response.ToJson();
	}

	public async void ProcessPopup()
	{
		if ((Popup == null) || !Popup.Check())
		{
			return;
		}

		Popup.InProgress = true;

		var task = Popup.Process();
		if (task == null)
		{
			Popup = null;
			return;
		}

		var finished = await task;

		PopupProcessed(Popup);

		if (finished)
		{
			Popup = null;
		}
		else
		{
			Popup.InProgress = false;
		}
	}

	public void RestoreLayoutData(string data)
	{
		var update = data.FromJson<PartialUpdate>();
		update.TryGet<Guid>(nameof(Id), x => Id = x);
		update.TryGet<string>(nameof(Header), x => Header = x);
		RestoreLayoutData(update);
	}

	public void ShowAndStartPopup(TabPopup popup)
	{
		popup.TabModel = this;
		Popup = popup;
		ProcessPopup();
	}

	public void ShowPopup(TabPopup popup)
	{
		popup.TabModel = this;
		Popup = popup;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Header;
	}

	/// <summary>
	/// Update the DockableTabModel with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(DockableTabModel update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			Header = UpdateProperty(Header, update.Header);
			HeaderMenu = UpdateProperty(HeaderMenu, update.HeaderMenu);
			Icon = UpdateProperty(Icon, update.Icon);
			IconImage = update.IconImage;
			Id = UpdateProperty(Id, update.Id);
			IsSelected = UpdateProperty(IsSelected, update.IsSelected);
			Popup = UpdateProperty(Popup, update.Popup);
			State = UpdateProperty(State, update.State);
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Header)), x => x.Header = UpdateProperty(Header, update.Header));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(HeaderMenu)), x => x.HeaderMenu = UpdateProperty(HeaderMenu, update.HeaderMenu));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Icon)), x => x.Icon = UpdateProperty(Icon, update.Icon));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IconImage)), x => x.IconImage = update.IconImage);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Id)), x => x.Id = UpdateProperty(Id, update.Id));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsSelected)), x => x.IsSelected = UpdateProperty(IsSelected, update.IsSelected));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Popup)), x => x.Popup = UpdateProperty(Popup, update.Popup));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(State)), x => x.State = UpdateProperty(State, update.State));
		}

		return base.UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			DockableTabModel value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	protected internal virtual void OnClosing()
	{
	}

	protected internal void UpdateState(DockingState state)
	{
		State = state;
	}

	protected virtual void GetLayoutData(PartialUpdate update)
	{
	}

	protected virtual void OnCloseRequest(bool forced = false)
	{
		CloseRequest?.Invoke(this, forced);
	}

	protected virtual void PopupProcessed(TabPopup popup)
	{
	}

	protected virtual void RestoreLayoutData(PartialUpdate update)
	{
	}

	#endregion

	#region Events

	public event EventHandler<bool> CloseRequest;

	#endregion
}