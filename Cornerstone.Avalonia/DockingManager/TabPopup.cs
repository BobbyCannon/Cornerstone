#region References

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Media;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Presentation;
using Dispatcher = Avalonia.Threading.Dispatcher;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public class TabPopup : Bindable
{
	#region Constants

	public const int DefaultWidth = 500;

	#endregion

	#region Fields

	private bool _inProgress;
	private bool _isDestructive;
	private string _progressDescription;
	private bool _showButtons;

	#endregion

	#region Constructors

	public TabPopup()
	{
		ProgressDescription = string.Empty;
		ShowButtons = true;
		CancelButtonEnabled = true;
		OkButtonEnabled = true;
	}

	#endregion

	#region Properties

	public IBrush BorderBrush =>
		IsDestructive
			? ResourceService.GetColorAsBrush("Red06")
			: ResourceService.GetColorAsBrush("Background06");

	public virtual bool CancelButtonEnabled { get; }

	public object ControlForView { get; protected set; }

	public bool InProgress
	{
		get => _inProgress;
		set
		{
			if (SetProperty(ref _inProgress, value))
			{
				OnPropertyChanged(nameof(ShowButtons));
			}
		}
	}

	public bool IsDestructive
	{
		get => _isDestructive;
		set => SetProperty(ref _isDestructive, value);
	}

	public virtual bool OkButtonEnabled { get; }

	public string ProgressDescription
	{
		get => _progressDescription;
		set => SetProperty(ref _progressDescription, value);
	}

	public bool ShowButtons
	{
		get => _showButtons && !InProgress;
		set => SetProperty(ref _showButtons, value);
	}

	public DockableTabModel TabModel { get; set; }

	#endregion

	#region Methods

	public void Cancel()
	{
		TabModel?.CancelPopup();
	}

	[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
	public bool Check()
	{
		//if (HasErrors)
		//{
		//	return false;
		//}
		//ValidateAllProperties();
		//return !HasErrors;
		return true;
	}

	public void StartProcess()
	{
		TabModel.ProcessPopup();
	}

	/// <summary>
	/// Process the OK button for the popup.
	/// </summary>
	/// <returns>
	/// True if the popup is completed otherwise false if the popup is continuing processing.
	/// </returns>
	protected internal virtual Task<bool> Process()
	{
		return Task.FromResult(true);
	}

	protected void Dispatch(Action action)
	{
		Dispatcher.UIThread.Invoke(action);
	}

	protected void SendNotification(string message)
	{
		Debugger.Break();
		//Dispatch(() => App.SendNotification(HostPageId, message));
	}

	protected void SetProgressDescription(string description)
	{
		Dispatch(() => ProgressDescription = description);
	}

	#endregion
}