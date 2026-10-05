#region References

using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class RectangleGeometryTests
{
	#region Methods

	[PresentationTestMethod]
	public void RectangleWithTransformCanBeChanged()
	{
		using (UnitTestApplication.Start(GetServices()))
		{
			var target = new RectangleGeometry
			{
				Rect = new Rect(0, 0, 100, 100),
				Transform = new RotateTransform(45)
			};

			target.Rect = new Rect(50, 50, 150, 150);
		}
	}

	private static TestServices GetServices()
	{
		var context = new StubStreamGeometryContextImpl();
		var streamGeometry = new StubStreamGeometryImpl();
		streamGeometry.OpenResult = context;
		var renderInterface = new HeadlessPlatformRenderInterface();
		return new TestServices(renderInterface: renderInterface);
	}

	#endregion
}