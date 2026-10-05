#region References

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core.Plugins;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core.Plugins;

[InvariantCulture]
[TestClass]
public class DataAnnotationsValidationPluginTests
{
	#region Methods

	[PresentationTestMethod]
	public void ProducesAggregateBindingNotificationsx()
	{
		var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
		var validatorPlugin = new DataAnnotationsValidationPlugin();
		var data = new Data();
		var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.PhoneNumber));
		CornerstoneTest.IsNotNull(accessor);
		var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.PhoneNumber), accessor);
		var result = new List<object>();

		validator.Subscribe(x => result.Add(x));
		validator.SetValue("123456", BindingPriority.LocalValue);
		validator.SetValue("abcdefghijklm", BindingPriority.LocalValue);

		CornerstoneTest.AreEqual(3, result.Count);
		CornerstoneTest.AreEqual(new BindingNotification(null), result[0]);
		CornerstoneTest.AreEqual(new BindingNotification("123456"), result[1]);
		var errorResult = CornerstoneTest.IsAssignableFrom<BindingNotification>(result[2]);
		CornerstoneTest.AreEqual(BindingErrorType.DataValidationError, errorResult.ErrorType);
		CornerstoneTest.AreEqual("abcdefghijklm", errorResult.Value);
		var exceptions = CornerstoneTest.IsAssignableFrom<AggregateException>(errorResult.Error).InnerExceptions;
		CornerstoneTest.IsTrue(exceptions.Any(ex =>
			ex.Message.Contains("The PhoneNumber field is not a valid phone number.")));
		CornerstoneTest.IsTrue(exceptions.Any(ex =>
			ex.Message.Contains("The field PhoneNumber must be a string or array type with a maximum length of '10'.")));
	}

	[PresentationTestMethod]
	public void ProducesRangeBindingNotificationsx()
	{
		var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
		var validatorPlugin = new DataAnnotationsValidationPlugin();
		var data = new Data();
		var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.Between5And10));
		CornerstoneTest.IsNotNull(accessor);
		var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.Between5And10), accessor);
		var result = new List<object>();

		var errmsg = new RangeAttribute(5, 10).FormatErrorMessage(nameof(Data.Between5And10));

		validator.Subscribe(x => result.Add(x));
		validator.SetValue(3, BindingPriority.LocalValue);
		validator.SetValue(7, BindingPriority.LocalValue);
		validator.SetValue(11, BindingPriority.LocalValue);

		CornerstoneTest.AreEqual(new[]
		{
			new BindingNotification(5),
			new BindingNotification(
				new DataValidationException(errmsg),
				BindingErrorType.DataValidationError,
				3),
			new BindingNotification(7),
			new BindingNotification(
				new DataValidationException(errmsg),
				BindingErrorType.DataValidationError,
				11)
		}, result);
	}

	[PresentationTestMethod]
	public void ShouldMatchPropertyWithMultipleValidatorAttributes()
	{
		var target = new DataAnnotationsValidationPlugin();
		var data = new Data();

		CornerstoneTest.IsTrue(target.Match(new WeakReference<object>(data), nameof(Data.PhoneNumber)));
	}

	[PresentationTestMethod]
	public void ShouldMatchPropertyWithValidatorAttribute()
	{
		var target = new DataAnnotationsValidationPlugin();
		var data = new Data();

		CornerstoneTest.IsTrue(target.Match(new WeakReference<object>(data), nameof(Data.Between5And10)));
	}

	[PresentationTestMethod]
	public void ShouldNotMatchPropertyWithoutValidatorAttribute()
	{
		var target = new DataAnnotationsValidationPlugin();
		var data = new Data();

		CornerstoneTest.IsFalse(target.Match(new WeakReference<object>(data), nameof(Data.Unvalidated)));
	}

	#endregion

	#region Classes

	private class Data
	{
		#region Properties

		[Range(5, 10)]
		public int Between5And10 { get; set; } = 5;

		[Phone]
		[MaxLength(10)]
		public string PhoneNumber { get; set; }

		public int Unvalidated { get; set; }

		#endregion
	}

	#endregion
}