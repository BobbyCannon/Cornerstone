#nullable enable

#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;

[TestClass]
public class GeometryTypeConverterTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	[TestData(nameof(GetGeometryTypeConverterData))]
	public void GeometryTypeConverterValueWork(object vm, bool nullData)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:c='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;assembly=Cornerstone.Presentation.UnitTests'>
    <Path Name='path' Data='{Binding PathData}' Height='10' Width='10'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var path = window.GetControl<Path>("path");
			window.DataContext = vm;
			CornerstoneTest.AreEqual(nullData, path.Data is null);
		}
	}

	public static IEnumerable<object[]> GetGeometryTypeConverterData()
	{
		yield return new object[] { new StringDataViewModel(), true };
		yield return new object[] { new StringDataViewModel { PathData = "M406.39,333.45l205.93,0" }, false };
		yield return new object[] { new IntDataViewModel(), true };
		yield return new object[] { new IntDataViewModel { PathData = 100 }, true };
	}

	#endregion

	#region Classes

	public class IntDataViewModel
	{
		#region Properties

		public int PathData { get; set; }

		#endregion
	}

	public class StringDataViewModel
	{
		#region Properties

		public string? PathData { get; set; }

		#endregion
	}

	#endregion
}