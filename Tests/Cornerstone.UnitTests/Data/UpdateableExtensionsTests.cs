#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class UpdateableExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void IsSyncAction()
	{
		var actions = EnumExtensions.GetFlagValues<UpdateableAction>();
		var scenarios = new Dictionary<UpdateableAction, bool>
		{
			{ UpdateableAction.SyncIncomingAdd, true },
			{ UpdateableAction.SyncIncomingUpdate, true },
			{ UpdateableAction.SyncOutgoing, true },
			{ UpdateableAction.UnwrapProxyEntity, false },
			{ UpdateableAction.Updateable, false },
			{ UpdateableAction.PropertyChangeTracking, false },
			{ UpdateableAction.PartialUpdate, false }
		};

		var missing = scenarios.Keys.Except(actions);
		if (missing.Any())
		{
			CopyToClipboard(scenarios.ToCSharp().Dump());
			Fail("Missing scenarios...");
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Value, scenario.Key.IsSyncAction());
		}

		// Adding flags outside sync should fail
		IsFalse((UpdateableAction.SyncIncomingAdd | UpdateableAction.PartialUpdate).IsSyncAction());
	}

	#endregion
}