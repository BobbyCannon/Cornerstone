#region References

using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
public class PredicateToSqlVisitorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	[SuppressMessage("ReSharper", "NegativeEqualityExpression")]
	[SuppressMessage("ReSharper", "DoubleNegationOperator")]
	[SuppressMessage("ReSharper", "ArrangeMissingParentheses")]
	[SuppressMessage("ReSharper", "CSharp14OverloadResolutionWithSpanBreakingChange")]
	public void Translate()
	{
		var scenarios = new (Expression<Func<AccountEntity, bool>> value, string sql, object[] parameters)[]
		{
			(p => null == p.Name, "([Name] IS NULL)", []),
			(p => null != p.Name, "([Name] IS NOT NULL)", []),
			(p => true == p.IsDeleted, "([IsDeleted] = 1)", []),
			(p => false == p.IsDeleted, "([IsDeleted] = 0)", []),
			(p => p.IsDeleted != true, "([IsDeleted] = 0)", []),
			(p => p.IsDeleted != false, "([IsDeleted] = 1)", []),
			(p => p.Status != AccountStatus.Enabled, "([Status] <> @p0)", [(int) AccountStatus.Enabled]),
			(p => p.CreatedOn != DateTime.Today, "([CreatedOn] <> @p0)", [DateTime.Today]),
			(p => p.Picture == null, "([Picture] IS NULL)", []),
			(p => p.TimeZoneId != null, "([TimeZoneId] IS NOT NULL)", []),
			(p => p.Name == "", "([Name] = @p0)", [""]),
			(p => !(p.Name != "John"), "([Name] = @p0)", ["John"]),
			(p => !(p.Id <= 10), "([Id] > @p0)", [10]),
			(p => !(p.Id < 10), "([Id] >= @p0)", [10]),
			(p => !(p.Id >= 10), "([Id] < @p0)", [10]),
			(p => p.Id > 100, "([Id] > @p0)", [100]),
			(p => p.Name == "Hardcoded", "([Name] = @p0)", ["Hardcoded"]),
			(p => p.CreatedOn == DateTime.Today, "([CreatedOn] = @p0)", [DateTime.Today]),
			(p => p.Status == AccountStatus.Enabled, "([Status] = @p0)", [(int) AccountStatus.Enabled]),
			(p => p.SyncId == Guid.Empty, "([SyncId] = @p0)", [Guid.Empty]),
			(p => p.Name.Length > 5, "(LEN([Name]) > @p0)", [5]),
			(p => p.Name.ToLower().Contains("john"), "([Name] LIKE @p0)", ["%john%"]),
			(p => p.Name.ToUpper() == "JOHN", "([Name] = @p0)", ["JOHN"]),
			(p => string.IsNullOrEmpty(p.Name), "([Name] IS NULL OR [Name] = '')", []),
			(p => string.IsNullOrWhiteSpace(p.Name), "([Name] IS NULL OR LTRIM(RTRIM([Name])) = '')", []),
			(p => p.IsDeleted && p.Name == null, "(([IsDeleted] = 1) AND ([Name] IS NULL))", []),
			(p => p.Id > 10 || p.Name.Contains("Test"), "(([Id] > @p0) OR ([Name] LIKE @p1))", [10, "%Test%"]),
			(p => (p.Id > 10 || p.IsDeleted) && p.Name != null, "((([Id] > @p0) OR ([IsDeleted] = 1)) AND ([Name] IS NOT NULL))", [10]),
			(p => !(p.Id > 10 || p.IsDeleted), "(NOT (([Id] > @p0) OR ([IsDeleted] = 1)))", [10]),
			(p => !p.IsDeleted, "([IsDeleted] = 0)", []),
			(p => p.IsDeleted, "([IsDeleted] = 1)", []),
			(p => p.LastLoginDate <= StartDateTime, "([LastLoginDate] <= @p0)", [StartDateTime]),
			(p => p.LastLoginDate < StartDateTime, "([LastLoginDate] < @p0)", [StartDateTime]),
			(p => p.LastLoginDate >= StartDateTime, "([LastLoginDate] >= @p0)", [StartDateTime]),
			(p => p.LastLoginDate > StartDateTime, "([LastLoginDate] > @p0)", [StartDateTime]),
			(p => p.Name == null, "([Name] IS NULL)", []),
			(p => p.Name.Contains("John") && ((p.Id > 10) || p.IsDeleted), "(([Name] LIKE @p0) AND (([Id] > @p1) OR ([IsDeleted] = 1)))", ["%John%", 10]),
			(p => p.Name.StartsWith("John") && ((p.Id < 10) || !p.IsDeleted), "(([Name] LIKE @p0) AND (([Id] < @p1) OR ([IsDeleted] = 0)))", ["John%", 10]),
			(p => p.Name.EndsWith("John"), "([Name] LIKE @p0)", ["%John"]),
			(p => p.Id == StartDateTime.Ticks, "([Id] = @p0)", [StartDateTime.Ticks]),
			(p => p.Id == int.MaxValue, "([Id] = @p0)", [int.MaxValue]),
			(p => p.Id == int.MinValue, "([Id] = @p0)", [int.MinValue]),
			(p => p.CreatedOn > new DateTime(2025, 02, 05), "([CreatedOn] > @p0)", [new DateTime(2025, 02, 05)]),
			(p => p.CreatedOn > DateTime.MaxValue, "([CreatedOn] > @p0)", [DateTime.MaxValue]),
			(p => p.CreatedOn > DateTime.MinValue, "([CreatedOn] > @p0)", [DateTime.MinValue]),
			(p => p.CreatedOn > DateTime.UnixEpoch, "([CreatedOn] > @p0)", [DateTime.UnixEpoch]),
			(p => p.CreatedOn > StartDateTime, "([CreatedOn] > @p0)", [StartDateTime]),
			(p => p.Id == 42, "([Id] = @p0)", [42]),
			(p => p.Id != 42, "([Id] <> @p0)", [42]),
			(p => p.Name == "John", "([Name] = @p0)", ["John"]),
			(p => p.Name != "John", "([Name] <> @p0)", ["John"]),
			(p => p.Id == 123.45m, "([Id] = @p0)", [123.45m]),
			(p => p.IsDeleted == true, "([IsDeleted] = 1)", []),
			(p => p.IsDeleted == false, "([IsDeleted] = 0)", []),
			(p => !p.IsDeleted, "([IsDeleted] = 0)", []),
			(p => !!p.IsDeleted, "([IsDeleted] = 1)", []),
			(p => !(p.Name == "John"), "([Name] <> @p0)", ["John"]),
			(p => !(p.LastLoginDate > StartDateTime), "([LastLoginDate] <= @p0)", [StartDateTime]),
			(p => !(p.Id > 10 && p.IsDeleted), "(NOT (([Id] > @p0) AND ([IsDeleted] = 1)))", [10])
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