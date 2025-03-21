#region References

using System.Threading.Tasks;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Presentation;

public class PopupManager : ViewModel, IPopupManager
{
	#region Constructors

	public PopupManager() : this(null)
	{
	}

	public PopupManager(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	public PopupViewModel Popup { get; set; }

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

	public async Task ProcessPopupAsync()
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

	public void ShowAndStartPopup(PopupViewModel popup)
	{
		TryShowPopup(popup);
		_ = ProcessPopupAsync();
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

	protected virtual void PopupProcessed(PopupViewModel popup)
	{
	}

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

	Task ProcessPopupAsync();

	void ShowAndStartPopup(PopupViewModel popup);

	bool TryShowPopup(PopupViewModel popup);

	#endregion
}