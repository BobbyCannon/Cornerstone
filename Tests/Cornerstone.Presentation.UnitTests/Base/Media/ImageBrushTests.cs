#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class ImageBrushTests
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingSourceRaisesInvalidated()
	{
		var bitmap1 = new StubImageBrushSource();
		var bitmap2 = new StubImageBrushSource();
		var target = new ImageBrush(bitmap1);

		RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.Source = bitmap2; });
	}

	#endregion
}