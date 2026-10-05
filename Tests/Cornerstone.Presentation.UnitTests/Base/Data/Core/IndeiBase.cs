#region References

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.UnitTests.Helpers;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

internal abstract class IndeiBase : NotifyingBase, INotifyDataErrorInfo
{
	#region Fields

	private EventHandler<DataErrorsChangedEventArgs> _errorsChanged;

	#endregion

	#region Properties

	public int ErrorsChangedSubscriptionCount { get; private set; }

	public abstract bool HasErrors { get; }

	#endregion

	#region Methods

	public abstract IEnumerable GetErrors(string propertyName);

	protected void RaiseErrorsChanged([CallerMemberName] string propertyName = "")
	{
		_errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
	}

	#endregion

	#region Events

	public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged
	{
		add
		{
			_errorsChanged += value;
			++ErrorsChangedSubscriptionCount;
		}
		remove
		{
			_errorsChanged -= value;
			--ErrorsChangedSubscriptionCount;
		}
	}

	#endregion
}