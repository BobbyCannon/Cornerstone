#region References

using System;
using System.Linq.Expressions;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Sync;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class ExpressionExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void TryGetPropertyName()
	{
		Expression<Func<Account, object>> test = x => x.IsDeleted;
		IsTrue(test.TryGetPropertyName(out var actual));
	}

	#endregion
}