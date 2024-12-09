#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Threading;
using Sample.Shared;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabSpeedyList : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "SpeedyList";

	#endregion

	#region Constructors

	public TabSpeedyList()
	{
		RuntimeInformation = GetService<IRuntimeInformation>();
		Dispatcher = GetService<IDispatcher>();
		LeftList = new SpeedyList<SelectionOption<int>>(Dispatcher);
		MiddleList = new SpeedyList<SelectionOption<int>>(Dispatcher);
		RightList = new SpeedyList<SelectionOption<int>>(Dispatcher);

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

		Limit = 100;
		NumberOfItems = 1000;
		NumberOfThreads = 32;
		SelectedReaderWriterLock = ReaderWriterLockValues[0];
		SelectedTestLoopValue = TestLoopValues[1];
		SelectedThrottleDelay = ThrottleDelayValues[1];
		Progress = 0;

		// Commands
		ClearCommand = new RelayCommand(_ => Clear());
		RandomizeCommand = new RelayCommand(_ => Randomize());
		
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool CancellationPending { get; set; }

	public ICommand ClearCommand { get; }

	public IDispatcher Dispatcher { get; }

	public bool IsRunning { get; set; }

	public SpeedyList<SelectionOption<int>> LeftList { get; }

	public int Limit { get; set; }

	public string ListFilterForLeft { get; set; }

	public string ListFilterForMiddle { get; set; }

	public string ListFilterForRight { get; set; }

	public bool LoopTest { get; set; }

	public string Message { get; set; }

	public SpeedyList<SelectionOption<int>> MiddleList { get; }

	public int NumberOfItems { get; set; }

	public int NumberOfThreads { get; set; }

	public int Progress { get; set; }

	public ICommand RandomizeCommand { get; }

	public SpeedyList<SelectionOption<int>> ReaderWriterLockValues { get; }

	public SpeedyList<SelectionOption<int>> RightList { get; }

	public TimeSpan RunElapsed { get; set; }

	public IRuntimeInformation RuntimeInformation { get; }

	public SelectionOption<int> SelectedReaderWriterLock { get; set; }

	public SelectionOption<int> SelectedTestLoopValue { get; set; }

	public SelectionOption<int> SelectedThrottleDelay { get; set; }

	public SpeedyList<SelectionOption<int>> TestLoopValues { get; }

	public SpeedyList<SelectionOption<int>> ThrottleDelayValues { get; }

	public bool UseLimit { get; set; }

	public bool UseOrder { get; set; }

	#endregion

	#region Methods

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
		}

		base.OnPropertyChanged(propertyName);
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

			LeftList.Load(Enumerable.Range(1, total).Select(x => new SelectionOption<int>(x, x.ToString())));
			MiddleList.Load(Enumerable.Range(total + 1, total).Select(x => new SelectionOption<int>(x, x.ToString())));
			RightList.Load(Enumerable.Range((total * 2) + 1, total).Select(x => new SelectionOption<int>(x, x.ToString())));

			var actions = EnumExtensions.GetEnumValues<SampleViewModel.ListAction>();
			var sources = new[] { LeftList, MiddleList, RightList };
			var destinations = new Dictionary<SpeedyList<SelectionOption<int>>, SpeedyList<SelectionOption<int>>[]>
			{
				{ LeftList, new[] { MiddleList, RightList } },
				{ MiddleList, new[] { LeftList, RightList } },
				{ RightList, new[] { LeftList, MiddleList } }
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
									destination.Insert(0, item);
									break;
								}
								case SampleViewModel.ListAction.Add:
								default:
								{
									destination.Add(item);
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

	#endregion
}