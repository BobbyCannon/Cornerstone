#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Threading;
using Sample.Shared;
using Timer = Cornerstone.Profiling.Timer;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabSpeedyList : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Speedy List";

	#endregion

	#region Constructors

	public TabSpeedyList()
	{
		RuntimeInformation = GetInstance<IRuntimeInformation>();
		Dispatcher = GetInstance<IDispatcher>();
		LeftList = new SpeedyList<SelectionOption<int>>(Dispatcher);
		LeftListFilterProxy = new FilteredSpeedyList<SelectionOption<int>>(LeftList, x => (x.Id % 2) == 0);
		MiddleList = new SpeedyList<SelectionOption<int>>(Dispatcher);
		MiddleListFilterProxy = new FilteredSpeedyList<SelectionOption<int>>(MiddleList, x => (x.Id % 3) == 0);
		RightList = new SpeedyList<SelectionOption<int>>(Dispatcher);
		RightListFilterProxy = new FilteredSpeedyList<SelectionOption<int>>(RightList, x => (x.Id % 10) == 0);
		States = new SpeedyList<string>(RandomGenerator.GetStates())
		{
			FilterCheck = x => string.IsNullOrEmpty(StateFilter) || (x?.IndexOf(StateFilter, StringComparison.OrdinalIgnoreCase) >= 0)
		};

		ReaderWriterLockValues =
		[
			new(0, "Tiny"),
			new(1, "Slim")
		];

		TestLoopValues =
		[
			new(100, "100 (tiny)"),
			new(1000, "1000 (small)"),
			new(10000, "10k (medium)"),
			new(100000, "100k (large)"),
			new(1000000, "1m (x-large)")
		];

		ThrottleDelayValues =
		[
			new(0, "No Delay"),
			new(5, "5 ms"),
			new(50, "50 ms"),
			new(500, "500 ms"),
			new(1000, "1000 ms")
		];

		GenerateDataTimer = new Timer();
		AddAverage = new AverageTimer();
		InsertAverage = new AverageTimer();
		Limit = 100;
		NumberOfItems = 100;
		NumberOfThreads = 32;
		SelectedReaderWriterLock = ReaderWriterLockValues[0];
		SelectedTestLoopValue = TestLoopValues[1];
		SelectedThrottleDelay = ThrottleDelayValues[0];
		Progress = 0;

		// Commands
		ClearCommand = new RelayCommand(_ => Clear());
		RandomizeCommand = new RelayCommand(_ => Randomize());

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public AverageTimer AddAverage { get; }

	public bool CancellationPending { get; set; }

	public ICommand ClearCommand { get; }

	public IDispatcher Dispatcher { get; }

	public Timer GenerateDataTimer { get; }

	public AverageTimer InsertAverage { get; }

	public bool IsRunning { get; set; }

	public SpeedyList<SelectionOption<int>> LeftList { get; }

	public FilteredSpeedyList<SelectionOption<int>> LeftListFilterProxy { get; }

	public int Limit { get; set; }

	public string ListFilterForLeft { get; set; }

	public string ListFilterForMiddle { get; set; }

	public string ListFilterForRight { get; set; }

	public bool LoopTest { get; set; }

	public string Message { get; set; }

	public SpeedyList<SelectionOption<int>> MiddleList { get; }

	public FilteredSpeedyList<SelectionOption<int>> MiddleListFilterProxy { get; }

	public int NumberOfItems { get; set; }

	public int NumberOfThreads { get; set; }

	public int Progress { get; set; }

	public ICommand RandomizeCommand { get; }

	public SpeedyList<SelectionOption<int>> ReaderWriterLockValues { get; }

	public SpeedyList<SelectionOption<int>> RightList { get; }

	public FilteredSpeedyList<SelectionOption<int>> RightListFilterProxy { get; }

	public TimeSpan RunElapsed { get; set; }

	public IRuntimeInformation RuntimeInformation { get; }

	public SelectionOption<int> SelectedReaderWriterLock { get; set; }

	public SelectionOption<int> SelectedTestLoopValue { get; set; }

	public SelectionOption<int> SelectedThrottleDelay { get; set; }

	public string StateFilter { get; set; }

	public SpeedyList<string> States { get; }

	public SpeedyList<SelectionOption<int>> TestLoopValues { get; }

	public SpeedyList<SelectionOption<int>> ThrottleDelayValues { get; }

	public bool UseLimit { get; set; }

	public bool UseOrder { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(ListFilterForLeft):
			{
				LeftList.FilterCheck = string.IsNullOrWhiteSpace(ListFilterForLeft)
					? null
					: x => x.Name?.Contains(ListFilterForLeft) ?? false;
				LeftList.RefreshFilter();
				break;
			}
			case nameof(ListFilterForMiddle):
			{
				MiddleList.FilterCheck = string.IsNullOrWhiteSpace(ListFilterForMiddle)
					? null
					: x => x.Name?.Contains(ListFilterForMiddle) ?? false;
				MiddleList.RefreshFilter();
				break;
			}
			case nameof(ListFilterForRight):
			{
				RightList.FilterCheck = string.IsNullOrWhiteSpace(ListFilterForRight)
					? null
					: x => x.Name?.Contains(ListFilterForRight) ?? false;
				RightList.RefreshFilter();
				break;
			}
			case nameof(StateFilter):
			{
				States.RefreshFilter();
				break;
			}
			case nameof(UseOrder):
			{
				States.OrderBy = UseOrder ? [new OrderBy<string>(x => x)] : null;
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (Design.IsDesignMode)
		{
			LeftList.Add(new SelectionOption<int>(1, "One"));
			LeftList.Add(new SelectionOption<int>(2, "Two"));

			MiddleList.Add(new SelectionOption<int>(3, "Three"));
			MiddleList.Add(new SelectionOption<int>(4, "Four"));

			RightList.Add(new SelectionOption<int>(5, "Five"));
			RightList.Add(new SelectionOption<int>(6, "Six"));
		}

		base.OnLoaded(e);
	}

	private void Clear()
	{
		LeftList.Clear();
		MiddleList.Clear();
		RightList.Clear();
	}

	private IReaderWriterLock GetLockable()
	{
		return SelectedReaderWriterLock.Id switch
		{
			1 => new ReaderWriterLockSlimProxy(),
			_ => new ReaderWriterLockTiny()
		};
	}

	private void Process()
	{
		IsRunning = true;
		CancellationPending = false;
		Message = "Starting...\r\n";
		Message += $"\tThrottle Delay: {SelectedThrottleDelay.Id} ms";
		Progress = 0;

		var watch = Stopwatch.StartNew();
		var maximum = SelectedTestLoopValue.Id;

		try
		{
			LeftList.UpdateLock(GetLockable());
			LeftList.Clear();
			LeftList.Limit = UseLimit ? Limit : int.MaxValue;
			LeftList.OrderBy = UseOrder ? [new OrderBy<SelectionOption<int>>(x => x.Id)] : null;
			MiddleList.Clear();
			MiddleList.Limit = UseLimit ? Limit : int.MaxValue;
			MiddleList.OrderBy = UseOrder ? [new OrderBy<SelectionOption<int>>(x => x.Id)] : null;
			RightList.Clear();
			RightList.Limit = UseLimit ? Limit : int.MaxValue;
			RightList.OrderBy = UseOrder ? [new OrderBy<SelectionOption<int>>(x => x.Id)] : null;

			var total = NumberOfItems;
			var minimum = Math.Max(10, (int) (total * 0.1));

			GenerateDataTimer.Restart();
			var left = Enumerable.Range(1, total).Select(x => new SelectionOption<int>(x, x.ToString())).ToArray();
			var middle = Enumerable.Range(total + 1, total).Select(x => new SelectionOption<int>(x, x.ToString())).ToArray();
			var right = Enumerable.Range((total * 2) + 1, total).Select(x => new SelectionOption<int>(x, x.ToString())).ToArray();
			GenerateDataTimer.Stop();

			LeftList.Load(left);
			MiddleList.Load(middle);
			RightList.Load(right);

			var actions = EnumExtensions.GetEnumValues<SampleViewModel.ListAction>();
			var sources = new[] { LeftList, MiddleList, RightList };
			var destinations = new Dictionary<SpeedyList<SelectionOption<int>>, SpeedyList<SelectionOption<int>>[]>
			{
				{ LeftList, [MiddleList, RightList] },
				{ MiddleList, [LeftList, RightList] },
				{ RightList, [LeftList, MiddleList] }
			};

			try
			{
				var options = new ParallelOptions
				{
					MaxDegreeOfParallelism = NumberOfThreads
				};

				Parallel.For(0, maximum, options, _ =>
				{
					if (CancellationPending)
					{
						return;
					}

					var source = RandomGenerator.GetItem(sources);
					var destination = RandomGenerator.GetItem(destinations[source]);
					var action = RandomGenerator.GetItem(actions);

					if (source.Count > minimum)
					{
						var index = RandomGenerator.NextInteger(0, source.Count);
						if (source.TryGetAndRemoveAt(index, out var item))
						{
							switch (action)
							{
								case SampleViewModel.ListAction.Insert:
								{
									InsertAverage.Time(() => destination.Insert(0, item));
									break;
								}
								case SampleViewModel.ListAction.Add:
								default:
								{
									AddAverage.Time(() => destination.Add(item));
									break;
								}
							}
						}
					}

					Progress++;

					if (SelectedThrottleDelay.Id > 0)
					{
						Thread.Sleep(SelectedThrottleDelay.Id);
					}
				});
			}
			catch (Exception ex)
			{
				Message = ex.ToDetailedString();
			}
		}
		finally
		{
			RunElapsed = watch.Elapsed;
			Message += $"\r\nDone {RunElapsed}";
			Progress = maximum;
			IsRunning = false;
		}
	}

	private void Randomize()
	{
		if (IsRunning)
		{
			CancellationPending = true;
			return;
		}

		Task.Run(Process, CancellationToken.None)
			.ContinueWith(_ =>
			{
				if (LoopTest && !CancellationPending)
				{
					Randomize();
				}
			});
	}

	#endregion
}