#nullable enable

#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class PresentationIntrinsicsTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AllIntrinsicsAreParsedAndSet()
	{
		var xaml = @"<local:TestIntrinsicsControl 
            xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
            TimeSpanProperty='00:10:10'
            ThicknessProperty='1 1 1 1'
            PointProperty='15, 15'
            VectorProperty='16.6, 16.6'
            SizeProperty='20, 20'
            MatrixProperty='1 0 0 1 0 0'
            CornerRadiusProperty='4'
            ColorProperty='#44ff11'
            RelativePointProperty='50%, 50%'
            GridLengthProperty='10*'
            IBrushProperty='#44ff11'
            TextTrimmingProperty='CharacterEllipsis'
            TextDecorationCollectionProperty='Strikethrough'
            WindowTransparencyLevelProperty='AcrylicBlur'
            UriProperty='https://avaloniaui.net/'
            ThemeVariantProperty='Dark'
            PointsProperty='1, 1, 2, 2' />";

		var target = CornerstoneRuntimeXamlLoader.Parse<TestIntrinsicsControl>(xaml);

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.AreEqual(new TimeSpan(0, 10, 10), target.TimeSpanProperty);
		CornerstoneTest.AreEqual(new Thickness(1), target.ThicknessProperty);
		CornerstoneTest.AreEqual(new Thickness(1), target.ThicknessProperty);
		CornerstoneTest.AreEqual(new Point(15, 15), target.PointProperty);
		CornerstoneTest.AreEqual(new Vector(16.6, 16.6), target.VectorProperty);
		CornerstoneTest.AreEqual(new Size(20, 20), target.SizeProperty);
		CornerstoneTest.AreEqual(new Matrix(1, 0, 0, 1, 0, 0), target.MatrixProperty);
		CornerstoneTest.AreEqual(new CornerRadius(4), target.CornerRadiusProperty);
		CornerstoneTest.AreEqual(Color.Parse("#44ff11"), target.ColorProperty);
		CornerstoneTest.AreEqual(new RelativePoint(0.5, 0.5, RelativeUnit.Relative), target.RelativePointProperty);
		CornerstoneTest.AreEqual(new GridLength(10, GridUnitType.Star), target.GridLengthProperty);
		CornerstoneTest.AreEqual(new ImmutableSolidColorBrush(Color.Parse("#44ff11")), target.IBrushProperty);
		CornerstoneTest.AreEqual(TextTrimming.CharacterEllipsis, target.TextTrimmingProperty);
		CornerstoneTest.AreEqual(TextDecorations.Strikethrough, target.TextDecorationCollectionProperty);
		CornerstoneTest.AreEqual(WindowTransparencyLevel.AcrylicBlur, target.WindowTransparencyLevelProperty);
		CornerstoneTest.AreEqual(new Uri("https://avaloniaui.net/"), target.UriProperty);
		CornerstoneTest.AreEqual(ThemeVariant.Dark, target.ThemeVariantProperty);
		CornerstoneTest.AreEqual(new[] { new Point(1, 1), new Point(2, 2) }, target.PointsProperty);
	}

	[PresentationTestMethod]
	public void AllIntrinsicsReportErrorsIfFailed()
	{
		var xaml = @"<local:TestIntrinsicsControl 
            xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
            TimeSpanProperty='00:00:10,1'
            ThicknessProperty='1 1 1'
            PointProperty='15% 15%'
            VectorProperty='16.6. 16.6'
            SizeProperty='20%, 20%'
            MatrixProperty='1 0 1 0 0'
            CornerRadiusProperty='4 1 4'
            ColorProperty='#44ff1'
            RelativePointProperty='50, 50%'
            GridLengthProperty='10%'
            PointsProperty='1, 1, 2' />";

		// TODO: double check why we don't throw error on other supported types. Should it be warnings?

		var diagnostics = new List<RuntimeXamlDiagnostic>();
		Assert.Throws<AggregateException>(() => CornerstoneRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml),
			new RuntimeXamlLoaderConfiguration
			{
				DiagnosticHandler = diagnostic =>
				{
					diagnostics.Add(diagnostic);
					return diagnostic.Severity;
				}
			}));

		CornerstoneTest.Collection(diagnostics, d => AssertDiagnostic(d, "time span"), d => AssertDiagnostic(d, "thickness"), d => AssertDiagnostic(d, "point"), d => AssertDiagnostic(d, "vector"), d => AssertDiagnostic(d, "size"), d => AssertDiagnostic(d, "matrix"), d => AssertDiagnostic(d, "corner radius"), d => AssertDiagnostic(d, "color"), d => AssertDiagnostic(d, "relative point"), d => AssertDiagnostic(d, "grid length"), d => AssertDiagnostic(d, "points list"), // Compiler attempts to parse PointsList twice - as a list and as a point.
			d => AssertDiagnostic(d, "point"));

		void AssertDiagnostic(RuntimeXamlDiagnostic runtimeXamlDiagnostic, string contains)
		{
			CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Error, runtimeXamlDiagnostic.Severity);
			CornerstoneTest.Contains(runtimeXamlDiagnostic.Title, contains, StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>
	/// GitHub Issue <see href="https://github.com/AvaloniaUI/Avalonia/issues/15320"> #15320 </see>
	/// </summary>
	[PresentationTestMethod]
	public void ShouldParseFormattedColorTag()
	{
		var target = CornerstoneRuntimeXamlLoader
			.Parse<ResourceDictionary>("""
										<ResourceDictionary xmlns="https://github.com/BobbyCannon/Cornerstone"
										                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
										    <Color x:Key="ColorKey">
										        White
										    </Color>
										</ResourceDictionary>
										""");
		CornerstoneTest.IsNotNull(target);
	}

	#endregion
}

[TestClass]
public class TestIntrinsicsControl : Control
{
	#region Properties

	public Color ColorProperty { get; set; }
	public CornerRadius CornerRadiusProperty { get; set; }
	public GridLength GridLengthProperty { get; set; }
	public IBrush? IBrushProperty { get; set; }
	public Matrix MatrixProperty { get; set; }
	public Point PointProperty { get; set; }
	public Points? PointsProperty { get; set; }
	public RelativePoint RelativePointProperty { get; set; }
	public Size SizeProperty { get; set; }
	public TextDecorationCollection? TextDecorationCollectionProperty { get; set; }
	public TextTrimming? TextTrimmingProperty { get; set; }
	public ThemeVariant? ThemeVariantProperty { get; set; }

	// public FontFamily FontFamilyProperty { get; set; }
	public Thickness ThicknessProperty { get; set; }
	public TimeSpan TimeSpanProperty { get; set; }
	public Uri? UriProperty { get; set; }
	public Vector VectorProperty { get; set; }
	public WindowTransparencyLevel WindowTransparencyLevelProperty { get; set; }

	#endregion
}