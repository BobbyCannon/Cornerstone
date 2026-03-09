#region References

using System;
using System.Linq.Expressions;
using Cornerstone.Generators.UnitTests.Sample;
using Cornerstone.Storage.Sql;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

public class OrderByExpressionVisitorTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Translate()
	{
		var scenarios = new (Expression<Func<AccountEntity, object>> value, string sql)[]
		{
			(p => p.IsDeleted, "[IsDeleted]"),
			(p => p.LastLoggedIn, "[LastLoggedIn]")
		};

		for (var index = 0; index < scenarios.Length; index++)
		{
			var scenario = scenarios[index];
			var visitor = new OrderByExpressionVisitor();
			var actual = visitor.Translate(scenario.value);
			AreEqual(scenario.sql, actual);
			Console.WriteLine();
		}
	}

	#endregion
}