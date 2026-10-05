#region References

using System.Runtime.InteropServices;
using System.Text;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

[TestClass]
public class BiDiClassTests
{
	#region Fields

	private readonly ITestLog _outputHelper;

	#endregion

	#region Constructors

	public BiDiClassTests()
	{
		_outputHelper = new NullTestLog();
	}

	#endregion

	#region Methods

	[ClassDataSource(typeof(BiDiClassTestDataGenerator))]
	[PresentationTestMethod(Skip = "Only run when we update Unicode data.")]
	public void ShouldResolve(
		int lineNumber,
		int[] codePoints,
		sbyte paragraphLevel,
		sbyte resolvedParagraphLevel,
		sbyte[] resolvedLevels,
		int[] resolvedOrder)
	{
		var bidi = new BidiAlgorithm();
		var bidiData = new BidiData { ParagraphEmbeddingLevel = paragraphLevel };

		var text = Encoding.UTF32.GetString(MemoryMarshal.Cast<int, byte>(codePoints).ToArray());

		// Append
		bidiData.Append(text);

		// Act
		for (var i = 0; i < 10; i++)
		{
			bidi.Process(bidiData);
		}

		var resultLevels = bidi.ResolvedLevels;
		var resultParagraphLevel = bidi.ResolvedParagraphEmbeddingLevel;

		CornerstoneTest.AreEqual(resolvedParagraphLevel, resultParagraphLevel);

		for (var i = 0; i < resolvedLevels.Length; i++)
		{
			if (resolvedLevels[i] == -1)
			{
				continue;
			}

			var expectedLevel = resolvedLevels[i];
			var actualLevel = resultLevels[i];

			CornerstoneTest.AreEqual(expectedLevel, actualLevel);
		}
	}

	#endregion
}