#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class FilterViewModel : ViewModel, INotifyDataErrorInfo
{
	#region Fields

	private readonly Dictionary<string, string> _errors;
	private Regex _filterRegex;
	private string _filterString;
	private bool _useRegexFilter, _useCaseSensitiveFilter, _useWholeWordFilter;

	#endregion

	#region Constructors

	public FilterViewModel()
	{
		_errors = new();
		_filterString = string.Empty;
	}

	#endregion

	#region Properties

	public string FilterString
	{
		get => _filterString;
		set
		{
			if (SetProperty(ref _filterString, value))
			{
				UpdateFilterRegex();
				RefreshFilter?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public bool HasErrors => _errors.Count > 0;

	public bool UseCaseSensitiveFilter
	{
		get => _useCaseSensitiveFilter;
		set
		{
			if (SetProperty(ref _useCaseSensitiveFilter, value))
			{
				UpdateFilterRegex();
				RefreshFilter?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public bool UseRegexFilter
	{
		get => _useRegexFilter;
		set
		{
			if (SetProperty(ref _useRegexFilter, value))
			{
				UpdateFilterRegex();
				RefreshFilter?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public bool UseWholeWordFilter
	{
		get => _useWholeWordFilter;
		set
		{
			if (SetProperty(ref _useWholeWordFilter, value))
			{
				UpdateFilterRegex();
				RefreshFilter?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	#endregion

	#region Methods

	public bool Filter(string input)
	{
		return _filterRegex?.IsMatch(input) ?? true;
	}

	public IEnumerable GetErrors(string propertyName)
	{
		if ((propertyName != null)
			&& _errors.TryGetValue(propertyName, out var error))
		{
			yield return error;
		}
	}

	private void UpdateFilterRegex()
	{
		void ClearError()
		{
			if (_errors.Remove(nameof(FilterString)))
			{
				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FilterString)));
			}
		}

		try
		{
			var options = RegexOptions.Compiled;
			var pattern = UseRegexFilter
				? FilterString.Trim()
				: Regex.Escape(FilterString.Trim());
			if (!UseCaseSensitiveFilter)
			{
				options |= RegexOptions.IgnoreCase;
			}
			if (UseWholeWordFilter)
			{
				pattern = $"\\b(?:{pattern})\\b";
			}

			_filterRegex = new Regex(pattern, options);
			ClearError();
		}
		catch (Exception exception)
		{
			_errors[nameof(FilterString)] = exception.Message;
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FilterString)));
		}
	}

	#endregion

	#region Events

	public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

	public event EventHandler RefreshFilter;

	#endregion
}