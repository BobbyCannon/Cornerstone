#region References

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Sync;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SyncClientExceptionTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void ForeignKeyConflictRecordsRelationshipIssue()
	{
		AssertThrownApply(
			new Exception("conflicted with the FOREIGN KEY constraint"),
			SyncIssueType.RelationshipConstraint,
			"This entity has relationship issue with another entity.",
			includeDetails: true
		);
	}

	[TestMethod]
	public void InvalidConstraintExceptionRecordsConstraintIssue()
	{
		AssertThrownApply(
			new InvalidConstraintException("duplicate key"),
			SyncIssueType.ConstraintException,
			"Invalid constraint exception...",
			includeDetails: true
		);
	}

	[TestMethod]
	public void InvalidOperationExceptionRecordsRelationshipIssue()
	{
		AssertThrownApply(
			new InvalidOperationException("store rejected the row"),
			SyncIssueType.RelationshipConstraint,
			"Invalid operation exception...",
			includeDetails: true
		);
	}

	[TestMethod]
	public void IssueDetailsStayOffTheMessageWhenTheFlagIsOff()
	{
		AssertThrownApply(
			new InvalidConstraintException("duplicate key"),
			SyncIssueType.ConstraintException,
			"Invalid constraint exception...",
			includeDetails: false
		);
	}

	[TestMethod]
	public void MissingRepositoryRecordsUnknownIssue()
	{
		var provider = new MemoryProvider(this, null);
		var client = new SampleSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
		var sessionId = Guid.NewGuid();
		var settings = new SyncSettings();
		settings.IncludeIssueDetails = true;
		client.BeginSync(sessionId, settings);

		var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(NewAddress()));

		AreEqual(1, result.Collection.Count);
		AreEqual(SyncIssueType.Unknown, result.Collection[0].IssueType);
		IsTrue(result.Collection[0].Message.Contains("Unknown issue..."));
		IsTrue(result.Collection[0].Message.Contains("Failed to find a syncable repository for the entity."));
		AreEqual(1, client.Statistics.IndividualProcessCount);
	}

	[TestMethod]
	public void ReferenceConstraintConflictRecordsRelationshipIssue()
	{
		AssertThrownApply(
			new Exception("The DELETE statement conflicted with the REFERENCE constraint"),
			SyncIssueType.RelationshipConstraint,
			"This entity has relationship issue with another entity.",
			includeDetails: true
		);
	}

	[TestMethod]
	public void RequiredParentMissRecordsRelationshipIssues()
	{
		var parentSyncId = Guid.NewGuid();
		var repository = new MemoryRepository(typeof(RequiredParentEntity));
		var provider = new MemoryProvider(this, repository);
		var client = new RequiredParentClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"), parentSyncId);
		var sessionId = Guid.NewGuid();
		client.BeginSync(sessionId, new SyncSettings());

		var incoming = NewAddress();
		var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(incoming));

		AreEqual(2, result.Collection.Count);
		AreEqual(0, repository.Adds);
		AreEqual(0, client.Statistics.IndividualProcessCount);
		var parentIssue = result.Collection.First(x => x.Message == "Failed to find the relational entity.");
		var rowIssue = result.Collection.First(x => x.Message == "This entity has relationship issues.");
		AreEqual(SyncIssueType.RelationshipConstraint, parentIssue.IssueType);
		AreEqual(SyncIssueType.RelationshipConstraint, rowIssue.IssueType);
		AreEqual(parentSyncId, parentIssue.Id);
		AreEqual(incoming.SyncId, rowIssue.Id);
		AreEqual(typeof(RequiredParentEntity).ToAssemblyName(), parentIssue.TypeName);
		AreEqual(incoming.TypeName, rowIssue.TypeName);
	}

	[TestMethod]
	public void UnclassifiedExceptionRecordsUnknownIssue()
	{
		AssertThrownApply(
			new Exception("boom"),
			SyncIssueType.Unknown,
			"Unknown issue...",
			includeDetails: true
		);
	}

	[TestMethod]
	public void ValidationExceptionRecordsValidationIssue()
	{
		var exception = new ValidationException("name is required");
		AssertThrownApply(exception, SyncIssueType.ValidationException, exception.Message, includeDetails: true);
	}

	private void AssertThrownApply(Exception exception, SyncIssueType issueType, string message, bool includeDetails)
	{
		WithEachProvider((provider, _) =>
		{
			var target = new ThrowingApplySyncClient("Target", provider, this, new SyncStatistics(), new Profiler("Target"), exception);
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings();
			settings.IncludeIssueDetails = includeDetails;
			target.BeginSync(sessionId, settings);

			var result = target.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(NewAddress()));

			AreEqual(1, result.Collection.Count);
			AreEqual(issueType, result.Collection[0].IssueType);
			if (includeDetails)
			{
				IsTrue(result.Collection[0].Message.StartsWith(message, StringComparison.Ordinal));
				IsTrue(result.Collection[0].Message.Contains(exception.Message));
				IsTrue(result.Collection[0].Message.Length > message.Length);
			}
			else
			{
				AreEqual(message, result.Collection[0].Message);
			}

			AreEqual(1, target.Statistics.IndividualProcessCount);
		});
	}

	private SyncObject NewAddress()
	{
		var createdOn = UtcNow;
		return SyncObject.ToSyncObject(new Address
		{
			City = "City",
			CreatedOn = createdOn,
			Line1 = "Incoming",
			ModifiedOn = createdOn,
			Postal = "29640",
			State = "SC",
			SyncId = Guid.NewGuid()
		});
	}

	#endregion

	#region Classes

	private sealed class MemoryDatabase : ISyncableDatabase
	{
		#region Fields

		private readonly MemoryRepository _repository;

		#endregion

		#region Constructors

		public MemoryDatabase(IDateTimeProvider time, MemoryRepository repository)
		{
			_repository = repository;
			DatabaseSettings = new DatabaseSettings();
			DateTimeProvider = time;
			Profiler = null;
			SyncOrder = [];
		}

		#endregion

		#region Properties

		public DatabaseSettings DatabaseSettings { get; }

		public IDateTimeProvider DateTimeProvider { get; }

		public bool IsDisposed => false;

		public DatabaseKeyCache KeyCache => null;

		public Profiler Profiler { get; set; }

		public (string entity, string syncObject)[] SyncOrder { get; }

		#endregion

		#region Methods

		public int DiscardChanges()
		{
			return 0;
		}

		public void Dispose()
		{
		}

		public DatabaseType GetDatabaseType()
		{
			return DatabaseType.Unknown;
		}

		public Assembly GetMappingAssembly()
		{
			return typeof(MemoryDatabase).Assembly;
		}

		public IRepository<T, T2> GetReadOnlyRepository<T, T2>() where T : Entity<T2>
		{
			throw new NotSupportedException();
		}

		public IRepository<T, T2> GetRepository<T, T2>() where T : Entity<T2>
		{
			throw new NotSupportedException();
		}

		public IEnumerable<ISyncableRepository> GetSyncableRepositories()
		{
			if (_repository == null)
			{
				return [];
			}

			return [_repository];
		}

		public ISyncableRepository<T, T2> GetSyncableRepository<T, T2>() where T : SyncEntity<T2>
		{
			throw new NotSupportedException();
		}

		public ISyncableRepository GetSyncableRepository(Type type)
		{
			if ((_repository != null) && (type == _repository.RealType))
			{
				return _repository;
			}

			return null;
		}

		public bool IsDatabaseMigrated()
		{
			return true;
		}

		public void Migrate()
		{
		}

		public T Remove<T, T2>(T item) where T : Entity<T2>
		{
			return item;
		}

		public int SaveChanges()
		{
			return 0;
		}

		public void UpdateDateTimeProvider(IDateTimeProvider dateTimeProvider)
		{
		}

		#endregion

		#region Events

		public event EventHandler<CollectionChangeTracker> ChangesSaved
		{
			add { }
			remove { }
		}

		public event EventHandler Disposed
		{
			add { }
			remove { }
		}

		#endregion
	}

	private sealed class MemoryProvider : ISyncableDatabaseProvider
	{
		#region Fields

		private readonly MemoryDatabase _database;

		#endregion

		#region Constructors

		public MemoryProvider(IDateTimeProvider time, MemoryRepository repository)
		{
			_database = new MemoryDatabase(time, repository);
			var settings = new DatabaseSettings();
			settings.SyncOrder = [];
			Settings = settings;
		}

		#endregion

		#region Properties

		public DatabaseKeyCache KeyCache => null;

		public DatabaseSettings Settings { get; set; }

		#endregion

		#region Methods

		public IDatabase GetDatabase()
		{
			return _database;
		}

		public IDatabase GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
		{
			return _database;
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _database;
		}

		public ISyncableDatabase GetSyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
		{
			return _database;
		}

		#endregion
	}

	private sealed class MemoryRepository : ISyncableRepository
	{
		#region Constructors

		public MemoryRepository(Type realType)
		{
			RealType = realType;
			TypeName = realType.ToAssemblyName();
			Adds = 0;
		}

		#endregion

		#region Properties

		public int Adds { get; private set; }

		public Type RealType { get; }

		public string TypeName { get; }

		#endregion

		#region Methods

		public void Add(ISyncEntity entity)
		{
			Adds++;
		}

		public void Discard(ISyncEntity entity)
		{
		}

		public int GetChangeCount(DateTime since, DateTime until, SyncRepositoryFilter filter)
		{
			return 0;
		}

		public IEnumerable<ISyncEntity> GetChanges(DateTime since, DateTime until, int skip, int take, SyncRepositoryFilter filter)
		{
			return [];
		}

		public ISyncEntity Read(Guid syncId)
		{
			return null;
		}

		public ISyncEntity Read(ISyncEntity entity, SyncRepositoryFilter filter)
		{
			return null;
		}

		public IDictionary<Guid, object> ReadAllKeys()
		{
			return new Dictionary<Guid, object>();
		}

		public ISyncEntity ReadByPrimaryId(object primaryId)
		{
			return null;
		}

		public void Remove(ISyncEntity entity)
		{
		}

		#endregion
	}

	private sealed class RequiredParentClient : SampleSyncClient
	{
		#region Fields

		private readonly Guid _parentSyncId;

		#endregion

		#region Constructors

		public RequiredParentClient(
			string name,
			ISyncableDatabaseProvider provider,
			IDateTimeProvider time,
			SyncStatistics statistics,
			Profiler profiler,
			Guid parentSyncId
		) : base(name, provider, time, statistics, profiler)
		{
			_parentSyncId = parentSyncId;
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return new SyncClientConverter(
				new SyncObjectConverter<SampleSyncClient, Address, RequiredParentEntity>(
					fromSyncModel: (_, _, entity) => entity.ParentSyncId = _parentSyncId,
					update: (_, source, destination, processUpdate, _) =>
					{
						processUpdate();
						destination.ParentSyncId = source.ParentSyncId;
						return true;
					}
				)
			);
		}

		protected override void SetSyncSettings()
		{
			SyncSettings.AddFilter<RequiredParentEntity>();
		}

		#endregion
	}

	internal sealed class RequiredParentEntity : SyncEntity<int>
	{
		#region Constructors

		public RequiredParentEntity()
		{
		}

		#endregion

		#region Properties

		public int ParentId { get; set; }

		public Guid ParentSyncId { get; set; }

		#endregion
	}

	private sealed class ThrowingApplySyncClient : SampleSyncClient
	{
		#region Fields

		private readonly Exception _exception;

		#endregion

		#region Constructors

		public ThrowingApplySyncClient(
			string name,
			ISyncableDatabaseProvider provider,
			IDateTimeProvider time,
			SyncStatistics statistics,
			Profiler profiler,
			Exception exception
		) : base(name, provider, time, statistics, profiler)
		{
			_exception = exception;
		}

		#endregion

		#region Methods

		protected override SyncClientConverter GetConverter()
		{
			return new SyncClientConverter(
				new SyncObjectConverter<SampleSyncClient, Address, AddressEntity>(update: (_, _, _, _, _) => throw _exception)
			);
		}

		#endregion
	}

	#endregion
}
