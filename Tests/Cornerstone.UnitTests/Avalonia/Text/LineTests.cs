#region References

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Cornerstone.Avalonia.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[TestClass]
public class LineTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void GetNearestOffsetAtVisual()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };

		//             012345678901
		viewModel.Load("Hello World");
		viewModel.Lines.Measure(new Size(50, 200), true);

		// Virtual Wrapped Line
		// 01234| Index | Size   50w 60h
		// Hello|  0-4  | 0,  0, 50, 20
		//  Worl|  5-9  | 0, 20, 50, 20
		// d    | 10-11 | 0, 40, 50, 20
		AreEqual(1, viewModel.Lines.Count);
		AreEqual(new Rect(0, 0, 50, 60), viewModel.Lines[0].VisualLayout);

		AreEqual(5, viewModel.Lines[0].GetNearestOffsetAtVisual(45, 0, false));
		AreEqual(5, viewModel.Lines[0].GetNearestOffsetAtVisual(45, 19, false));
		AreEqual(10, viewModel.Lines[0].GetNearestOffsetAtVisual(45, 20, false));
		AreEqual(11, viewModel.Lines[0].GetNearestOffsetAtVisual(45, 40, false));
		
		AreEqual(0, viewModel.Lines[0].GetNearestOffsetAtVisual(0, 0, false));
		AreEqual(6, viewModel.Lines[0].GetNearestOffsetAtVisual(10, 20, false));
	}
	
	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void GetNearestOffsetAtVisualWithoutWrap()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };

		//             01234567890
		viewModel.Load("Hello World");
		viewModel.Lines.Measure(new Size(50, 200), false);

		// Virtual Wrapped Line
		// 01234567890
		// Hello World
		AreEqual(1, viewModel.Lines.Count);
		AreEqual(new Rect(0, 0, 110, 20), viewModel.Lines[0].VisualLayout);

		AreEqual(0, viewModel.Lines[0].GetNearestOffsetAtVisual(0, 0, false));
		AreEqual(5, viewModel.Lines[0].GetNearestOffsetAtVisual(50, 20, false));

		// Should still work even if the Y is way out of index
		AreEqual(5, viewModel.Lines[0].GetNearestOffsetAtVisual(50, 40, false));
	}

	#endregion
}