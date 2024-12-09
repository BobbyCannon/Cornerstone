#region References

using System.Linq;
using Cornerstone.Parsers;
using Cornerstone.Profiling;
using Cornerstone.Testing;
using Cornerstone.Text;

#endregion

namespace Cornerstone.UnitTests.Parsers;

public abstract class TokenizerTest : CornerstoneUnitTest
{
	#region Methods

	protected void CreateExpectedTokens<T, T2, T3>(string folder, string fileName, bool enableTestScenarioCreation)
		where T : Tokenizer<T2,T3>, new() 
		where T2 : TokenData<T2, T3>, new()
	{
		if (!enableTestScenarioCreation)
		{
			"Not generating scenarios due to not being enabled".Dump();
			return;
		}

		var filePath = $@"{UnitTestsDirectory}\Parsers\{folder}\{fileName}";
		var builder = new TextBuilder();
		var t = new T();
		t.Add(GetContentToTokenize());

		var timer = Timer.StartNewTimer();
		var actual = t.GetTokens().ToArray();
		timer.Elapsed.Dump();
		actual.DumpCSharpArray(builder);

		UpdateFileIfNecessary(filePath, builder);
	}

	protected abstract string GetContentToTokenize();

	#endregion
}