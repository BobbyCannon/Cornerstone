#region References

using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;

#endregion

namespace Cornerstone.Storage.Sql;

public class SqlDatabase : IDisposable
{
	#region Fields

	private readonly DbConnection _connection;

	#endregion

	#region Constructors

	public SqlDatabase(string connectionString, SqlProvider provider)
	{
		ConnectionString = connectionString;
		Provider = provider;

		if ((provider == SqlProvider.Sqlite)
			&& ConnectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase))
		{
			// This connection keeps the :memory: DB alive until this database is disposed.
			_connection = new SqliteConnection(connectionString);
			_connection.Open();
		}
	}

	#endregion

	#region Properties

	public string ConnectionString { get; }

	public SqlProvider Provider { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public SqlRepository<T> GetRepository<T>() where T : Entity, new()
	{
		return new SqlRepository<T>(ConnectionString, Provider);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		_connection?.Close();
		_connection?.Dispose();
	}

	#endregion
}