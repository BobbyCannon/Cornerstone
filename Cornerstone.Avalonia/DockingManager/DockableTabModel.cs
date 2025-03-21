#region References

using System;
using System.Windows.Input;
using Avalonia.Controls;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public class DockableTabModel : PopupManager
{
	#region Constructors

	public DockableTabModel() : this(Guid.Empty, string.Empty, string.Empty, null)
	{
	}

	protected DockableTabModel(Guid id, string header, string iconName, IDispatcher dispatcher) : base(dispatcher)
	{
		Id = id;
		Header = header;
		IconName = iconName ?? "FontAwesome.Smile.Solid";

		// Commands
		CloseCommand = new RelayCommand(_ => OnCloseRequest(), _ => CanClose);
	}

	#endregion

	#region Properties

	public bool CanClose => CanCloseTab();

	public ICommand CloseCommand { get; }

	public string Header { get; set; }

	public ContextMenu HeaderMenu { get; set; }

	public byte[] IconImage { get; set; }

	public string IconName { get; set; }

	public Guid Id { get; protected set; }

	public bool IsSelected { get; set; }

	public DockingState State { get; protected set; }

	#endregion

	#region Methods

	public virtual bool CanCloseTab()
	{
		return true;
	}

	public string GetLayoutData()
	{
		var response = new PartialUpdate();
		response.AddOrUpdate(nameof(Id), Id);
		response.AddOrUpdate(nameof(Header), Header);
		GetLayoutData(response);
		return response.ToJson();
	}

	public void RestoreLayoutData(string data)
	{
		var update = data.FromJson<PartialUpdate>();
		update.TryGet<Guid>(nameof(Id), x => Id = x);
		update.TryGet<string>(nameof(Header), x => Header = x);
		RestoreLayoutData(update);
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
		// Code Generated - UpdateWith

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(Header, update.Header, settings.ShouldProcessProperty(nameof(Header)), x => Header = x);
		UpdateProperty(HeaderMenu, update.HeaderMenu, settings.ShouldProcessProperty(nameof(HeaderMenu)), x => HeaderMenu = x);
		UpdateProperty(IconImage, update.IconImage, settings.ShouldProcessProperty(nameof(IconImage)), x => IconImage = x);
		UpdateProperty(IconName, update.IconName, settings.ShouldProcessProperty(nameof(IconName)), x => IconName = x);
		UpdateProperty(Id, update.Id, settings.ShouldProcessProperty(nameof(Id)), x => Id = x);
		UpdateProperty(IsSelected, update.IsSelected, settings.ShouldProcessProperty(nameof(IsSelected)), x => IsSelected = x);
		UpdateProperty(State, update.State, settings.ShouldProcessProperty(nameof(State)), x => State = x);

		// Code Generated - /UpdateWith

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

	protected virtual void RestoreLayoutData(PartialUpdate update)
	{
	}

	#endregion

	#region Events

	public event EventHandler<bool> CloseRequest;

	#endregion
}