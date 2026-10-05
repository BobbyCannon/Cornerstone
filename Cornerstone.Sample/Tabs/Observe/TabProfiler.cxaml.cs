#region References

using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Controls.TreeDataGrid;
using Cornerstone.Presentation.Controls.TreeDataGrid.Columns;
using Cornerstone.Presentation.Controls.TreeDataGrid.Models;
using Cornerstone.Presentation.Threading;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Observe;

[SourceReflection]
public partial class TabProfiler : UserControl
{
	#region Constants

	public const string HeaderName = "Profiler";

	#endregion

	#region Fields

	private readonly ClipboardService _clipboardService;
	private bool _hasStartupProfile;
	private StartupProfileDetail _startupProfileFilter;
	private readonly PresentationList<StartupProfileItem> _startupProfileItems;

	#endregion

	#region Constructors

	public TabProfiler()
		: this(AppBootstrap.GetInstance<ClipboardService>(), AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabProfiler(ClipboardService clipboardService, IRuntimeInformation runtimeInformation)
	{
		_clipboardService = clipboardService;
		RuntimeInformation = runtimeInformation;
		_startupProfileItems = new PresentationList<StartupProfileItem>();
		_startupProfileFilter = StartupProfileDetail.Slow;

		StartupProfile = new HierarchicalTreeDataGridSource<StartupProfileItem>(_startupProfileItems)
		{
			Columns =
			{
				new TextColumn<StartupProfileItem, string>("Elapsed", x => x.Elapsed, null, new GridLength(100, GridUnitType.Pixel), null, new TextColumnOptions<StartupProfileItem> { TextAlignment = TextAlignment.Right }),
				new TextColumn<StartupProfileItem, string>("%", x => x.Percent, null, new GridLength(100, GridUnitType.Pixel), null, new TextColumnOptions<StartupProfileItem> { TextAlignment = TextAlignment.Right }),
				new HierarchicalExpanderColumn<StartupProfileItem>(
					new TextColumn<StartupProfileItem, string>("Name", x => x.Name, null, new GridLength(1, GridUnitType.Star)),
					x => x.Children,
					x => (x.Children != null) && (x.Children.Count > 0),
					x => x.IsExpanded
				)
			}
		};

		if (Design.IsDesignMode && AppBootstrap.StartupProfiler is not { IsCompleted: true })
		{
			AppBootstrap.StartupProfiler = CreateDesignStartupProfiler();
		}

		DataContext = this;
		InitializeComponent();
		RefreshStartupProfile();
	}

	#endregion

	#region Properties

	public string EmptyMessage =>
		AppBootstrap.StartupProfiler is { IsCompleted: false }
			? "Startup profiling is still running. It completes after the first window loads."
			: "No startup profile yet.";

	public bool HasStartupProfile
	{
		get => _hasStartupProfile;
		private set
		{
			if (_hasStartupProfile == value)
			{
				return;
			}

			_hasStartupProfile = value;
			OnPropertyChanged(nameof(HasStartupProfile));
		}
	}

	public IRuntimeInformation RuntimeInformation { get; }

	public HierarchicalTreeDataGridSource<StartupProfileItem> StartupProfile { get; }

	public StartupProfileDetail StartupProfileFilter
	{
		get => _startupProfileFilter;
		set
		{
			if (_startupProfileFilter == value)
			{
				return;
			}

			_startupProfileFilter = value;
			OnPropertyChanged(nameof(StartupProfileFilter));
			RefreshStartupProfile();
		}
	}

	public IReadOnlyList<StartupProfileDetail> StartupProfileFilterOptions { get; } =
	[
		StartupProfileDetail.All,
		StartupProfileDetail.Slow,
		StartupProfileDetail.Slowest
	];

	#endregion

	#region Methods

	public void RefreshStartupProfile()
	{
		var profiler = AppBootstrap.StartupProfiler;
		if (profiler is not { IsCompleted: true })
		{
			HasStartupProfile = false;
			_startupProfileItems.Clear();
			OnPropertyChanged(nameof(EmptyMessage));
			return;
		}

		_startupProfileItems.Load(StartupProfileItem.FromProfiler(profiler, StartupProfileFilter));
		HasStartupProfile = _startupProfileItems.Count > 0;
		OnPropertyChanged(nameof(EmptyMessage));
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		if (AppBootstrap.StartupProfiler is { IsCompleted: false })
		{
			Dispatcher.UIThread.Post(RefreshStartupProfile, DispatcherPriority.Background);
			return;
		}

		RefreshStartupProfile();
	}

	[RelayCommand]
	private void CollapseStartupProfile()
	{
		SetStartupProfileExpanded(_startupProfileItems, false);
	}

	[RelayCommand]
	private void CopyStartupProfile()
	{
		var profiler = AppBootstrap.StartupProfiler;
		if (profiler is not { IsCompleted: true })
		{
			return;
		}

		_clipboardService.SetTextAsync(GetStartupProfileText(StartupProfileFilter));
	}

	[RelayCommand]
	private void CopyStartupProfileAll()
	{
		var profiler = AppBootstrap.StartupProfiler;
		if (profiler is not { IsCompleted: true })
		{
			return;
		}

		_clipboardService.SetTextAsync(GetStartupProfileText(StartupProfileDetail.All));
	}

	private static StartupProfiler CreateDesignStartupProfiler()
	{
		var ticks = DateTime.UtcNow.Ticks;
		var provider = new DateTimeProvider(() => new DateTime(ticks, DateTimeKind.Utc));

		void Advance(int milliseconds)
		{
			ticks += TimeSpan.FromMilliseconds(milliseconds).Ticks;
		}

		var profiler = new StartupProfiler(provider);
		using (profiler.Start("AppBootstrap.Initialize"))
		{
			using (profiler.Start("RuntimeInformation"))
			{
				Advance(40);
			}
			using (profiler.Start("Dependencies"))
			{
				Advance(8);
			}
			using (profiler.Start("Platform"))
			{
				Advance(12);
			}
		}
		using (profiler.Start("App.RegisterServices"))
		{
			Advance(16);
		}
		using (profiler.Start("App.RegisterApplicationServices"))
		{
			Advance(22);
		}
		using (profiler.Start("App.XamlAndSerializers"))
		{
			Advance(180);
		}
		using (profiler.Start("Application.Keystone.Resolve"))
		{
			Advance(40);
		}
		using (profiler.Start("MainWindow.Create"))
		{
			Advance(90);
		}
		using (profiler.Start("MainWindow.Show"))
		{
			Advance(20);
		}
		Advance(30);
		profiler.Complete();
		return profiler;
	}

	[RelayCommand]
	private void ExpandStartupProfile()
	{
		SetStartupProfileExpanded(_startupProfileItems, true);
	}

	private string GetStartupProfileText(StartupProfileDetail detail)
	{
		var runtime = RuntimeInformation;
		var builder = new StringBuilder();
		builder.AppendLine($"{runtime.ApplicationName} {runtime.ApplicationVersion}");
		builder.AppendLine($"Profile filter: {detail}");
		var profiler = AppBootstrap.StartupProfiler;
		if (profiler is { IsCompleted: true })
		{
			builder.AppendLine();
			builder.Append(profiler.ToReport(detail));
		}

		return builder.ToString();
	}

	private static void SetStartupProfileExpanded(IEnumerable<StartupProfileItem> items, bool isExpanded)
	{
		if (items == null)
		{
			return;
		}

		foreach (var item in items)
		{
			item.IsExpanded = isExpanded;
			SetStartupProfileExpanded(item.Children, isExpanded);
		}
	}

	#endregion
}