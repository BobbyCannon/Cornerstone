#region References

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Presentation;

public partial class PopupViewModel : Bindable
{
	#region Constants

	public const int DefaultWidth = 500;

	#endregion

	#region Fields

	private PopupManager _popupManager;

	#endregion

	#region Constructors

	public PopupViewModel() : this(null)
	{
	}

	public PopupViewModel(IDispatcher dispatcher) : base(dispatcher)
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

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.Dispatch(() =>
		{
			ExecuteYesCommand?.Refresh();
			ExecuteNoCommand?.Refresh();
			ExecuteCancelCommand?.Refresh();
		});

		base.OnPropertyChanged(propertyName);
	}

	protected void SetProgressDescription(string description)
	{
		this.Dispatch(() => ProgressDescription = description);
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