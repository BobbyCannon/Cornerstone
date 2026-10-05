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
	public void GetCreateTableScriptForMigrationOmitsIfNotExists()
	{
		var sourceTypeInfo = SourceReflector.GetRequiredSourceType<AccountEntity>();
		var sqlite = SqlGenerator.GetCreateTableScript(sourceTypeInfo, SqlProvider.Sqlite, false);
		IsTrue(sqlite.StartsWith("CREATE TABLE \"Accounts\""));
		IsFalse(sqlite.Contains("IF NOT EXISTS"));

		var sqlServer = SqlGenerator.GetCreateTableScript(sourceTypeInfo, SqlProvider.SqlServer, false);
		IsTrue(sqlServer.StartsWith("CREATE TABLE [Accounts]"));
		IsFalse(sqlServer.Contains("IF NOT EXISTS"));
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
					"AddressId" BIGINT,
					"AddressSyncId" UNIQUEIDENTIFIER,
					"CreatedOn" DATETIME2 NOT NULL,
					"CustomerId" INT,
					"CustomerSyncId" UNIQUEIDENTIFIER,
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
					CONSTRAINT PK_Accounts PRIMARY KEY CLUSTERED ([Id]),
					CONSTRAINT UQ_Accounts_SyncId UNIQUE ([SyncId]),
					CONSTRAINT FK_Accounts_Addresses_AddressId FOREIGN KEY ([AddressId]) REFERENCES [Addresses]([Id])
				);
			END
			""";

		// Generated Code - /Expected

		ValidateExpected(expected, actual);
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
				"AddressId" INTEGER,
				"AddressSyncId" TEXT,
				"CreatedOn" DATE NOT NULL,
				"CustomerId" INTEGER,
				"CustomerSyncId" TEXT,
				"EmailAddress" TEXT NOT NULL,
				"Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
				"IsDeleted" INTEGER NOT NULL,
				"LastLoginDate" DATE NOT NULL,
				"ModifiedOn" DATE NOT NULL,
				"Name" TEXT NOT NULL,
				"Picture" TEXT,
				"Roles" TEXT NOT NULL,
				"Status" INTEGER NOT NULL,
				"SyncId" TEXT NOT NULL UNIQUE,
				"TimeZoneId" TEXT,
				FOREIGN KEY ("AddressId") REFERENCES "Addresses" ("Id")
			);
			""";

		// Generated Code - /Expected

		ValidateExpected(expected, actual);
	}

	[TestMethod]
	public void GetMaxBatchRowsUsesProviderParameterLimits()
	{
		AreEqual(136, SqlGenerator.GetMaxBatchRows(SqlProvider.Sqlite, 15));
		AreEqual(140, SqlGenerator.GetMaxBatchRows(SqlProvider.SqlServer, 15));
		AreEqual(1, SqlGenerator.GetMaxBatchRows(SqlProvider.Sqlite, 0));
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
			USING (VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14))
				AS y ([AddressId], [AddressSyncId], [CreatedOn], [CustomerId], [CustomerSyncId], [EmailAddress], [IsDeleted], [LastLoginDate], [ModifiedOn], [Name], [Picture], [Roles], [Status], [SyncId], [TimeZoneId])
			ON x.[Id] = @p15
			WHEN MATCHED THEN
				UPDATE SET
					[AddressId] = y.[AddressId],
					[AddressSyncId] = y.[AddressSyncId],
					[CreatedOn] = y.[CreatedOn],
					[CustomerId] = y.[CustomerId],
					[CustomerSyncId] = y.[CustomerSyncId],
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
				INSERT ([AddressId], [AddressSyncId], [CreatedOn], [CustomerId], [CustomerSyncId], [EmailAddress], [IsDeleted], [LastLoginDate], [ModifiedOn], [Name], [Picture], [Roles], [Status], [SyncId], [TimeZoneId])
				VALUES (y.[AddressId], y.[AddressSyncId], y.[CreatedOn], y.[CustomerId], y.[CustomerSyncId], y.[EmailAddress], y.[IsDeleted], y.[LastLoginDate], y.[ModifiedOn], y.[Name], y.[Picture], y.[Roles], y.[Status], y.[SyncId], y.[TimeZoneId])
				OUTPUT inserted.[Id];
			""";

		// Generated Code - /Expected

		ValidateExpected(expected, sql);

		AreEqual([
				null,
				null,
				DateTime.MinValue,
				null,
				null,
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
			INSERT INTO "Accounts" ("AddressId", "AddressSyncId", "CreatedOn", "CustomerId", "CustomerSyncId", "EmailAddress", "IsDeleted", "LastLoginDate", "ModifiedOn", "Name", "Picture", "Roles", "Status", "SyncId", "TimeZoneId")
				VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14)
			ON CONFLICT("Id") DO UPDATE SET
				"AddressId" = @p0,
				"AddressSyncId" = @p1,
				"CreatedOn" = @p2,
				"CustomerId" = @p3,
				"CustomerSyncId" = @p4,
				"EmailAddress" = @p5,
				"IsDeleted" = @p6,
				"LastLoginDate" = @p7,
				"ModifiedOn" = @p8,
				"Name" = @p9,
				"Picture" = @p10,
				"Roles" = @p11,
				"Status" = @p12,
				"SyncId" = @p13,
				"TimeZoneId" = @p14
			RETURNING "Id";
			""";

		// Generated Code - /Expected

		ValidateExpected(expected, sql);

		AreEqual([
				null,
				null,
				DateTime.MinValue,
				null,
				null,
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
	public void GetSqlColumnTypeMatchesGeneratedCreateTable()
	{
		AreEqual("DATE", SqlGenerator.GetSqlColumnType(typeof(DateTime), SqlProvider.Sqlite));
		AreEqual("TEXT", SqlGenerator.GetSqlColumnType(typeof(Guid), SqlProvider.Sqlite));
		AreEqual("DATETIME2", SqlGenerator.GetSqlColumnType(typeof(DateTime), SqlProvider.SqlServer));
		AreEqual("UNIQUEIDENTIFIER", SqlGenerator.GetSqlColumnType(typeof(Guid), SqlProvider.SqlServer));
		AreEqual("DECIMAL(18,6)", SqlGenerator.GetSqlColumnType(typeof(decimal), SqlProvider.SqlServer));
		AreEqual("VARBINARY(MAX)", SqlGenerator.GetSqlColumnType(typeof(byte[]), SqlProvider.SqlServer));
		AreEqual("NVARCHAR(MAX)", SqlGenerator.GetSqlColumnType(typeof(string), SqlProvider.SqlServer));
	}

	[TestMethod]
	public void GetSyncUpsertBatchQueryForSqlite()
	{
		var first = GetAccountEntity();
		first.SyncId = new Guid("3B0AB396-BE53-4C48-9EFD-6C22D1098524");
		var second = GetAccountEntity();
		second.Name = "Jane";
		second.SyncId = new Guid("02FDCC17-61A5-4C9E-83EA-3DB562E08E65");
		var (sql, parameters) = SqlGenerator.GetSyncUpsertBatchQuery([first, second], SqlProvider.Sqlite);
		sql.Dump();

		IsTrue(sql.Contains("VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14), (@p15, @p16, @p17, @p18, @p19, @p20, @p21, @p22, @p23, @p24, @p25, @p26, @p27, @p28, @p29)"));
		IsTrue(sql.Contains("ON CONFLICT(\"SyncId\") DO UPDATE SET"));
		IsTrue(sql.Contains("= excluded.\"Name\""));
		IsTrue(sql.Contains("WHERE excluded.\"ModifiedOn\" > \"Accounts\".\"ModifiedOn\""));
		IsTrue(sql.Contains("RETURNING \"Id\", \"SyncId\""));
		AreEqual(30, parameters.Count);
		AreEqual("John", parameters["@p9"].Item1);
		AreEqual("Jane", parameters["@p24"].Item1);
	}

	[TestMethod]
	public void GetSyncUpsertQueryForSqlServer()
	{
		var account = GetAccountEntity();
		var (sql, parameters) = SqlGenerator.GetSyncUpsertQuery(account, SqlProvider.SqlServer);

		sql.Dump();
		parameters.Values.ToArray().DumpArray(Environment.NewLine);

		// Generated Code - Expected
		var expected =
			"""
			MERGE INTO [Accounts] AS x
			USING (VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14))
				AS y ([AddressId], [AddressSyncId], [CreatedOn], [CustomerId], [CustomerSyncId], [EmailAddress], [IsDeleted], [LastLoginDate], [ModifiedOn], [Name], [Picture], [Roles], [Status], [SyncId], [TimeZoneId])
			ON x.[SyncId] = y.[SyncId]
			WHEN MATCHED AND y.[ModifiedOn] > x.[ModifiedOn] THEN
				UPDATE SET
					[AddressId] = y.[AddressId],
					[AddressSyncId] = y.[AddressSyncId],
					[CreatedOn] = y.[CreatedOn],
					[CustomerId] = y.[CustomerId],
					[CustomerSyncId] = y.[CustomerSyncId],
					[EmailAddress] = y.[EmailAddress],
					[IsDeleted] = y.[IsDeleted],
					[LastLoginDate] = y.[LastLoginDate],
					[ModifiedOn] = y.[ModifiedOn],
					[Name] = y.[Name],
					[Picture] = y.[Picture],
					[Roles] = y.[Roles],
					[Status] = y.[Status],
					[TimeZoneId] = y.[TimeZoneId]
			WHEN NOT MATCHED THEN
				INSERT ([AddressId], [AddressSyncId], [CreatedOn], [CustomerId], [CustomerSyncId], [EmailAddress], [IsDeleted], [LastLoginDate], [ModifiedOn], [Name], [Picture], [Roles], [Status], [SyncId], [TimeZoneId])
				VALUES (y.[AddressId], y.[AddressSyncId], y.[CreatedOn], y.[CustomerId], y.[CustomerSyncId], y.[EmailAddress], y.[IsDeleted], y.[LastLoginDate], y.[ModifiedOn], y.[Name], y.[Picture], y.[Roles], y.[Status], y.[SyncId], y.[TimeZoneId])
				OUTPUT inserted.[Id];
			""";

		// Generated Code - /Expected

		ValidateExpected(expected, sql);

		AreEqual([
				null,
				null,
				DateTime.MinValue,
				null,
				null,
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
	public void GetSyncUpsertQueryForSqlite()
	{
		var account = GetAccountEntity();
		var (sql, parameters) = SqlGenerator.GetSyncUpsertQuery(account, SqlProvider.Sqlite);

		sql.Dump();
		parameters.Values.ToArray().DumpArray(Environment.NewLine);

		// Generated Code - Expected
		var expected =
			"""
			INSERT INTO "Accounts" ("AddressId", "AddressSyncId", "CreatedOn", "CustomerId", "CustomerSyncId", "EmailAddress", "IsDeleted", "LastLoginDate", "ModifiedOn", "Name", "Picture", "Roles", "Status", "SyncId", "TimeZoneId")
				VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14)
			ON CONFLICT("SyncId") DO UPDATE SET
				"AddressId" = @p0,
				"AddressSyncId" = @p1,
				"CreatedOn" = @p2,
				"CustomerId" = @p3,
				"CustomerSyncId" = @p4,
				"EmailAddress" = @p5,
				"IsDeleted" = @p6,
				"LastLoginDate" = @p7,
				"ModifiedOn" = @p8,
				"Name" = @p9,
				"Picture" = @p10,
				"Roles" = @p11,
				"Status" = @p12,
				"TimeZoneId" = @p14
			WHERE @p8 > "Accounts"."ModifiedOn"
			RETURNING "Id";
			""";

		// Generated Code - /Expected

		ValidateExpected(expected, sql);

		AreEqual([
				null,
				null,
				DateTime.MinValue,
				null,
				null,
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
	public void GetTableName()
	{
		AreEqual("Accounts", SqlGenerator.GetTableName(SourceReflector.GetSourceType<AccountEntity>()));
	}

	[TestMethod]
	public void GetTableQueryScriptSqlServerIsOneRowPerColumn()
	{
		var sql = SqlGenerator.GetTableQueryScript(SqlProvider.SqlServer);
		IsTrue(sql.Contains("FROM sys.tables t"));
		IsTrue(sql.Contains("INNER JOIN sys.columns c"));
		IsFalse(sql.Contains("LEFT JOIN sys.indexes"));
		IsTrue(sql.Contains("EXISTS ("));
	}

	[TestMethod]
	public void NormalizeDeclaredTypeMapsSqliteAliases()
	{
		AreEqual("INTEGER", SqlGenerator.NormalizeDeclaredType("INT"));
		AreEqual("INTEGER", SqlGenerator.NormalizeDeclaredType("BIGINT"));
		AreEqual("TEXT", SqlGenerator.NormalizeDeclaredType("NVARCHAR(MAX)"));
		AreEqual("DATE", SqlGenerator.NormalizeDeclaredType("DATETIME2"));
		AreEqual("DATE", SqlGenerator.NormalizeDeclaredType("DATE"));
		AreEqual("BLOB", SqlGenerator.NormalizeDeclaredType("VARBINARY(MAX)"));
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