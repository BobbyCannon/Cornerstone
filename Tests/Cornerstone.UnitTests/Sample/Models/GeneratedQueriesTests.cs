#region References

using System;
using System.Collections.Generic;
using Cornerstone.Reflection;
using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sample.Models;

[TestClass]
public class GeneratedQueriesTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GeneratedQueries()
	{
		var info = SourceReflector.GetSourceType(typeof(SqlGenerator));
		var value = (Dictionary<(Type, SqlProvider), string>) info.GetField("_tableScripts").GetValue(null);
		IsTrue(value.TryGetValue((typeof(AccountEntity), SqlProvider.Sqlite), out var query));
		AreEqual("""
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
				""", query);

		IsTrue(value.TryGetValue((typeof(AccountEntity), SqlProvider.SqlServer), out query));
		AreEqual("""
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
				""", query);
	}

	#endregion
}