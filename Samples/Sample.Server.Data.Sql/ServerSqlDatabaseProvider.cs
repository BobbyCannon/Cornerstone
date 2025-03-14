﻿#region References

using Cornerstone.Runtime;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data.Sql;

public class ServerSqlDatabaseProvider : ServerDatabaseProvider
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ServerSqlDatabaseProvider(string connectionString, IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IServerDatabase GetDatabaseFromProvider()
	{
		return ServerSqlDatabase.UseSqlServer(_connectionString);
	}

	#endregion
}