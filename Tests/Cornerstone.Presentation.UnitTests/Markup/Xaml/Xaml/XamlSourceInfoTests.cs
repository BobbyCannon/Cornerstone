#nullable enable

#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class XamlSourceInfoTests : XamlTestBase
{
	#region Fields

	private static readonly RuntimeXamlLoaderConfiguration s_configuration = new()
	{
		CreateSourceInfo = true
	};

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AnimationsGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Styles>
		<Style Selector=""Rectangle.red"">
			<Setter Property=""Fill"" Value=""Red""/>
			<Style.Animations>
				<Animation Duration=""0:0:3"">
					<KeyFrame Cue=""0%"">
						<Setter Property=""Opacity"" Value=""0.0""/>
					</KeyFrame>
					<KeyFrame Cue=""100%"">
						<Setter Property=""Opacity"" Value=""1.0""/>
					</KeyFrame>
				</Animation>
			</Style.Animations>
		</Style>
    </UserControl.Styles>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var style = (Style) userControl.Styles[0];
		var animation = (Animation.Animation) style.Animations[0];
		var frame1 = animation.Children[0];
		var frame2 = animation.Children[1];

		var styleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(style);
		CornerstoneTest.IsNotNull(styleSourceInfo);

		var animationSourceInfo = XamlSourceInfo.GetXamlSourceInfo(animation);
		CornerstoneTest.IsNotNull(animationSourceInfo);

		var frameOneSourceInfo = XamlSourceInfo.GetXamlSourceInfo(frame1);
		CornerstoneTest.IsNotNull(frameOneSourceInfo);

		var frameTwoSourceInfo = XamlSourceInfo.GetXamlSourceInfo(frame2);
		CornerstoneTest.IsNotNull(frameTwoSourceInfo);
	}

	[PresentationTestMethod]
	public void DataTemplatesAndDeferredContentsGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
     xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>
	<UserControl.DataTemplates>
		<DataTemplate DataType=""local:SourceInfoTestViewModel"">
			<Border Background=""Red"" CornerRadius=""8"">
				<TextBox Text=""{Binding Name}""/>
			</Border>
		</DataTemplate>
	</UserControl.DataTemplates>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var datatemplate = (DataTemplate) userControl.DataTemplates[0];

		Border border;

		// The template and it's content is deferred as not used (yet)
		if (datatemplate.Content is IDeferredContent deferredContent)
		{
			var templateResult = (ITemplateResult) deferredContent.Build(null)!;
			border = (Border) templateResult.Result!;
		}
		else
		{
			border = (Border) datatemplate.Content!;
		}

		var textBox = (TextBox) border!.Child!;

		var datatemplateSourceInfo = XamlSourceInfo.GetXamlSourceInfo(datatemplate);
		CornerstoneTest.IsNotNull(datatemplateSourceInfo);

		var borderSourceInfo = XamlSourceInfo.GetXamlSourceInfo(border);
		CornerstoneTest.IsNotNull(borderSourceInfo);

		var textBoxSourceInfo = XamlSourceInfo.GetXamlSourceInfo(textBox);
		CornerstoneTest.IsNotNull(textBoxSourceInfo);
	}

	[PresentationTestMethod]
	public void GesturesGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.GestureRecognizers>
		<ScrollGestureRecognizer CanHorizontallyScroll=""True""
								 CanVerticallyScroll=""True""/>
		<PullGestureRecognizer PullDirection=""TopToBottom""/>
	</UserControl.GestureRecognizers>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var scroll = (ScrollGestureRecognizer) userControl.GestureRecognizers.First();
		var pull = (PullGestureRecognizer) userControl.GestureRecognizers.Last();

		var scrollSourceInfo = XamlSourceInfo.GetXamlSourceInfo(scroll);
		CornerstoneTest.IsNotNull(scrollSourceInfo);

		var pullSourceInfo = XamlSourceInfo.GetXamlSourceInfo(pull);
		CornerstoneTest.IsNotNull(pullSourceInfo);
	}

	[PresentationTestMethod]
	public void NestedControlsAllGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <StackPanel>
        <Button />
        <TextBlock />
    </StackPanel>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var stackPanel = (StackPanel) userControl.Content!;
		var button = (Button) stackPanel.Children[0];
		var textblock = (TextBlock) stackPanel.Children[1];

		var userControlSourceInfo = XamlSourceInfo.GetXamlSourceInfo(userControl);
		CornerstoneTest.IsNotNull(userControlSourceInfo);

		var stackPanelSourceInfo = XamlSourceInfo.GetXamlSourceInfo(stackPanel);
		CornerstoneTest.IsNotNull(stackPanelSourceInfo);

		var buttonSourceInfo = XamlSourceInfo.GetXamlSourceInfo(button);
		CornerstoneTest.IsNotNull(buttonSourceInfo);

		var textblockSourceInfo = XamlSourceInfo.GetXamlSourceInfo(textblock);
		CornerstoneTest.IsNotNull(textblockSourceInfo);
	}

	[PresentationTestMethod]
	public void PropertyElementsGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Rectangle Fill=""Blue"" Width=""63"" Height=""41"">
        <Rectangle.OpacityMask>
            <LinearGradientBrush StartPoint=""0%,0%"" EndPoint=""100%,100%"">
                <LinearGradientBrush.GradientStops>
                    <GradientStop Offset=""0"" Color=""Black""/>
                    <GradientStop Offset=""1"" Color=""Transparent""/>
                </LinearGradientBrush.GradientStops>
            </LinearGradientBrush>
        </Rectangle.OpacityMask>
    </Rectangle>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var rect = (Rectangle) userControl.Content!;
		var gradient = (LinearGradientBrush) rect.OpacityMask!;
		var stopOne = (GradientStop) gradient.GradientStops.First();
		var stopTwo = (GradientStop) gradient.GradientStops.Last();

		var rectSourceInfo = XamlSourceInfo.GetXamlSourceInfo(rect);
		CornerstoneTest.IsNotNull(rectSourceInfo);

		var gradientSourceInfo = XamlSourceInfo.GetXamlSourceInfo(gradient);
		CornerstoneTest.IsNotNull(gradientSourceInfo);

		var stopOneSourceInfo = XamlSourceInfo.GetXamlSourceInfo(stopOne);
		CornerstoneTest.IsNotNull(stopOneSourceInfo);

		var stopTwoSourceInfo = XamlSourceInfo.GetXamlSourceInfo(stopTwo);
		CornerstoneTest.IsNotNull(stopTwoSourceInfo);
	}

	[PresentationTestMethod]
	public void ResourceDictionarySetResourceSourceInfo()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Resources>
		<x:String x:Key='text'>foobar</x:String>
		<x:Double x:Key=""A_Double"">123.3</x:Double>
		<x:Int16 x:Key=""An_Int16"">123</x:Int16>
		<x:Int32 x:Key=""An_Int32"">37434323</x:Int32>
		<Thickness x:Key=""PreferredPadding"">10,20,10,0</Thickness>
        <x:Uri x:Key='homepage'>http://avaloniaui.net</x:Uri>
        <SolidColorBrush x:Key='MyBrush' Color='Red'/>
	</UserControl.Resources>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var resources = userControl.Resources;

		var foobarStringSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "text");
		CornerstoneTest.IsNotNull(foobarStringSourceInfo);

		var aDoubleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "A_Double");
		CornerstoneTest.IsNotNull(aDoubleSourceInfo);

		var anInt16SourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "An_Int16");
		CornerstoneTest.IsNotNull(anInt16SourceInfo);

		var anInt32SourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "An_Int32");
		CornerstoneTest.IsNotNull(anInt32SourceInfo);

		var paddingSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "PreferredPadding");
		CornerstoneTest.IsNotNull(paddingSourceInfo);

		var homepageSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "homepage");
		CornerstoneTest.IsNotNull(homepageSourceInfo);

		var myBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "MyBrush");
		CornerstoneTest.IsNotNull(myBrushSourceInfo);
	}

	[PresentationTestMethod]
	public void ResourceDictionarySetResourceSourceInfoWithNestedDictionaries()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Resources>
        <ResourceDictionary>
            <x:String x:Key='text'>foobar</x:String>
            <x:Double x:Key=""A_Double"">123.3</x:Double>
            <x:Int16 x:Key=""An_Int16"">123</x:Int16>
            <x:Int32 x:Key=""An_Int32"">37434323</x:Int32>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <Thickness x:Key=""PreferredPadding"">10,20,10,0</Thickness>
                    <x:Uri x:Key='homepage'>http://avaloniaui.net</x:Uri>
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='MyBrush' Color='Red'/>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
	</UserControl.Resources>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var resources = userControl.Resources;
		var innerResources = (IResourceDictionary) resources.MergedDictionaries[0];
		var themeResources = (IResourceDictionary) resources.ThemeDictionaries[ThemeVariant.Light];

		// Outer define source info
		var foobarStringSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "text");
		CornerstoneTest.IsNotNull(foobarStringSourceInfo);

		var aDoubleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "A_Double");
		CornerstoneTest.IsNotNull(aDoubleSourceInfo);

		var anInt16SourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "An_Int16");
		CornerstoneTest.IsNotNull(anInt16SourceInfo);

		var anInt32SourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "An_Int32");
		CornerstoneTest.IsNotNull(anInt32SourceInfo);

		// Outer one should not have source info for inner resources
		var paddingSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "PreferredPadding");
		CornerstoneTest.IsNull(paddingSourceInfo);

		var homepageSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "homepage");
		CornerstoneTest.IsNull(homepageSourceInfo);

		var myBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "MyBrush");
		CornerstoneTest.IsNull(myBrushSourceInfo);

		// Inner defined source info
		homepageSourceInfo = XamlSourceInfo.GetXamlSourceInfo(innerResources, "homepage");
		CornerstoneTest.IsNotNull(homepageSourceInfo);

		myBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(themeResources, "MyBrush");
		CornerstoneTest.IsNotNull(myBrushSourceInfo);

		// Non-value types should have source info themselves
		var homepage = XamlSourceInfo.GetXamlSourceInfo(innerResources["homepage"]!);
		CornerstoneTest.IsNotNull(homepage);

		var myBrush = XamlSourceInfo.GetXamlSourceInfo(themeResources["MyBrush"]!);
		CornerstoneTest.IsNotNull(myBrush);
	}

	[PresentationTestMethod]
	public void ResourceDictionaryValueTypesDoNotSetXamlSourceInfo()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Resources>
		<x:String x:Key='text'>foobar</x:String>
		<x:Double x:Key=""A_Double"">123.3</x:Double>
		<x:Int16 x:Key=""An_Int16"">123</x:Int16>
		<x:Int32 x:Key=""An_Int32"">37434323</x:Int32>
		<Thickness x:Key=""PreferredPadding"">10,20,10,0</Thickness>
	</UserControl.Resources>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var foobarString = userControl.Resources["text"];
		var aDouble = userControl.Resources["A_Double"];
		var anInt16 = userControl.Resources["An_Int16"];
		var anInt32 = userControl.Resources["An_Int32"];
		var padding = userControl.Resources["PreferredPadding"];

		// Value types shouldn't get source info
		var foobarStringSourceInfo = XamlSourceInfo.GetXamlSourceInfo(foobarString!);
		CornerstoneTest.IsNull(foobarStringSourceInfo);

		var aDoubleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(aDouble!);
		CornerstoneTest.IsNull(aDoubleSourceInfo);

		var anInt16SourceInfo = XamlSourceInfo.GetXamlSourceInfo(anInt16!);
		CornerstoneTest.IsNull(anInt16SourceInfo);

		var anInt32SourceInfo = XamlSourceInfo.GetXamlSourceInfo(anInt32!);
		CornerstoneTest.IsNull(anInt32SourceInfo);

		var paddingSourceInfo = XamlSourceInfo.GetXamlSourceInfo(padding!);
		CornerstoneTest.IsNull(paddingSourceInfo);
	}

	[PresentationTestMethod]
	public void ResourcesGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Resources>
		<ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
		        <ResourceDictionary x:Key='Light'>
		            <SolidColorBrush x:Key='BackgroundBrush' Color='White'/>
		            <SolidColorBrush x:Key='ForegroundBrush' Color='Black'/>
		        </ResourceDictionary>
		        <ResourceDictionary x:Key='Dark'>
		            <SolidColorBrush x:Key='BackgroundBrush' Color='Black'/>
		            <SolidColorBrush x:Key='ForegroundBrush' Color='White'/>
		        </ResourceDictionary>
		    </ResourceDictionary.ThemeDictionaries>

		    <SolidColorBrush x:Key=""Background"" Color=""Yellow"" />
		    <SolidColorBrush x:Key='OtherBrush'>Black</SolidColorBrush>

		</ResourceDictionary>
	</UserControl.Resources>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var backgroundBrush = userControl.Resources["Background"];
		var lightDictionary = (ResourceDictionary) userControl.Resources.ThemeDictionaries[ThemeVariant.Light];
		var darkDictionary = (ResourceDictionary) userControl.Resources.ThemeDictionaries[ThemeVariant.Dark];
		var lightForeground = lightDictionary["ForegroundBrush"];
		var darkBackground = lightDictionary["BackgroundBrush"];
		var otherBrush = userControl.Resources["OtherBrush"];

		var backgroundBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(backgroundBrush!);
		CornerstoneTest.IsNotNull(backgroundBrushSourceInfo);

		var lightDictionarySourceInfo = XamlSourceInfo.GetXamlSourceInfo(lightDictionary!);
		CornerstoneTest.IsNotNull(lightDictionarySourceInfo);

		var darkDictionarySourceInfo = XamlSourceInfo.GetXamlSourceInfo(darkDictionary!);
		CornerstoneTest.IsNotNull(darkDictionarySourceInfo);

		var lightForegroundSourceInfo = XamlSourceInfo.GetXamlSourceInfo(lightForeground!);
		CornerstoneTest.IsNotNull(lightForegroundSourceInfo);

		var darkBackgroundSourceInfo = XamlSourceInfo.GetXamlSourceInfo(darkBackground!);
		CornerstoneTest.IsNotNull(darkBackgroundSourceInfo);

		var otherBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(otherBrush!);
		CornerstoneTest.IsNotNull(otherBrushSourceInfo);
	}

	[PresentationTestMethod]
	public void RootUserControlGetsXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);

		var sourceInfo = XamlSourceInfo.GetXamlSourceInfo(userControl);

		CornerstoneTest.IsNotNull(sourceInfo);
	}

	[PresentationTestMethod]
	[DataRow(@"C:\TestFolder\TestFile.xaml")] // Windows-style path
	[DataRow("/TestFolder/TestFile.xaml")] // Unix-style path
	public void RootUserControlWithBaseUriGetsXamlSourceInfoSourceUriSet(string document)
	{
		var xamlDocument = new RuntimeXamlLoaderDocument(
			"""
			<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
			     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
			</UserControl>
			""")
		{
			Document = document
		};

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xamlDocument, s_configuration);

		var sourceInfo = XamlSourceInfo.GetXamlSourceInfo(userControl);

		CornerstoneTest.IsNotNull(sourceInfo);
		CornerstoneTest.AreEqual("file", sourceInfo.SourceUri!.Scheme);
		CornerstoneTest.IsTrue(sourceInfo.SourceUri!.IsAbsoluteUri);
		CornerstoneTest.AreEqual(new UriBuilder("file", "") { Path = document }.Uri, sourceInfo.SourceUri);
	}

	[PresentationTestMethod]
	public void ShapesGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Canvas Name=""TheCanvas"" Background=""Yellow"" Width=""300"" Height=""400"">
        <Ellipse Fill=""Green"" Width=""58"" Height=""58"" Canvas.Left=""88"" Canvas.Top=""100""/>
        <Path Fill=""Orange"" Canvas.Left=""30"" Canvas.Top=""250""/>
        <Path Fill=""OrangeRed"" Canvas.Left=""180"" Canvas.Top=""250"">
            <Path.Data>
                <PathGeometry>
                    <PathFigure StartPoint=""0,0"" IsClosed=""True"">
                        <QuadraticBezierSegment Point1=""50,0"" Point2=""50,-50"" />
                        <QuadraticBezierSegment Point1=""100,-50"" Point2=""100,0"" />
                        <LineSegment Point=""50,0"" />
                        <LineSegment Point=""50,50"" />
                    </PathFigure>
                </PathGeometry>
            </Path.Data>
        </Path>
        <Line StartPoint=""120,185"" EndPoint=""30,115"" Stroke=""Red"" StrokeThickness=""2""/>
        <Polygon Points=""75,0 120,120 0,45 150,45 30,120"" Stroke=""DarkBlue"" StrokeThickness=""1"" Fill=""Violet"" Canvas.Left=""150"" Canvas.Top=""31""/>
    </Canvas>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var canvas = (Canvas) userControl.Content!;
		var ellipse = (Ellipse) canvas.Children[0];
		var path1 = (Path) canvas.Children[1];
		var path2 = (Path) canvas.Children[2];
		var geometry = (PathGeometry) path2.Data!;
		var figure = geometry.Figures![0];
		var segment1 = figure.Segments![0];
		var segment2 = figure.Segments![1];
		var segment3 = figure.Segments![2];
		var segment4 = figure.Segments![3];
		var line = (Line) canvas.Children[3];
		var polygon = (Polygon) canvas.Children[4];

		var canvasSourceInfo = XamlSourceInfo.GetXamlSourceInfo(canvas);
		CornerstoneTest.IsNotNull(canvasSourceInfo);

		var ellipseSourceInfo = XamlSourceInfo.GetXamlSourceInfo(ellipse);
		CornerstoneTest.IsNotNull(ellipseSourceInfo);

		var path1SourceInfo = XamlSourceInfo.GetXamlSourceInfo(path1);
		CornerstoneTest.IsNotNull(path1SourceInfo);

		var path2SourceInfo = XamlSourceInfo.GetXamlSourceInfo(path2);
		CornerstoneTest.IsNotNull(path2SourceInfo);

		var geometrySourceInfo = XamlSourceInfo.GetXamlSourceInfo(geometry);
		CornerstoneTest.IsNotNull(geometrySourceInfo);

		var figureSourceInfo = XamlSourceInfo.GetXamlSourceInfo(figure);
		CornerstoneTest.IsNotNull(figureSourceInfo);

		var segment1SourceInfo = XamlSourceInfo.GetXamlSourceInfo(segment1);
		CornerstoneTest.IsNotNull(segment1SourceInfo);

		var segment2SourceInfo = XamlSourceInfo.GetXamlSourceInfo(segment2);
		CornerstoneTest.IsNotNull(segment2SourceInfo);

		var segment3SourceInfo = XamlSourceInfo.GetXamlSourceInfo(segment3);
		CornerstoneTest.IsNotNull(segment3SourceInfo);

		var segment4SourceInfo = XamlSourceInfo.GetXamlSourceInfo(segment4);
		CornerstoneTest.IsNotNull(segment4SourceInfo);

		var lineSourceInfo = XamlSourceInfo.GetXamlSourceInfo(line);
		CornerstoneTest.IsNotNull(lineSourceInfo);

		var polygonSourceInfo = XamlSourceInfo.GetXamlSourceInfo(polygon);
		CornerstoneTest.IsNotNull(polygonSourceInfo);
	}

	[PresentationTestMethod]
	public void StylesGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Styles>
		<Style Selector=""Button"">
			<Setter Property=""Margin"" Value=""5"" />
		</Style>
		<ContainerQuery Name=""container""
						Query=""max-width:400"">
			<Style Selector=""Button"">
				<Setter Property=""Background""
						Value=""Red""/>
			</Style>
		</ContainerQuery>
    </UserControl.Styles>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var style = (Style) userControl.Styles[0];
		var query = (ContainerQuery) userControl.Styles[1];

		var styleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(style);
		CornerstoneTest.IsNotNull(styleSourceInfo);

		var querySourceInfo = XamlSourceInfo.GetXamlSourceInfo(query);
		CornerstoneTest.IsNotNull(querySourceInfo);
	}

	[PresentationTestMethod]
	public void TransitionsGetXamlSourceInfoSet()
	{
		var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Transitions>
		<Transitions>
			<DoubleTransition Property=""Width"" Duration=""0:0:1.5""/>
			<DoubleTransition Property=""Height"" Duration=""0:0:1.5""/>
		</Transitions>
	</UserControl.Transitions>
</UserControl>");

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml, s_configuration);
		var width = (DoubleTransition) userControl.Transitions!.First();
		var height = (DoubleTransition) userControl.Transitions!.Last();

		var widthSourceInfo = XamlSourceInfo.GetXamlSourceInfo(width);
		CornerstoneTest.IsNotNull(widthSourceInfo);

		var heightSourceInfo = XamlSourceInfo.GetXamlSourceInfo(height);
		CornerstoneTest.IsNotNull(heightSourceInfo);
	}

	#endregion
}

public class SourceInfoTestViewModel
{
	#region Properties

	public string? Name { get; set; }

	#endregion
}