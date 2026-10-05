#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ParentStackProviderTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ParentsAreCorrectForDeferredContent()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var capturedParents = new CapturedParents();
		PresentationLocator.CurrentMutable.BindToSelf(capturedParents);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>

  <Window.Resources>
    <SolidColorBrush x:Key='Brush' Color='{local:CapturingParentsMarkupExtension}' />
  </Window.Resources>

  <TextBlock Foreground='{StaticResource Brush}' />

</Window>");

		window.Show();

		VerifyParents(capturedParents.LazyParents);
		VerifyParents(capturedParents.EagerParents);

		static void VerifyParents(object[]? parents)
		{
			CornerstoneTest.IsNotNull(parents);
			CornerstoneTest.NotEmpty(parents);
			CornerstoneTest.Collection(parents, o => CornerstoneTest.IsType<SolidColorBrush>(o), o => CornerstoneTest.IsType<Window>(o), o => CornerstoneTest.IsType<UnitTestApplication>(o));
		}
	}

	#endregion
}

[TestClass]
public class CapturedParents
{
	#region Properties

	public object[]? EagerParents { get; set; }
	public object[]? LazyParents { get; set; }

	#endregion
}

[TestClass]
public class CapturingParentsMarkupExtension
{
	#region Methods

	public object ProvideValue(IServiceProvider serviceProvider)
	{
		var parentsProvider = serviceProvider.GetRequiredService<ICornerstoneXamlIlParentStackProvider>();
		var eagerParentsProvider = CornerstoneTest.IsAssignableFrom<ICornerstoneXamlIlEagerParentStackProvider>(parentsProvider);

		var capturedParents = PresentationLocator.Current.GetRequiredService<CapturedParents>();
		capturedParents.LazyParents = parentsProvider.Parents.ToArray();
		capturedParents.EagerParents = EnumerateEagerParents(eagerParentsProvider);

		return Colors.Blue;
	}

	private static object[] EnumerateEagerParents(ICornerstoneXamlIlEagerParentStackProvider provider)
	{
		var parents = new List<object>();

		var enumerator = new EagerParentStackEnumerator(provider);
		while (enumerator.TryGetNext() is { } parent)
		{
			parents.Add(parent);
		}

		return parents.ToArray();
	}

	#endregion
}