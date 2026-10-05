#region References

using Cornerstone.VisualStudio.Core.Completion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class GoToDefinitionLocationPickerTests
{
	#region Methods

	[TestMethod]
	public void IsGeneratedPathDetectsDesignerAndGeneratedFiles()
	{
		Assert.IsTrue(GoToDefinitionLocationPicker.IsGeneratedPath(@"C:\src\obj\Debug\Button.g.cs"));
		Assert.IsTrue(GoToDefinitionLocationPicker.IsGeneratedPath(@"C:\src\Button.g.cs"));
		Assert.IsTrue(GoToDefinitionLocationPicker.IsGeneratedPath(@"C:\src\Button.g.i.cs"));
		Assert.IsTrue(GoToDefinitionLocationPicker.IsGeneratedPath(@"C:\src\Button.Designer.cs"));
		Assert.IsFalse(GoToDefinitionLocationPicker.IsGeneratedPath(@"C:\src\Controls\Button.cs"));
	}

	[TestMethod]
	public void PickPreferredPathPrefersNonGeneratedOverObj()
	{
		var picked = GoToDefinitionLocationPicker.PickPreferredPath(
			[
				@"C:\src\obj\Debug\net8.0\Button.g.cs",
				@"C:\src\Controls\Button.cs"
			],
			"Button");
		Assert.AreEqual(@"C:\src\Controls\Button.cs", picked);
	}

	[TestMethod]
	public void PickPreferredPathPrefersFileNamedAfterSymbol()
	{
		var picked = GoToDefinitionLocationPicker.PickPreferredPath(
			[
				@"C:\src\Controls\Controls.cs",
				@"C:\src\Controls\Button.cs"
			],
			"Button");
		Assert.AreEqual(@"C:\src\Controls\Button.cs", picked);
	}

	[TestMethod]
	public void PickPreferredPathReturnsFirstWhenOnlyGeneratedExists()
	{
		var generated = @"C:\src\obj\Debug\Button.g.cs";
		var picked = GoToDefinitionLocationPicker.PickPreferredPath([generated], "Button");
		Assert.AreEqual(generated, picked);
	}

	[TestMethod]
	public void PickPreferredPathReturnsNullWhenEmpty()
	{
		Assert.IsNull(GoToDefinitionLocationPicker.PickPreferredPath(null, "Button"));
		Assert.IsNull(GoToDefinitionLocationPicker.PickPreferredPath([], "Button"));
	}

	#endregion
}
