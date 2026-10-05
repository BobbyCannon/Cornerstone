#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class SolidColorBrushTests
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingColorRaisesInvalidated()
	{
		var target = new SolidColorBrush(Colors.Red);
		RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.Color = Colors.Green; });
	}

	#endregion
}