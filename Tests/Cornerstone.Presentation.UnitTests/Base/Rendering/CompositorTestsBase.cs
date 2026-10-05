#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering;

[TestClass]
public class CompositorTestsBase : ScopedTestBase
{
	#region Classes

	protected class CompositorCanvas : CompositorTestServices
	{
		#region Constructors

		public CompositorCanvas()
		{
			TopLevel.Content = Canvas;
			RunJobs();
			Events.Reset();
		}

		#endregion

		#region Properties

		public Canvas Canvas { get; } = new();

		#endregion
	}

	#endregion
}