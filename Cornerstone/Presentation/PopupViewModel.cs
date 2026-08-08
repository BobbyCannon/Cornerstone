#region References

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Presentation;

public partial class PopupViewModel : CornerstoneObject
{
	#region Constants

	public const int DefaultWidth = 500;

	#endregion

	#region Fields

	private PopupManager _popupManager;

	#endregion

	#region Constructors

	public PopupViewModel()
	{
		ProgressDescription = string.Empty;
		ShowButtons = true;
		ButtonCancelText = "Cancel";
		ButtonCancelVisible = true;
		ButtonNoText = "No";
		ButtonNoVisible = false;
		ButtonYesText = "Ok";
		ButtonYesVisible = true;
	}

	#endregion

	#region Properties

	[Notify]
	public partial string ButtonCancelText { get; set; }

	[Notify]
	public partial bool ButtonCancelVisible { get; set; }

	[Notify]
	public partial string ButtonNoText { get; set; }

	[Notify]
	public partial bool ButtonNoVisible { get; set; }

	[Notify]
	public partial string ButtonYesText { get; set; }

	[Notify]
	public partial bool ButtonYesVisible { get; set; }

	[Notify]
	public partial bool InProgress { get; set; }

	[Notify]
	public partial bool IsDestructive { get; set; }

	[Notify]
	public partial string ProgressDescription { get; set; }

	[Notify]
	public partial string ProgressError { get; set; }

	[Notify]
	public partial bool ShowButtons { get; set; }

	[Notify]
	public partial string ValidationError { get; set; }

	#endregion

	#region Methods

	public void AssignHost(PopupManager popupManager)
	{
		_popupManager = popupManager;
	}

	/// <summary>
	/// Process the Yes/No button for the popup.
	/// </summary>
	/// <param name="shouldProcess"> True for yes to process otherwise false meaning do not process. </param>
	/// <returns>
	/// True if the popup is completed otherwise false if the popup is continuing processing.
	/// </returns>
	protected internal virtual Task<bool> Process(bool shouldProcess)
	{
		return Task.FromResult(true);
	}

	protected virtual bool CanExecuteCancel()
	{
		return true;
	}

	protected virtual bool CanExecuteNo(object parameter)
	{
		return true;
	}

	protected virtual bool CanExecuteYes(object parameter)
	{
		var result = ValidateAllProperties();
		return result;
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		ExecuteYesCommand?.Refresh();
		ExecuteNoCommand?.Refresh();
		ExecuteCancelCommand?.Refresh();
		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	/// <summary>
	/// Update progress text. Safe to call from background threads (e.g. git process callbacks).
	/// </summary>
	protected void SetProgressDescription(string description)
	{
		DispatchToUi(() => ProgressDescription = description);
	}

	/// <summary>
	/// Update progress error text. Safe to call from background threads.
	/// </summary>
	protected void SetProgressError(string error)
	{
		DispatchToUi(() => ProgressError = error);
	}

	protected virtual bool ValidateAllProperties()
	{
		var propertiesToValidate = GetPropertiesToValidate();
		foreach (var entry in propertiesToValidate)
		{
			var value = entry.Key.GetValue(this);
			if (entry.Key.PropertyType == typeof(string))
			{
				if ((entry.Value.AllowEmptyStrings && (value == null))
					|| string.IsNullOrWhiteSpace(value as string))
				{
					ValidationError = entry.Value.ErrorMessage;
					return false;
				}
			}

			if (value == null)
			{
				ValidationError = entry.Value.ErrorMessage;
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Run UI-bound popup property updates on the app dispatcher when needed.
	/// Git popups call progress helpers from Task.Run / process output handlers.
	/// </summary>
	private static void DispatchToUi(Action action)
	{
		// todo: fix this when we move to KeyStone, leaving it for now.
		// Do NOT use this pattern elsewhere. AppBootstrap should NOT be used directly.
		var provider = AppBootstrap.DependencyProvider;
		if ((provider != null)
			&& provider.TryGetInstance<IDispatcher>(out var dispatcher)
			&& dispatcher.ShouldDispatch())
		{
			dispatcher.Dispatch(action);
			return;
		}

		action();
	}

	[RelayCommand(CanExecuteMethod = nameof(CanExecuteCancel))]
	private void ExecuteCancel()
	{
		_popupManager?.CancelPopup();
	}

	[RelayCommand(CanExecuteMethod = nameof(CanExecuteNo))]
	private void ExecuteNo()
	{
		_popupManager?.ProcessPopupAsync(false);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanExecuteYes))]
	private void ExecuteYes()
	{
		_popupManager?.ProcessPopupAsync(true);
	}

	private IDictionary<PropertyInfo, RequiredAttribute> GetPropertiesToValidate()
	{
		// todo: implement
		return new Dictionary<PropertyInfo, RequiredAttribute>();
	}

	#endregion
}