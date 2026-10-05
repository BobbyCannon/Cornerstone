#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class AssignBindingTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AssignBindingWorksWithAttachedProperty()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var control = (Control) CornerstoneRuntimeXamlLoader.Load(
			"""
			<Control
			    xmlns='https://github.com/BobbyCannon/Cornerstone'
			    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			    xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
			    local:AssignBindingTestControl.AttachedBinding='{Binding SomePath}' />
			""");

		var binding = AssignBindingTestControl.GetAttachedBinding(control);
		CornerstoneTest.IsNotNull(binding);
	}

	[PresentationTestMethod]
	public void AssignBindingWorksWithClrProperty()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var control = (AssignBindingTestControl) CornerstoneRuntimeXamlLoader.Load(
			"""
			<local:AssignBindingTestControl
			    xmlns='https://github.com/BobbyCannon/Cornerstone'
			    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			    xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
			    ClrBinding='{Binding SomePath}' />
			""");

		CornerstoneTest.IsNotNull(control.ClrBinding);
	}

	#endregion
}

[TestClass]
public sealed class AssignBindingTestControl : Control
{
	#region Fields

	public static readonly AttachedProperty<BindingBase?> AttachedBindingProperty =
		PresentationProperty.RegisterAttached<AssignBindingTestControl, Control, BindingBase?>("AttachedBinding");

	#endregion

	#region Properties

	[AssignBinding]
	public BindingBase? ClrBinding { get; set; }

	#endregion

	#region Methods

	[AssignBinding]
	public static BindingBase? GetAttachedBinding(Control obj)
	{
		return obj.GetValue(AttachedBindingProperty);
	}

	public static void SetAttachedBinding(Control obj, BindingBase? value)
	{
		obj.SetValue(AttachedBindingProperty, value);
	}

	#endregion
}