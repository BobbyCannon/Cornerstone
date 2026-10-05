#nullable enable

#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ProvideValueTargetTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ProvideValueTargetHasCorrectTargetsSet()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var capturedTargets = new CapturedTargets();
		PresentationLocator.CurrentMutable.BindToSelf(capturedTargets);

		CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
        Foreground='{local:CapturingTargetsMarkup}'
        x:CompileBindings='True'>

  <TextBlock Tag='{Binding Source={local:CapturingTargetsMarkup}}'
             Background='{local:CapturingTargetsMarkup}' />

</Window>");

		CornerstoneTest.Collection(capturedTargets.Targets, item =>
		{
			CornerstoneTest.IsType<Window>(item.TargetObject);
			CornerstoneTest.AreEqual(TextElement.ForegroundProperty, item.TargetProperty);
		}, item =>
		{
			CornerstoneTest.IsAssignableFrom<CompiledBindingExtension>(item.TargetObject);
			var prop = CornerstoneTest.IsType<ClrPropertyInfo>(item.TargetProperty);
			CornerstoneTest.AreEqual(nameof(Binding.Source), prop.Name);
		}, item =>
		{
			CornerstoneTest.IsType<TextBlock>(item.TargetObject);
			CornerstoneTest.AreEqual(TextBlock.BackgroundProperty, item.TargetProperty);
		});
	}

	#endregion
}

[TestClass]
public class CapturedTargets
{
	#region Properties

	public List<(object TargetObject, object TargetProperty)> Targets { get; } = [];

	#endregion
}

[TestClass]
public class CapturingTargetsMarkupExtension
{
	#region Methods

	public object ProvideValue(IServiceProvider serviceProvider)
	{
		var parentsProvider = serviceProvider.GetRequiredService<IProvideValueTarget>();
		var capturedTargets = PresentationLocator.Current.GetRequiredService<CapturedTargets>();
		capturedTargets.Targets.Add((parentsProvider.TargetObject, parentsProvider.TargetProperty));
		return Brushes.DarkViolet;
	}

	#endregion
}