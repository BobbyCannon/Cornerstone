#region References

using System;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Generators.UnitTests.Sample;
using Cornerstone.Storage.Sql;
using Cornerstone.Testing;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

public class PredicateToSqlVisitorTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Translate()
	{
		var scenarios = new (Expression<Func<AccountEntity, bool>> value, string sql, object[] parameters)[]
		{
			(p => !p.IsDeleted, "([IsDeleted] = 0)", []),
			(p => p.IsDeleted, "([IsDeleted] = 1)", []),
			(p => p.LastLoggedIn <= StartDateTime, "([LastLoggedIn] <= @p0)", [StartDateTime]),
			(p => p.LastLoggedIn < StartDateTime, "([LastLoggedIn] < @p0)", [StartDateTime]),
			(p => p.LastLoggedIn >= StartDateTime, "([LastLoggedIn] >= @p0)", [StartDateTime]),
			(p => p.LastLoggedIn > StartDateTime, "([LastLoggedIn] > @p0)", [StartDateTime]),
			(p => p.LastLoggedIn == null, "([LastLoggedIn] IS NULL)", []),
			(p => p.Name == null, "([Name] IS NULL)", []),
			(p => p.Name.Contains("Bobby") && ((p.Id > 10) || p.IsDeleted), "(([Name] LIKE @p0) AND (([Id] > @p1) OR ([IsDeleted] = 1)))", ["%Bobby%", 10]),
			(p => p.Name.StartsWith("Bobby") && ((p.Id < 10) || !p.IsDeleted), "(([Name] LIKE @p0) AND (([Id] < @p1) OR ([IsDeleted] = 0)))", ["Bobby%", 10]),
			(p => p.Name.EndsWith("Bobby"), "([Name] LIKE @p0)", ["%Bobby"]),
			(p => p.Id == StartDateTime.Ticks, "([Id] = @p0)", [StartDateTime.Ticks]),
			(p => p.Id == int.MaxValue, "([Id] = @p0)", [int.MaxValue]),
			(p => p.Id == int.MinValue, "([Id] = @p0)", [int.MinValue]),
			(p => p.CreatedOn > new DateTime(2025, 02, 05), "([CreatedOn] > @p0)", [new DateTime(2025, 02, 05)]),
			(p => p.CreatedOn > DateTime.MaxValue, "([CreatedOn] > @p0)", [DateTime.MaxValue]),
			(p => p.CreatedOn > DateTime.MinValue, "([CreatedOn] > @p0)", [DateTime.MinValue]),
			(p => p.CreatedOn > DateTime.UnixEpoch, "([CreatedOn] > @p0)", [DateTime.UnixEpoch]),
			(p => p.CreatedOn > StartDateTime, "([CreatedOn] > @p0)", [StartDateTime])
		};

		for (var index = 0; index < scenarios.Length; index++)
		{
			var scenario = scenarios[index];
			var visitor = new PredicateToSqlVisitor();
			var actual = visitor.Translate(scenario.value);
			actual.Sql.Dump();
			if (actual.Parameters.Length > 0)
			{
				string.Join(", ", actual.Parameters.Select((x, y) => $"p{y}: {x}")).Dump();
			}
			AreEqual(scenario.sql, actual.Sql);
			AreEqual(scenario.parameters, actual.Parameters);
			Console.WriteLine();
		}
	}

	#endregion
}