#region References

using System.Threading;
using Cornerstone.EntityFramework;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Cornerstone.Testing;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.UnitTests;

public static class TestExtensions
{
	#region Methods

	public static void AddSaveAndCleanup<TDb, T>(this TDb database, T item)
		where TDb : IDatabase
		where T : Entity
	{
		database.GetRepository<T>().Add(item);
		database.SaveChanges();
		database.Dispose();

		// Because sync is based on "until" (less than, not equal) then we must delay at least a millisecond to delay the data.
		Thread.Sleep(1);
	}

	public static void AddSaveAndCleanupForClient<TDb, T>(this SyncManager manager, T item)
		where TDb : IDatabase
		where T : Entity
	{
		var database = (TDb) manager.ClientSyncClientProvider.GetSyncableDatabase();

		database.GetRepository<T>().Add(item);
		database.SaveChanges();
		database.Dispose();

		// Because sync is based on "until" (less than, not equal) then we must delay at least a millisecond to delay the data.
		Thread.Sleep(1);
	}

	public static void AddSaveAndCleanupForServer<TDb, T>(this SyncManager manager, T item)
		where TDb : IDatabase
		where T : Entity
	{
		var database = (TDb) manager.ServerSyncClientProvider.GetSyncableDatabase();

		database.GetRepository<T>().Add(item);
		database.SaveChanges();
		database.Dispose();

		// Because sync is based on "until" (less than, not equal) then we must delay at least a millisecond to delay the data.
		Thread.Sleep(1);
	}

	public static T ClearDatabase<T>(this T database) where T : EntityFrameworkDatabase
	{
		database.Database.ExecuteSqlRaw(CornerstoneTest.ClearDatabaseQuery);
		return database;
	}

	public static void RemoveSaveAndCleanup<TDb, T, T2>(this TDb database, T item)
		where TDb : IDatabase
		where T : Entity<T2>
	{
		database.Remove<T, T2>(item);
		database.SaveChanges();
		database.Dispose();

		// Because sync is based on "until" (less than, not equal) then we must delay at least a millisecond to delay the data.
		Thread.Sleep(1);
	}

	#endregion
}