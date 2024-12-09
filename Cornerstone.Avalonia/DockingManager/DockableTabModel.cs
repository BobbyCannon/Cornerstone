#region References

using System;
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

	protected DockableTabModel(Guid id, string header, string iconName, IDispatcher dispatcher)
		: this(id.ToString(), header, iconName, dispatcher)
	{
	}

	protected DockableTabModel(string id, string header, string iconName, IDispatcher dispatcher) : base(dispatcher)
	{
		Id = id;
		Header = header;
		Icon = ResourceService.GetSvgImage(iconName ?? "FontAwesome.Smile.Solid");
	}

	#endregion

	#region Properties

	public string Header { get; set; }

	public Geometry Icon { get; set; }

	public byte[] IconImage { get; set; }

	public string Id { get; protected set; }

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
		update.TryGet<string>(nameof(Id), x => Id = x);
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

	protected internal void UpdateState(DockingState state)
	{
		State = state;
	}

	protected virtual void GetLayoutData(PartialUpdate update)
	{
	}

	protected virtual void RestoreLayoutData(PartialUpdate update)
	{
	}

	#endregion
}