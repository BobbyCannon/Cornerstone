#region References

using System;
using System.Linq.Expressions;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
public class OrderByExpressionVisitorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Translate()
	{
		var scenarios = new (Expression<Func<AccountEntity, object>> value, string sql)[]
		{
			(p => p.IsDeleted, "[IsDeleted]"),
			(p => p.LastLoginDate, "[LastLoginDate]"),
			(p => p.Id, "[Id]"),
			(p => p.SyncId, "[SyncId]"),
			(p => p.CreatedOn, "[CreatedOn]"),
			(p => p.ModifiedOn, "[ModifiedOn]"),
			(p => p.Name, "[Name]")
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

	[TestMethod]
	public void TranslateMultiParameterLambdaThrows()
	{
		var visitor = new OrderByExpressionVisitor();
		Expression<Func<AccountEntity, AccountEntity, object>> expr = (a, b) => a.Id;
		ExpectedException<ArgumentException>(() => visitor.Translate(expr));
	}

	[TestMethod]
	public void TranslateNestedMemberThrows()
	{
		var visitor = new OrderByExpressionVisitor();
		Expression<Func<AccountEntity, object>> expr = p => p.Name.Length;
		ExpectedException<NotSupportedException>(() => visitor.Translate(expr));
	}

	#endregion
}