#region References

using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Generators;
using Cornerstone.Logging;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.ViewModels;
using Cornerstone.Sync;
using Sample.Client.Data;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabSyncManager : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Sync Manager";

	#endregion

	#region Fields

	private readonly DebounceService _createAccounts;
	private readonly DebounceService _syncAccounts;

	#endregion

	#region Constructors

	public TabSyncManager() : this(
		DesignModeDependencyProvider.Get<IDateTimeProvider>(),
		DesignModeDependencyProvider.Get<IRuntimeInformation>(),
		DesignModeDependencyProvider.Get<IDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabSyncManager(IDateTimeProvider timeProvider, IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
	{
		_createAccounts = new DebounceService(TimeSpan.Zero, CreateAccounts);
		_syncAccounts = new DebounceService(TimeSpan.Zero, SyncAccounts);

		Dispatcher = dispatcher;
		TimeProvider = timeProvider;
		SyncSessionManager = new SyncSession(null, Guid.Empty, string.Empty, timeProvider, dispatcher);
		Progress = new ProgressTracker(dispatcher);
		RuntimeInformation = runtimeInformation;
		NumberOfItems = 1;
		MemoryTrackerRepository = new MemoryTrackerRepository(dispatcher);
		Tracker = Tracker.Start(MemoryTrackerRepository, timeProvider, dispatcher);

		ClientDatabase = new ClientMemoryDatabase();
		ClientDatabaseProvider = new SyncableDatabaseProvider2<IClientDatabase>(() => ClientDatabase);
		ClientAccounts = new SpeedyList<ClientAccount>(Dispatcher);

		ServerDatabase = new ClientMemoryDatabase();
		ServerDatabaseProvider = new SyncableDatabaseProvider2<IClientDatabase>(() => ServerDatabase);
		ServerAccounts = new SpeedyList<ClientAccount>(Dispatcher);

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public SpeedyList<ClientAccount> ClientAccounts { get; }

	public ClientMemoryDatabase ClientDatabase { get; private set; }

	public SyncableDatabaseProvider2<IClientDatabase> ClientDatabaseProvider { get; set; }

	public IDispatcher Dispatcher { get; }

	public MemoryTrackerRepository MemoryTrackerRepository { get; }

	public int NumberOfItems { get; set; }

	public ProgressTracker Progress { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	public SpeedyList<ClientAccount> ServerAccounts { get; }

	public ClientMemoryDatabase ServerDatabase { get; private set; }

	public SyncableDatabaseProvider2<IClientDatabase> ServerDatabaseProvider { get; set; }

	public SyncSession SyncSessionManager { get; }

	public IDateTimeProvider TimeProvider { get; }

	public Tracker Tracker { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (Design.IsDesignMode)
		{
			MemoryTrackerRepository.Paths.Add(
				new TrackerPath
				{
					Name = "Foo",
					StartedOn = TimeProvider.UtcNow
						.AddSeconds(-15)
						.AddMilliseconds(459),
					CompletedOn = TimeProvider.UtcNow
				}
			);
		}

		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		_createAccounts.Cancel();
		_syncAccounts.Cancel();
		base.OnUnloaded(e);
	}

	private void CreateAccounts(CancellationToken token)
	{
		try
		{
			Progress.IsProgressing = true;
			ClientDatabase = new ClientMemoryDatabase();
			ClientAccounts.Clear();
			ServerDatabase = new ClientMemoryDatabase();
			ServerAccounts.Clear();

			using var path = Tracker.StartPath("Create Accounts");
			Progress.Update(0, 0, NumberOfItems * 1000);

			for (var i = 0; i < Progress.Maximum; i++)
			{
				var name = RandomGenerator.GetFullName();
				var account = new ClientAccount
				{
					EmailAddress = RandomGenerator.GetEmailAddress(name),
					Name = name,
					Roles = string.Empty
				};

				ServerDatabase.Accounts.Add(account);
				Progress.Update(i);

				if (token.IsCancellationRequested)
				{
					return;
				}
			}

			ServerDatabase.SaveChanges();

			path.Complete();

			Tracker.RunPath("Load Accounts", _ => ServerAccounts.Load(ServerDatabase.Accounts));
		}
		catch (Exception ex)
		{
			Tracker.AddException(ex);
		}
		finally
		{
			Progress.IsProgressing = false;
		}
	}

	private void CreateAccountsOnClick(object sender, RoutedEventArgs e)
	{
		_createAccounts.Trigger();
	}

	private void SyncAccounts(CancellationToken token)
	{
		//try
		//{
		//	Progress.IsProgressing = true;
		//	ClientDatabase.Accounts.Remove(x => x.Id > 0);
		//	ClientDatabase.SaveChanges();
		//	ClientAccounts.Clear();

		//	using var path = Tracker.StartPath("Sync All");
		//	var serverProvider = new SyncClientFromDatabaseProvider("Server", ServerDatabaseProvider, TimeProvider, true);
		//	var manager = new SampleSyncManager(ClientDatabaseProvider, serverProvider, RuntimeInformation, TimeProvider, Dispatcher);
		//	var result = manager.SyncAll();
		//	path.Complete();

		//	var builder = new TextBuilder();
		//	builder.AppendLine("Session: " + result.Elapsed);
		//	builder.AppendLine(result.SyncClientProfilerForClient.ToString(result.Elapsed));
		//	this.Dispatch(() => SyncStatus.Text = builder.ToString());

		//	SyncSession.UpdateWith(result);
		//	ClientAccounts.Load(ClientDatabase.Accounts);
		//}
		//catch (Exception ex)
		//{
		//	Tracker.AddException(ex);
		//}
		//finally
		//{
		//	Progress.IsProgressing = false;
		//}
	}

	private void SyncAccountsOnClick(object sender, RoutedEventArgs e)
	{
		_syncAccounts.Trigger();
	}

	#endregion
}