#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core.Plugins;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core.Plugins;

[TestClass]
public class ExceptionValidationPluginTests
{
	#region Methods

	[PresentationTestMethod]
	public void ProducesBindingNotifications()
	{
		var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
		var validatorPlugin = new ExceptionValidationPlugin();
		var data = new Data();
		var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.MustBePositive));
		CornerstoneTest.IsNotNull(accessor);
		var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.MustBePositive), accessor);
		var result = new List<object>();

		validator.Subscribe(x => result.Add(x));
		validator.SetValue(5, BindingPriority.LocalValue);
		validator.SetValue(-2, BindingPriority.LocalValue);
		validator.SetValue(6, BindingPriority.LocalValue);

		CornerstoneTest.AreEqual(new[]
		{
			new BindingNotification(0),
			new BindingNotification(5),
			new BindingNotification(new ArgumentOutOfRangeException("value"), BindingErrorType.DataValidationError),
			new BindingNotification(6)
		}, result);

		GC.KeepAlive(data);
	}

	#endregion

	#region Classes

	public class Data : NotifyingBase
	{
		#region Fields

		private int _mustBePositive;

		#endregion

		#region Properties

		public int MustBePositive
		{
			get => _mustBePositive;
			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				if (value != _mustBePositive)
				{
					_mustBePositive = value;
					RaisePropertyChanged();
				}
			}
		}

		#endregion
	}

	#endregion
}