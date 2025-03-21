#region References

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Presentation;

public class PopupViewModel : Bindable
{
	#region Constants

	public const int DefaultWidth = 500;

	#endregion

	#region Fields

	private bool _inProgress;
	private PopupManager _popupManager;
	private Dictionary<PropertyInfo, RequiredAttribute> _propertiesToValidate;
	private bool _showButtons;

	#endregion

	#region Constructors

	public PopupViewModel()
	{
		ProgressDescription = string.Empty;
		ShowButtons = true;
		CancelButtonEnabled = true;
		OkButtonEnabled = true;

		CancelCommand = new RelayCommand(_ => Cancel());
		StartProcessCommand = new RelayCommand(_ => StartProcess());
	}

	#endregion

	#region Properties

	public bool CancelButtonEnabled { get; set; }

	public ICommand CancelCommand { get; }

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

	public bool IsDestructive { get; set; }

	public bool OkButtonEnabled { get; set; }

	public string ProgressDescription { get; set; }

	public bool ShowButtons
	{
		get => _showButtons && !InProgress;
		set => SetProperty(ref _showButtons, value);
	}

	public ICommand StartProcessCommand { get; }

	public string ValidationError { get; set; }

	#endregion

	#region Methods

	public void AssignHost(PopupManager popupManager)
	{
		_popupManager = popupManager;
	}

	public bool Check()
	{
		var result = ValidateAllProperties();
		OkButtonEnabled = result;
		return result;
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

	protected void SendNotification(string message)
	{
		Debugger.Break();
		//Dispatch(() => App.SendNotification(HostPageId, message));
	}

	protected void SetProgressDescription(string description)
	{
		this.Dispatch(() => ProgressDescription = description);
	}

	private void Cancel()
	{
		_popupManager?.CancelPopup();
	}

	private IDictionary<PropertyInfo, RequiredAttribute> GetPropertiesToValidate()
	{
		if (_propertiesToValidate != null)
		{
			return _propertiesToValidate;
		}

		_propertiesToValidate = this.GetCachedProperties()
			.Select(x => new { p = x, a = x.GetCustomAttribute<RequiredAttribute>() })
			.Where(x => x.a != null)
			.ToDictionary(x => x.p, x => x.a);

		return _propertiesToValidate;
	}

	private void StartProcess()
	{
		_popupManager?.ProcessPopupAsync();
	}

	protected virtual bool ValidateAllProperties()
	{
		var propertiesToValidate = GetPropertiesToValidate();
		foreach (var entry in propertiesToValidate)
		{
			var value = entry.Key.GetValue(this);
			if (value == null)
			{
				ValidationError = entry.Value.ErrorMessage;
				return false;
			}
		}

		return true;
	}

	#endregion
}