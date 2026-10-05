#region References

using Cornerstone.Presentation.Controls.Utils;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Utils;

[TestClass]
public class BindingEvaluatorTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClearDataContextSetsDataContextToNull()
	{
		var evaluator = new BindingEvaluator<string>();
		evaluator.Evaluate("foo");
		CornerstoneTest.AreEqual("foo", evaluator.DataContext);

		evaluator.ClearDataContext();
		CornerstoneTest.IsNull(evaluator.DataContext);
	}

	#endregion
}