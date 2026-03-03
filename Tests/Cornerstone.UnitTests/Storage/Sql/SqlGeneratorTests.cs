#region References

using System;
using System.Linq;
using Cornerstone.Reflection;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Cornerstone.Testing;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

public class SqlGeneratorTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void GetCreateTableScriptForSqlite()
	{
		var sourceTypeInfo = SourceReflector.GetRequiredSourceType<Account>();
		var actual = SqlGenerator.GetCreateTableScript(sourceTypeInfo, SqlProvider.Sqlite);
		actual.Dump();

		// Generated Code - Expected
		var expected =
			"""
			CREATE TABLE IF NOT EXISTS "Accounts"
			(
				"Birthday" DATE,
				"CreatedOn" DATE NOT NULL,
				"Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
				"IsDeleted" INTEGER NOT NULL,
				"ModifiedOn" DATE NOT NULL,
				"Name" TEXT NOT NULL,
				"SyncId" TEXT NOT NULL
			);
			""";
		// Generated Code - /Expected

		ValidateExpected(expected, actual);
	}

	[Test]
	public void GetCreateTableScriptForSqlServer()
	{
		var sourceTypeInfo = SourceReflector.GetRequiredSourceType<Account>();
		var actual = SqlGenerator.GetCreateTableScript(sourceTypeInfo, SqlProvider.SqlServer);
		actual.Dump();

		// Generated Code - Expected
		var expected = 
			"""
			IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Accounts')
			BEGIN
				CREATE TABLE [Accounts]
				(
					"Birthday" DATETIME2,
					"CreatedOn" DATETIME2 NOT NULL,
					"Id" INT NOT NULL IDENTITY(1,1),
					"IsDeleted" BIT NOT NULL,
					"ModifiedOn" DATETIME2 NOT NULL,
					"Name" NVARCHAR(MAX) NOT NULL,
					"SyncId" UNIQUEIDENTIFIER NOT NULL,
					CONSTRAINT PK_Accounts PRIMARY KEY CLUSTERED ([Id])
				)
			END
			""";
		// Generated Code - /Expected

		ValidateExpected(expected, actual);
	}

	[Test]
	public void GetInsertQueryForSqlite()
	{
		var account = GetAccount();
		var (sql, parameters) = SqlGenerator.GetInsertQuery(account, SqlProvider.Sqlite);

		sql.Dump();
		parameters.Values.ToArray().DumpArray(Environment.NewLine);

		// Generated Code - Expected
		var expected = 
			"""
			INSERT INTO "Accounts" ("Birthday", "CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId")
				VALUES (@p0, @p1, @p2, @p3, @p4, @p5)
			ON CONFLICT("Id") DO UPDATE SET
				"Birthday" = [Birthday, @p0],
				"CreatedOn" = [CreatedOn, @p1],
				"IsDeleted" = [IsDeleted, @p2],
				"ModifiedOn" = [ModifiedOn, @p3],
				"Name" = [Name, @p4],
				"SyncId" = [SyncId, @p5]
			RETURNING "Id";
			""";
		// Generated Code - /Expected

		ValidateExpected(expected, sql);

		AreEqual([
				StartDateTime.Subtract(TimeSpan.FromDays(365 * 21)),
				DateTime.MinValue,
				false,
				DateTime.MinValue,
				"John",
				Guid.Empty
			],
			parameters.Values
		);
	}

	[Test]
	public void GetInsertQueryForSqlServer()
	{
		var account = GetAccount();
		var (sql, parameters) = SqlGenerator.GetInsertQuery(account, SqlProvider.SqlServer);

		sql.Dump();
		parameters.Values.ToArray().DumpArray(Environment.NewLine);

		// Generated Code - Expected
		var expected = 
			"""
			MERGE INTO [Accounts] AS x
			USING (VALUES ([Birthday, @p0], [CreatedOn, @p1], [IsDeleted, @p2], [ModifiedOn, @p3], [Name, @p4], [SyncId, @p5]))
				AS y ([Birthday], [CreatedOn], [Id], [IsDeleted], [ModifiedOn], [Name], [SyncId])
			ON x.[Id] = y.[Id]
			WHEN MATCHED THEN
				UPDATE SET
					[Birthday] = y.[Birthday],
					[CreatedOn] = y.[CreatedOn],
					[IsDeleted] = y.[IsDeleted],
					[ModifiedOn] = y.[ModifiedOn],
					[Name] = y.[Name],
					[SyncId] = y.[SyncId]
			WHEN NOT MATCHED THEN
				INSERT ([Birthday], [CreatedOn], [Id], [IsDeleted], [ModifiedOn], [Name], [SyncId])
				VALUES (y.[Birthday], y.[CreatedOn], y.[Id], y.[IsDeleted], y.[ModifiedOn], y.[Name], y.[SyncId])
			OUTPUT inserted.[Id];
			""";
		// Generated Code - /Expected

		ValidateExpected(expected, sql);

		AreEqual([
				StartDateTime.Subtract(TimeSpan.FromDays(365 * 21)),
				DateTime.MinValue,
				false,
				DateTime.MinValue,
				"John",
				Guid.Empty
			],
			parameters.Values
		);
	}

	private static Account GetAccount()
	{
		return new Account
		{
			Birthday = StartDateTime.Subtract(TimeSpan.FromDays(365 * 21)),
			Name = "John"
		};
	}

	#endregion
}