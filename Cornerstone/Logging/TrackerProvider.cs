#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Microsoft.Extensions.Caching.Memory;

#endregion

namespace Cornerstone.Logging;

public class TrackerProvider<T>
	: TrackerProvider, ITrackerRepository
	where T : ISyncableDatabase, ITrackerDatabase
{
	#region Fields

	private readonly SyncableDatabaseProvider<T> _databaseProvider;

	#endregion

	#region Constructors

	public TrackerProvider(
		SyncableDatabaseProvider<T> databaseProvider,
		IDateTimeProvider dateTimeProvider,
		TimeSpan timeout)
		: base(dateTimeProvider, timeout)
	{
		_databaseProvider = databaseProvider;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Write(params TrackerPath[] paths)
	{
		using var database = _databaseProvider.GetDatabase();
		paths.ForEach(x => WritePath(database, x, null));

		try
		{
			database.SaveChanges();
		}
		catch
		{
			WriteEventsIndividually(paths);
		}
	}

	private void AddOrUpdateEvent(T database, TrackerPath item, TrackerPathEntity parent)
	{
		var entity = database.TrackerPaths.FirstOrDefault(x => x.SyncId == item.Id);

		if (entity != null)
		{
			Update(database, entity, item);
		}
		else
		{
			entity = ToEntity(database, item, parent);
			database.TrackerPaths.Add(entity);
		}

		item.Children.ForEach(x => AddOrUpdateEvent(database, x, entity));
	}

	private TrackerPathEntity ToEntity(T database, TrackerPath item, TrackerPathEntity parent)
	{
		var response = new TrackerPathEntity { Parent = parent, SyncId = item.Id };
		Update(database, response, item);
		return response;
	}

	private void Update(T database, TrackerPathEntity entity, TrackerPath item)
	{
		entity.CompletedOn = item.CompletedOn;
		entity.Data = item.Data;
		entity.StartedOn = item.StartedOn;
		entity.SyncId = item.Id;

		if (entity.SyncId == Guid.Empty)
		{
			entity.SyncId = Guid.NewGuid();
			item.Id = entity.SyncId;
		}

		var configuration = entity.Configuration
			?? (Cache.TryGetValue(item.Name, out var result)
				? (TrackerPathConfigurationEntity) result
				: entity.Configuration
			);

		if (configuration == null)
		{
			// Configuration doesn't exist on the entity or in the cache, so try and read it then cache if found
			configuration = database.TrackerPathConfigurations.FirstOrDefault(x => x.PathName == item.Name);
			if (configuration != null)
			{
				Cache.Set(item.Name, configuration, DateTimeProvider.UtcNow.Add(Timeout));
			}
		}

		if (configuration == null)
		{
			// Item was not on the entity, cached, or in the database. We need to add the event configuration
			configuration = new TrackerPathConfigurationEntity { PathName = item.Name, PathType = item.Type ?? item.Name, IsException = item.IsException };
			database.TrackerPathConfigurations.Add(configuration);
			database.SaveChanges();
			Cache.Set(item.Name, configuration, DateTimeProvider.UtcNow.Add(Timeout));
		}

		if ((entity.Configuration == null) && (entity.ConfigurationId == 0))
		{
			entity.ConfigurationId = configuration.Id;
		}

		foreach (var value in item.Values.OrderBy(x => x.Name))
		{
			var configurationValues = ConfigurationNameInfos.ToDictionary(x => x, x => x.GetValue(configuration)?.ToString());
			var configurationProperty = configurationValues.FirstOrDefault(x => x.Value == value.Name);

			if (configurationProperty.Key == null)
			{
				configurationProperty = configurationValues.FirstOrDefault(x => x.Value is not { Length: > 0 });
				if (configurationProperty.Key == null)
				{
					continue;
				}

				configurationProperty.Key.SetValue(configuration, value.Name);
			}

			var eventProperty = TrackerPathValueInfos.First(x => x.Name == configurationProperty.Key.Name.Replace("Name", "Value"));
			eventProperty.SetValue(entity, value.Value.MaxLength(900));
		}
	}

	private void WriteEventsIndividually(TrackerPath[] paths)
	{
		using (var database = _databaseProvider.GetDatabase())
		{
			foreach (var x in paths)
			{
				try
				{
					AddOrUpdateEvent(database, x, null);
					database.SaveChanges();
				}
				catch (Exception)
				{
					// log?
				}
			}
		}
	}

	private void WritePath(T database, TrackerPath item, TrackerPathEntity parent)
	{
		var entity = ToEntity(database, item, parent);
		database.TrackerPaths.Add(entity);
		item.Children.ForEach(x => WritePath(database, x, entity));
	}

	#endregion
}

public abstract class TrackerProvider
{
	#region Fields

	protected readonly MemoryCache Cache;
	protected readonly List<PropertyInfo> ConfigurationNameInfos;
	protected readonly IDateTimeProvider DateTimeProvider;
	protected readonly TimeSpan Timeout;
	protected readonly List<PropertyInfo> TrackerPathValueInfos;

	#endregion

	#region Constructors

	protected TrackerProvider(IDateTimeProvider dateTimeProvider, TimeSpan timeout)
	{
		Cache = new MemoryCache(new MemoryCacheOptions());
		DateTimeProvider = dateTimeProvider;
		Timeout = timeout;
		TrackerPathValueInfos = typeof(TrackerPathEntity)
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(x => x.Name.StartsWith("Value"))
			.ToList();

		ConfigurationNameInfos = typeof(TrackerPathConfigurationEntity)
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(x => x.Name.StartsWith("Name"))
			.ToList();
	}

	#endregion
}

/// <summary>
/// Interface for storing tracker path data.
/// </summary>
public interface ITrackerRepository
{
	#region Methods

	/// <summary>
	/// Writes a collection of tracker paths.
	/// </summary>
	/// <param name="paths"> The tracker paths to write. </param>
	void Write(params TrackerPath[] paths);

	#endregion
}