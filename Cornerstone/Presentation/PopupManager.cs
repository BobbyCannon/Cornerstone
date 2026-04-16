#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

[SourceReflection]
public partial class PopupManager : ViewModel, IPopupManager
{
	#region Properties

	[Notify]
	public partial PopupViewModel Popup { get; set; }

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

	public bool CanCreatePopup()
	{
		return this is { Popup: not { InProgress: true } };
	}

	public async Task ProcessPopupAsync(bool accept)
	{
		if (Popup == null)
		{
			return;
		}

		Popup.InProgress = true;

		var result = Popup.Process(accept);
		if (result == null)
		{
			OnPopupClosed(Popup);
			Popup = null;
			return;
		}

		var processCompleted = await result;

		PopupProcessed(Popup, processCompleted);

		if (processCompleted)
		{
			OnPopupClosed(Popup);
			Popup = null;
		}
		else
		{
			Popup.InProgress = false;
		}
	}

	public void ShowAndStartPopup(PopupViewModel popup, bool process)
	{
		TryShowPopup(popup);
		_ = ProcessPopupAsync(process);
	}

	public bool TryShowPopup(PopupViewModel popup)
	{
		if (!CanCreatePopup())
		{
			return false;
		}

		popup.AssignHost(this);
		Popup = popup;
		return true;
	}

	protected virtual void OnPopupClosed(PopupViewModel e)
	{
		PopupClosed?.Invoke(this, e);
	}

	protected virtual void PopupProcessed(PopupViewModel popup, bool processCompleted)
	{
	}

	#endregion

	#region Events

	public event EventHandler<PopupViewModel> PopupClosed;

	#endregion
}

public interface IPopupManager
{
	#region Properties

	PopupViewModel Popup { get; set; }

	#endregion

	#region Methods

	void CancelPopup();

	bool CanCreatePopup();

	/// <summary>
	/// Process the popup.
	/// </summary>
	/// <param name="accept"> True for yes otherwise false for no. </param>
	/// <returns> The task. </returns>
	Task ProcessPopupAsync(bool accept);

	/// <summary>
	/// Show and start the process.
	/// </summary>
	/// <param name="popup"> The popup to show and process. </param>
	/// <param name="process"> True for yes otherwise false for no. </param>
	void ShowAndStartPopup(PopupViewModel popup, bool process);

	bool TryShowPopup(PopupViewModel popup);

	#endregion

	#region Events

	event EventHandler<PopupViewModel> PopupClosed;

	#endregion
}