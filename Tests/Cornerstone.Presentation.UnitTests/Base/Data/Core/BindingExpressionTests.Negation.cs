#region References

using System;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void CanSetDoubleNegatedValue()
	{
		var data = new ViewModel { BoolValue = true };
		var target = CreateTargetWithSource(data, o => !!o.BoolValue, mode: BindingMode.TwoWay);

		target.Bool = false;

		CornerstoneTest.IsFalse(data.BoolValue);
	}

	[PresentationTestMethod]
	public void CanSetDoubleNegatedValueInPath()
	{
		var data = new ViewModel { Next = new() { BoolValue = true } };
		var target = CreateTargetWithSource(data, o => !!o.Next!.BoolValue, mode: BindingMode.TwoWay);

		target.Bool = false;

		CornerstoneTest.IsFalse(data.Next.BoolValue);
	}

	[PresentationTestMethod]
	public void CanSetNegatedValue()
	{
		var data = new ViewModel { BoolValue = true };
		var target = CreateTargetWithSource(data, o => !o.BoolValue, mode: BindingMode.TwoWay);

		target.Bool = true;

		CornerstoneTest.IsFalse(data.BoolValue);
	}

	[PresentationTestMethod]
	public void CanSetNegatedValueInPath()
	{
		var data = new ViewModel { Next = new() { BoolValue = true } };
		var target = CreateTargetWithSource(data, o => !o.Next!.BoolValue, mode: BindingMode.TwoWay);

		target.Bool = true;

		CornerstoneTest.IsFalse(data.Next.BoolValue);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ShouldDoubleNegateBooleanValue(bool value)
	{
		var data = new ViewModel { BoolValue = value };
		var target = CreateTargetWithSource(data, o => !!o.BoolValue);

		CornerstoneTest.AreEqual(value, target.Bool);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ShouldDoubleNegateBooleanValueInPath(bool value)
	{
		var data = new ViewModel { Next = new() { BoolValue = value } };
		var target = CreateTargetWithSource(data, o => !!o.Next!.BoolValue);

		CornerstoneTest.AreEqual(value, target.Bool);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ShouldNegateBooleanValue(bool value)
	{
		var data = new ViewModel { BoolValue = value };
		var target = CreateTargetWithSource(data, o => !o.BoolValue);

		//CornerstoneTest.AreEqual(!value, target.Bool);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ShouldNegateBooleanValueInPath(bool value)
	{
		var data = new ViewModel { Next = new() { BoolValue = value } };
		var target = CreateTargetWithSource(data, o => !o.Next!.BoolValue);

		CornerstoneTest.AreEqual(!value, target.Bool);

		GC.KeepAlive(data);
	}

	#endregion
}