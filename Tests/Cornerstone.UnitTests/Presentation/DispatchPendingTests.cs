#region References

using Cornerstone.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class DispatchPendingTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ClearIsIdempotent()
	{
		var pending = new DispatchPending();
		pending.MarkPending();
		pending.ClearHasPending();
		pending.ClearHasPending();
		IsFalse(pending.HasPending);
	}

	[TestMethod]
	public void StartsClearThenMarkAndClear()
	{
		var pending = new DispatchPending();
		IsFalse(pending.HasPending);

		pending.MarkPending();
		IsTrue(pending.HasPending);

		pending.ClearHasPending();
		IsFalse(pending.HasPending);
	}

	#endregion
}
