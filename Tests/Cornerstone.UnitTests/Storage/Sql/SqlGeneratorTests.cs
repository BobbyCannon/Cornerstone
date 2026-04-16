#region References

using System;
using System.Linq;
using Cornerstone.Reflection;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
public class SqlGeneratorTests : GeneratorUnitTest
{
	#region Methods

	[TestMethod]
	public void GetTableName()
	{
		AreEqual("Accounts", SqlGenerator.GetTableName(SourceReflector.GetSourceType<AccountEntity>()));
	}

	[TestMethod]
	public void GetCreateTableScriptForSqlite()
	{
		var sourceTypeInfo = SourceReflector.GetRequiredSourceType<AccountEntity>();
		var actual = SqlGenerator.GetCreateTableScript(sourceTypeInfo, SqlProvider.Sqlite);
		actual.Dump();

		// Generated Code - Expected
		var expected =
			"""
			CREATE TABLE IF NOT EXISTS "Accounts"
			(
				"CreatedOn" DATE NOT NULL,
				"EmailAddress" TEXT NOT NULL,
				"Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
				"IsDeleted" INTEGER NOT NULL,
				"LastLoginDate" DATE NOT NULL,
				"ModifiedOn" DATE NOT NULL,
				"Name" TEXT NOT NULL,
				"Picture" TEXT,
				"Roles" TEXT NOT NULL,
				"Status" INTEGER NOT NULL,
				"SyncId" TEXT NOT NULL,
				"TimeZoneId" TEXT
			);
			""";
		// Generated Code - /Expected

		ValidateExpected(expected, actual);
	}

	[TestMethod]
	public void GetCreateTableScriptForSqlServer()
	{
		var sourceTypeInfo = SourceReflector.GetRequiredSourceType<AccountEntity>();
		var actual = SqlGenerator.GetCreateTableScript(sourceTypeInfo, SqlProvider.SqlServer);
		actual.Dump();

		// Generated Code - Expected
		var expected =
			"""
			IF NOT EXISTS (SELECT * FROM [sys].[tables] WHERE [name] = 'Accounts')
			BEGIN
				CREATE TABLE [Accounts]
				(
					"CreatedOn" DATETIME2 NOT NULL,
					"EmailAddress" NVARCHAR(MAX) NOT NULL,
					"Id" INT NOT NULL IDENTITY(1,1),
					"IsDeleted" BIT NOT NULL,
					"LastLoginDate" DATETIME2 NOT NULL,
					"ModifiedOn" DATETIME2 NOT NULL,
					"Name" NVARCHAR(MAX) NOT NULL,
					"Picture" NVARCHAR(MAX),
					"Roles" NVARCHAR(MAX) NOT NULL,
					"Status" INT NOT NULL,
					"SyncId" UNIQUEIDENTIFIER NOT NULL,
					"TimeZoneId" NVARCHAR(MAX),
					CONSTRAINT PK_Accounts PRIMARY KEY CLUSTERED ([Id])
				)
			END
			""";
		// Generated Code - /Expected

		ValidateExpected(expected, actual);
	}

	[TestMethod]
	public void GetInsertQueryForSqlite()
	{
		var account = GetAccountEntity();
		var (sql, parameters) = SqlGenerator.GetInsertQuery(account, SqlProvider.Sqlite);

		sql.Dump();
		parameters.Values.ToArray().DumpArray(Environment.NewLine);

		// Generated Code - Expected
		var expected =
			"""
			INSERT INTO "Accounts" ("CreatedOn", "EmailAddress", "IsDeleted", "LastLoginDate", "ModifiedOn", "Name", "Picture", "Roles", "Status", "SyncId", "TimeZoneId")
				VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)
			ON CONFLICT("Id") DO UPDATE SET
				"CreatedOn" = @p0,
				"EmailAddress" = @p1,
				"IsDeleted" = @p2,
				"LastLoginDate" = @p3,
				"ModifiedOn" = @p4,
				"Name" = @p5,
				"Picture" = @p6,
				"Roles" = @p7,
				"Status" = @p8,
				"SyncId" = @p9,
				"TimeZoneId" = @p10
			RETURNING "Id";
			""";
		// Generated Code - /Expected

		ValidateExpected(expected, sql);

		AreEqual([
				DateTime.MinValue,
				"john@domain.com",
				false,
				StartDateTime,
				DateTime.MinValue,
				"John",
				null,
				",,",
				AccountStatus.Enabled,
				Guid.Empty,
				""
			],
			parameters.Values.Select(x => x.Item1).ToArray()
		);
	}

	[TestMethod]
	public void GetInsertQueryForSqlServer()
	{
		var account = GetAccountEntity();
		var (sql, parameters) = SqlGenerator.GetInsertQuery(account, SqlProvider.SqlServer);

		sql.Dump();
		parameters.Values.ToArray().DumpArray(Environment.NewLine);

		// Generated Code - Expected
		var expected =
			"""
			MERGE INTO [Accounts] AS x
			USING (VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10))
				AS y ([CreatedOn], [EmailAddress], [IsDeleted], [LastLoginDate], [ModifiedOn], [Name], [Picture], [Roles], [Status], [SyncId], [TimeZoneId])
			ON x.[Id] = @p11
			WHEN MATCHED THEN
				UPDATE SET
					[CreatedOn] = y.[CreatedOn],
					[EmailAddress] = y.[EmailAddress],
					[IsDeleted] = y.[IsDeleted],
					[LastLoginDate] = y.[LastLoginDate],
					[ModifiedOn] = y.[ModifiedOn],
					[Name] = y.[Name],
					[Picture] = y.[Picture],
					[Roles] = y.[Roles],
					[Status] = y.[Status],
					[SyncId] = y.[SyncId],
					[TimeZoneId] = y.[TimeZoneId]
				WHEN NOT MATCHED THEN
				INSERT ([CreatedOn], [EmailAddress], [IsDeleted], [LastLoginDate], [ModifiedOn], [Name], [Picture], [Roles], [Status], [SyncId], [TimeZoneId])
				VALUES (y.[CreatedOn], y.[EmailAddress], y.[IsDeleted], y.[LastLoginDate], y.[ModifiedOn], y.[Name], y.[Picture], y.[Roles], y.[Status], y.[SyncId], y.[TimeZoneId])
				OUTPUT inserted.[Id];
			""";
		// Generated Code - /Expected

		ValidateExpected(expected, sql);

		AreEqual([
				DateTime.MinValue,
				"john@domain.com",
				false,
				StartDateTime,
				DateTime.MinValue,
				"John",
				null,
				",,",
				AccountStatus.Enabled,
				Guid.Empty,
				"",
				0
			],
			parameters.Values.Select(x => x.Item1).ToArray()
		);
	}

	private static AccountEntity GetAccountEntity()
	{
		return new AccountEntity
		{
			EmailAddress = "john@domain.com",
			Name = "John",
			LastLoginDate = StartDateTime,
			Roles = ",,",
			Status = AccountStatus.Enabled,
			TimeZoneId = string.Empty
		};
	}

	#endregion
}