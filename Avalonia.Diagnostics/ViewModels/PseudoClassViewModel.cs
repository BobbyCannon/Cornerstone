#region References

using Avalonia.Controls;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class PseudoClassViewModel : ViewModel
{
	#region Fields

	private bool _isActive;
	private bool _isUpdating;
	private readonly IPseudoClasses _pseudoClasses;
	private readonly StyledElement _source;

	#endregion

	#region Constructors

	public PseudoClassViewModel(string name, StyledElement source)
	{
		Name = name;

		_source = source;
		_pseudoClasses = _source.Classes;

		Update();
	}

	#endregion

	#region Properties

	public bool IsActive
	{
		get => _isActive;
		set
		{
			SetProperty(ref _isActive, value);

			if (!_isUpdating)
			{
				_pseudoClasses.Set(Name, value);
			}
		}
	}

	public string Name { get; }

	#endregion

	#region Methods

	public void Update()
	{
		try
		{
			_isUpdating = true;

			IsActive = _source.Classes.Contains(Name);
		}
		finally
		{
			_isUpdating = false;
		}
	}

	#endregion
}