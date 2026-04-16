#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia;
using Cornerstone.Avalonia.Text;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[SuppressMessage("ReSharper", "CommentTypo")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
[TestClass]
public class CaretTests : CornerstoneUnitTest
{
	#region Methods

	/// <summary>
	/// Ensure the caret can move in all directions with wrap enabled
	/// </summary>
	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void CaretMoveInAllDirectionsWithWrapEnabled()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		var caret = new Caret(viewModel);

		//              012345678901 23456789
		viewModel.Load("abcdefghijk\n12345678");
		viewModel.Lines.Measure(new Size(50, 200), true);
		AreEqual([5, 10], viewModel.Lines[0].WrappedStartOffsets.ToArray());
		AreEqual(new Rect(0,0,50,60), viewModel.Lines[0].VisualLayout);
		AreEqual([17], viewModel.Lines[1].WrappedStartOffsets.ToArray());
		AreEqual(new Rect(0,60,50,40), viewModel.Lines[1].VisualLayout);

		// Virtual Wrapped Line
		// 01234| Index | Size   50w 60h
		// abcde|  0-4  | 0,  0, 50, 20
		// fghij|  5-9  | 0, 20, 50, 20
		// k    | 10-11 | 0, 40, 50, 20
		// 12345| 12-16 | 0, 60, 50, 20
		// 678  | 17-20 | 0, 80, 50, 20

		caret.Move(6);
		AreEqual(6, caret.Offset);
		AreEqual(1, caret.Line.LineNumber);
		AreEqual('g', viewModel.Buffer[caret.Offset]);

		var scenarios = new (Action<Caret> Action, int Offset, char? Character, Rect Visual)[]
		{
			(x => x.MoveRight(), 7, 'h', new Rect(20, 20, 10, 20)),
			(x => x.MoveLineUp(), 2, 'c', new Rect(20, 0, 10, 20)),
			(x => x.MoveRight(), 3, 'd', new Rect(30, 0, 10, 20)),
			(x => x.MoveLineDown(), 8, 'i', new Rect(30, 20, 10, 20)),
			(x => x.MoveLineDown(), 11, '\n', new Rect(10, 40, 10, 20)),
			(x => x.MoveLineDown(), 15, '4', new Rect(30, 60, 10, 20)),
			(x => x.MoveLineDown(), 20, null, new Rect(30, 80, 10, 20)),
			(x => x.MoveLineUp(), 15, '4', new Rect(30, 60, 10, 20)),
			(x => x.MoveLineUp(), 11, '\n', new Rect(10, 40, 10, 20))
		};

		foreach (var scenario in scenarios)
		{
			$"Offset {scenario.Offset}".Dump();
			scenario.Action(caret);
			AreEqual(scenario.Offset, caret.Offset);
			AreEqual(scenario.Visual, caret.VisualLayout);
			if (scenario.Character != null)
			{
				AreEqual(scenario.Character, viewModel.Buffer[caret.Offset]);
			}
		}
	}
	
	/// <summary>
	/// Ensure the caret can move around line endings
	/// </summary>
	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void CaretMoveInAllDirectionsWithWrapEnabledOnLineEndings()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		var caret = new Caret(viewModel);

		//              012345678901 23456789
		viewModel.Load("abcdefghijk\n12345678");
		viewModel.Lines.Measure(new Size(50, 200), true);
		AreEqual([5, 10], viewModel.Lines[0].WrappedStartOffsets.ToArray());
		AreEqual(new Rect(0,0,50,60), viewModel.Lines[0].VisualLayout);
		AreEqual([17], viewModel.Lines[1].WrappedStartOffsets.ToArray());
		AreEqual(new Rect(0,60,50,40), viewModel.Lines[1].VisualLayout);

		// Virtual Wrapped Line
		// 01234| Index | Size   50w 60h
		// -----------------------------
		// abcde|  0-4  | 0,  0, 50, 20
		// fghij|  5-9  | 0, 20, 50, 20
		// k    | 10-11 | 0, 40, 50, 20
		// 12345| 12-16 | 0, 60, 50, 20
		// 678  | 17-20 | 0, 80, 50, 20
		
		// todo: need spaces?
		var scenarios = new (Action<Caret> Action, int Offset, char? Character, bool EndOfLine, Rect Visual)[]
		{
			(x => x.MoveToLineEnd(), 5, 'f', true, new Rect(50, 0, 10, 20)),
			(x => x.MoveLineDown(), 10, 'k', true, new Rect(50, 20, 10, 20)),
			(x => x.MoveLineDown(), 11, '\n', true, new Rect(10, 40, 10, 20)),
			(x => x.MoveLineDown(), 17, '6', true, new Rect(50, 60, 10, 20)),
			(x => x.MoveLineDown(), 20, null, true, new Rect(30, 80, 10, 20)),
			(x => x.MoveLineDown(), 20, null, true, new Rect(30, 80, 10, 20)),
			(x => x.MoveLineUp(), 17, '6', true, new Rect(50, 60, 10, 20)),
			(x => x.MoveLineUp(), 11, '\n', true, new Rect(10, 40, 10, 20)),
			(x => x.MoveLineUp(), 10, 'k', true, new Rect(50, 20, 10, 20)),
			(x => x.MoveLineUp(), 5, 'f', true, new Rect(50, 0, 10, 20)),
			(x => x.MoveLineUp(), 5, 'f', true, new Rect(50, 0, 10, 20)),
		};

		foreach (var scenario in scenarios)
		{
			$"Offset {scenario.Offset}".Dump();
			scenario.Action(caret);
			AreEqual(scenario.Offset, caret.Offset);
			AreEqual(scenario.Visual, caret.VisualLayout);
			AreEqual(scenario.EndOfLine, caret.IsAtEndOfLine);
			if (scenario.Character != null)
			{
				AreEqual(scenario.Character, viewModel.Buffer[caret.Offset]);
			}
		}
	}
	
	/// <summary>
	/// Ensure the caret can move in all directions with wrap enabled
	/// </summary>
	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void CaretMoveInAllDirectionsWithWrapEnabledAndWordBreaking()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		var caret = new Caret(viewModel);

		//              01234567890123456789 0123456789012345678
		viewModel.Load("Hello World Foo Bar\nFoo Bar Hello World");
		viewModel.Lines.Measure(new Size(80, 200), true);
		AreEqual([6, 12], viewModel.Lines[0].WrappedStartOffsets.ToArray());
		AreEqual(new Rect(0,0,60,60), viewModel.Lines[0].VisualLayout);
		AreEqual([28,34], viewModel.Lines[1].WrappedStartOffsets.ToArray());
		AreEqual(new Rect(0,60,80,60), viewModel.Lines[1].VisualLayout);

		// Virtual Wrapped Line
		// 01234567 | Index | Size   50w 60h
		// Hello    |  0-5  | 
		// World    |  6-11 |
		// Foo Bar  | 12-19 |
		// Foo Bar  | 20-27 |
		// Hello    | 28-33 |
		// World    | 34-39 |

		caret.Move(18);
		AreEqual(18, caret.Offset);
		AreEqual(1, caret.Line.LineNumber);
		AreEqual('r', viewModel.Buffer[caret.Offset]);
		AreEqual(new Rect(60, 40, 10, 20), caret.VisualLayout);

		var scenarios = new (Action<Caret> Action, int Offset, char? Character, Rect Visual)[]
		{
			(x => x.MoveLineUp(), 11, null, new Rect(50, 20, 10, 20)),
		};

		foreach (var scenario in scenarios)
		{
			$"Offset {scenario.Offset}".Dump();
			scenario.Action(caret);
			AreEqual(scenario.Offset, caret.Offset);
			AreEqual(scenario.Visual, caret.VisualLayout);
			if (scenario.Character != null)
			{
				AreEqual(scenario.Character, viewModel.Buffer[caret.Offset]);
			}
		}
	}
	
	[TestMethod]
	public void CaretMoveShouldSetVisualLayout()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		var caret = new Caret(viewModel);

		//             01234567890
		viewModel.Load("Hello World");
		viewModel.Lines.Measure(new Size(50, 200), true);
		AreEqual(new Rect(0, 0, 50, 60), viewModel.Lines[0].VisualLayout);
		AreEqual([5, 10], viewModel.Lines[0].WrappedStartOffsets.ToArray());

		// Virtual Wrapped Line
		// 01234| Index | Size   50w 60h
		// Hello|  0-4  | 0,  0, 50, 20
		//  Worl|  5-9  | 0, 20, 50, 20
		// d    | 10    | 0, 40, 50, 20

		var scenarios = new (int Index, Rect Expected)[]
		{
			(0, new Rect(0, 0, 10, 20)),
			(1, new Rect(10, 0, 10, 20)),
			(4, new Rect(40, 0, 10, 20)),
			(5, new Rect(0, 20, 10, 20)),
			(8, new Rect(30, 20, 10, 20)),
			(9, new Rect(40, 20, 10, 20)),
			(11, new Rect(10, 40, 10, 20))
		};

		foreach (var scenario in scenarios)
		{
			$"Caret: {scenario.Index}".Dump();
			caret.Move(scenario.Index);
			AreEqual(scenario.Index, caret.Offset);
			AreEqual(scenario.Expected, caret.VisualLayout);
		}
	}

	#endregion
}