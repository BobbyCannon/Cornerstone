#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsSelf : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingToPropertyOnSelfShouldWork()
	{
		var target = new TextBlock
		{
			Tag = "Hello World!",
			[!TextBlock.TextProperty] = new Binding("Tag")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.Self)
			}
		};

		CornerstoneTest.AreEqual("Hello World!", target.Text);
	}

	[PresentationTestMethod]
	public void TwoWayBindingToPropertyOnSelfShouldWork()
	{
		var target = new TextBlock
		{
			Tag = "Hello World!",
			[!TextBlock.TextProperty] = new Binding("Tag")
			{
				Mode = BindingMode.TwoWay,
				RelativeSource = new RelativeSource(RelativeSourceMode.Self)
			}
		};

		CornerstoneTest.AreEqual("Hello World!", target.Text);
		target.Text = "Goodbye cruel world :(";
		CornerstoneTest.AreEqual("Goodbye cruel world :(", target.Text);
	}

	#endregion
}