#region References

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public class NotifyingBase : INotifyPropertyChanged
{
	#region Fields

	private PropertyChangedEventHandler _propertyChanged;

	#endregion

	#region Properties

	public int PropertyChangedSubscriptionCount { get; private set; }

	#endregion

	#region Methods

	public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
	{
		_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}

		field = value;
		RaisePropertyChanged(propertyName);
		return true;
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged
	{
		add
		{
			_propertyChanged += value;
			++PropertyChangedSubscriptionCount;
		}

		remove
		{
			if (_propertyChanged?.GetInvocationList().Contains(value) == true)
			{
				_propertyChanged -= value;
				--PropertyChangedSubscriptionCount;
			}
		}
	}

	#endregion
}