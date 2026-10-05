#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Logging;

[TestClass]
[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
public class LoggingTests
{
	#region Methods

	[PresentationTestMethod]
	public void ControlShouldLogBindingErrorsWhenNoAncestorWithSuchName()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Base.Logging;assembly=Cornerstone.Presentation.UnitTests'>
    <Panel>
    <Rectangle Fill='{Binding $parent[Grid].Background}'/>
  </Panel>
</Window>";
			var calledTimes = 0;
			using var logSink = TestLogSink.Start((l, a, s, m, d) =>
			{
				if ((l >= LogEventLevel.Warning) && s is Rectangle)
				{
					calledTimes++;
				}
			});
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			CornerstoneTest.AreEqual(1, calledTimes);
		}
	}

	[PresentationTestMethod]
	public void ControlShouldNotLogBindingErrorsWhenDetachedFromVisualTree()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Base.Logging;assembly=Cornerstone.Presentation.UnitTests'>
    <Panel Name='panel'>
    <Rectangle Name='rect' Fill='{Binding $parent[Window].Background}'/>
  </Panel>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var calledTimes = 0;
			using var logSink = TestLogSink.Start((l, a, s, m, d) =>
			{
				if (l >= LogEventLevel.Warning)
				{
					calledTimes++;
				}
			});
			var panel = window.GetControl<Panel>("panel");
			var rect = window.GetControl<Rectangle>("rect");
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			panel.Children.Remove(rect);
			CornerstoneTest.AreEqual(0, calledTimes);
		}
	}

	#endregion
}