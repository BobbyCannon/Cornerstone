#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class DesignTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldApplyDesignModePropertiesFromControlToWindow()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		// Use-case: User previews a control, which is wrapped by the window.
		var window = new Window();
		var control = new ContentControl();
		window.Content = control;

		Design.SetWidth(control, 200);
		Design.SetHeight(control, 150);
		Design.SetDataContext(control, "TestDataContext");
		Design.SetDesignStyle(control,
			new Style(x => x.OfType<ContentControl>())
			{
				Setters = { new Setter(TemplatedControl.BackgroundProperty, Brushes.Yellow) }
			});

		Design.ApplyDesignModeProperties(window, control);

		CornerstoneTest.AreEqual(200, window.Width);
		CornerstoneTest.AreEqual(150, window.Height);
		CornerstoneTest.AreEqual("TestDataContext", window.DataContext);
		CornerstoneTest.Contains(window.Styles, s => ((Style) s).Setters.OfType<Setter>().First().Property == TemplatedControl.BackgroundProperty);
	}

	[PresentationTestMethod]
	public void ShouldNotThrowExceptionOnApplication()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var app = new Application();
		var preview = Design.CreatePreviewWithControl(app);

		CornerstoneTest.IsNotNull(preview);
	}

	[PresentationTestMethod]
	public void ShouldNotThrowExceptionOnGenericDataTemplate()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var preview = Design.CreatePreviewWithControl(new FuncDataTemplate<string>((data, _) =>
			new TextBlock { Text = data }));

		CornerstoneTest.IsNotNull(preview);
	}

	[PresentationTestMethod]
	public void ShouldNotThrowExceptionOnGenericResourceDictionary()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var preview = Design.CreatePreviewWithControl(new ResourceDictionary());

		CornerstoneTest.IsNotNull(preview);
	}

	[PresentationTestMethod]
	public void ShouldNotThrowExceptionOnGenericStyle()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var preview = Design.CreatePreviewWithControl(new Style(x => x.OfType<Button>()));

		// We are not going to test specific content of the placeholder preview control.
		// But it should not throw and should not return null at least.
		CornerstoneTest.IsNotNull(preview);
	}

	[PresentationTestMethod]
	public void ShouldPreviewControlWithAnotherControl()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var control = new TextBlock();
		Design.SetPreviewWith(control,
			new FuncTemplate<Control>(static () => new Border()));

		var preview = Design.CreatePreviewWithControl(control);

		CornerstoneTest.IsType<Border>(preview);
	}

	[PresentationTestMethod]
	public void ShouldPreviewDataTemplateWithContentControl()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		const string testData = "Test Data";
		var dataTemplate = new FuncDataTemplate<string>((data, _) =>
			new TextBlock { Text = data });
		Design.SetPreviewWith(dataTemplate,
			new FuncTemplate<Control>(static () => new ContentControl { Content = testData }));

		var preview = Design.CreatePreviewWithControl(dataTemplate);

		var previewContentControl = CornerstoneTest.IsType<ContentControl>(preview);
		CornerstoneTest.AreEqual(testData, previewContentControl.Content);
		CornerstoneTest.Same(dataTemplate, previewContentControl.ContentTemplate);
	}

	[PresentationTestMethod]
	public void ShouldPreviewDataTemplateWithDataContext()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		const string testData = "Test Data";
		var dataTemplate = new FuncDataTemplate<string>((data, _) =>
			new TextBlock { Text = data });
		Design.SetDataContext(dataTemplate, testData);

		var preview = Design.CreatePreviewWithControl(dataTemplate);

		var previewContentControl = CornerstoneTest.IsType<ContentControl>(preview);
		CornerstoneTest.AreEqual(testData, previewContentControl.Content);
		CornerstoneTest.Same(dataTemplate, previewContentControl.ContentTemplate);
	}

	[PresentationTestMethod]
	public void ShouldPreviewResourceDictionaryWithTemplate()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var dictionary = new ResourceDictionary { ["TestColor"] = Colors.Green };
		Design.SetPreviewWith(dictionary,
			new FuncTemplate<Control>(static () =>
				new Border { [!Border.BackgroundProperty] = new DynamicResourceExtension("TestColor") }));

		var preview = Design.CreatePreviewWithControl(dictionary);

		var border = CornerstoneTest.IsType<Border>(preview);
		CornerstoneTest.AreEqual(Colors.Green, ((ISolidColorBrush) border.Background!).Color);
	}

	#endregion
}