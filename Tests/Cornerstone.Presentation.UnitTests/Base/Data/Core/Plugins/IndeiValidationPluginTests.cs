#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core.Plugins;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core.Plugins;

[TestClass]
public class IndeiValidationPluginTests
{
	#region Methods

	[PresentationTestMethod]
	public void ProducesBindingNotifications()
	{
		var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
		var validatorPlugin = new IndeiValidationPlugin();
		var data = new Data { Maximum = 5 };
		var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.Value));
		CornerstoneTest.IsNotNull(accessor);
		var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.Value), accessor);
		var result = new List<object>();

		validator.Subscribe(x => result.Add(x));
		validator.SetValue(5, BindingPriority.LocalValue);
		validator.SetValue(6, BindingPriority.LocalValue);
		data.Maximum = 10;
		data.Maximum = 5;

		CornerstoneTest.AreEqual(new[]
		{
			new BindingNotification(0),
			new BindingNotification(5),

			// Value is first signalled without an error as validation hasn't been updated.
			new BindingNotification(6),

			// Then the ErrorsChanged event is fired.
			new BindingNotification(new DataValidationException("Must be less than Maximum"), BindingErrorType.DataValidationError, 6),

			// Maximum is changed to 10 so value is now valid.
			new BindingNotification(6),

			// And Maximum is changed back to 5.
			new BindingNotification(new DataValidationException("Must be less than Maximum"), BindingErrorType.DataValidationError, 6)
		}, result);
	}

	[PresentationTestMethod]
	public void SubscribesAndUnsubscribes()
	{
		var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
		var validatorPlugin = new IndeiValidationPlugin();
		var data = new Data { Maximum = 5 };
		var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.Value));
		CornerstoneTest.IsNotNull(accessor);
		var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.Value), accessor);

		CornerstoneTest.AreEqual(0, data.ErrorsChangedSubscriptionCount);
		validator.Subscribe(_ => { });
		CornerstoneTest.AreEqual(1, data.ErrorsChangedSubscriptionCount);
		validator.Unsubscribe();

		// Forces WeakEvent compact
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		CornerstoneTest.AreEqual(0, data.ErrorsChangedSubscriptionCount);
	}

	#endregion

	#region Classes

	internal class Data : IndeiBase
	{
		#region Fields

		private string _error;
		private int _maximum;
		private int _value;

		#endregion

		#region Properties

		public override bool HasErrors => _error != null;

		public int Maximum
		{
			get => _maximum;
			set
			{
				_maximum = value;
				UpdateError();
			}
		}

		public int Value
		{
			get => _value;
			set
			{
				_value = value;
				RaisePropertyChanged();
				UpdateError();
			}
		}

		#endregion

		#region Methods

		public override IEnumerable GetErrors(string propertyName)
		{
			if ((propertyName == nameof(Value)) && (_error != null))
			{
				return new[] { _error };
			}

			return Array.Empty<string>();
		}

		private void UpdateError()
		{
			if (_value <= _maximum)
			{
				if (_error != null)
				{
					_error = null;
					RaiseErrorsChanged(nameof(Value));
				}
			}
			else
			{
				if (_error == null)
				{
					_error = "Must be less than Maximum";
					RaiseErrorsChanged(nameof(Value));
				}
			}
		}

		#endregion
	}

	#endregion
}