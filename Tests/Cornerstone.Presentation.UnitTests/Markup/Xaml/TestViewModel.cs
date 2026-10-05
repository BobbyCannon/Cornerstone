#nullable enable

#region References

using Cornerstone.Presentation.UnitTests.Helpers;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml;

public class TestViewModel : NotifyingBase
{
	#region Fields

	private bool _boolean;
	private TestViewModel? _child;
	private int _integer;
	private string? _string;

	#endregion

	#region Properties

	public bool Boolean
	{
		get => _boolean;
		set
		{
			_boolean = value;
			RaisePropertyChanged();
		}
	}

	public TestViewModel? Child
	{
		get => _child;
		set
		{
			_child = value;
			RaisePropertyChanged();
		}
	}

	public int Integer
	{
		get => _integer;
		set
		{
			_integer = value;
			RaisePropertyChanged();
		}
	}

	public string? String
	{
		get => _string;
		set
		{
			_string = value;
			RaisePropertyChanged();
		}
	}

	#endregion
}