#region References

using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

[TestClass]
public class BiDiAlgorithmTests
{
	#region Fields

	private readonly ITestLog _outputHelper;

	#endregion

	#region Constructors

	public BiDiAlgorithmTests()
	{
		_outputHelper = new NullTestLog();
	}

	#endregion

	#region Methods

	[ClassDataSource(typeof(BiDiTestDataGenerator))]
	[PresentationTestMethod(Skip = "Only run when we update Unicode data.")]
	public void ShouldProcess(int lineNumber, BidiClass[] classes, sbyte paragraphEmbeddingLevel, int[] levels)
	{
		var bidi = new BidiAlgorithm();

		// Run the algorithm...
		ArraySlice<sbyte> resultLevels;

		bidi.Process(
			classes,
			ArraySlice<BidiPairedBracketType>.Empty,
			ArraySlice<int>.Empty,
			paragraphEmbeddingLevel,
			false,
			null,
			null,
			null);

		resultLevels = bidi.ResolvedLevels;

		// Check the results match
		var pass = true;

		if (resultLevels.Length == levels.Length)
		{
			for (var i = 0; i < levels.Length; i++)
			{
				if (levels[i] == -1)
				{
					continue;
				}

				if (resultLevels[i] != levels[i])
				{
					pass = false;
					break;
				}
			}
		}
		else
		{
			pass = false;
		}

		if (!pass)
		{
			_outputHelper.WriteLine($"Failed line {lineNumber}");
			_outputHelper.WriteLine($"        Data: {string.Join(" ", classes)}");
			_outputHelper.WriteLine($" Embed Level: {paragraphEmbeddingLevel}");
			_outputHelper.WriteLine($"    Expected: {string.Join(" ", levels)}");
			_outputHelper.WriteLine($"      Actual: {string.Join(" ", resultLevels)}");
		}

		CornerstoneTest.IsTrue(pass);
	}

	#endregion
}