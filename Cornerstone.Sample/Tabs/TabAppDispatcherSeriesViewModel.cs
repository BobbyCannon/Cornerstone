#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Projects model series into view series via TrackSeries (fixed ring + variable derived window).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class TabAppDispatcherSeriesViewModel : DispatchableViewModel
{
	#region Constants

	public const int FixedLength = 60;
	public const int MaxVariableLength = 24;
	public const int MinVariableLength = 2;

	#endregion

	#region Fields

	private int _variableWindow = 8;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public TabAppDispatcherSeriesViewModel()
	{
		ModelSeries = new SeriesDataProvider(FixedLength);
		ViewSeries = new SeriesDataProvider(FixedLength);
		TrackSeries(ModelSeries, ViewSeries);

		// Seed a gentle wave so design-time / first attach has shape.
		for (var i = 0; i < FixedLength; i++)
		{
			ModelSeries.Add((Math.Sin(i / 6.0) * 40) + 50);
		}

		VariableModel = new SpeedyList<double>(MaxVariableLength + 4, true);
		VariableViewSeries = new SeriesDataProvider(MinVariableLength);
		RefillVariableModel(_variableWindow);
		TrackSeries(
			VariableModel,
			() => VariableViewSeries,
			series => VariableViewSeries = series,
			BuildVariableSamples);

		VariableCaption = $"Window: {_variableWindow}";
	}

	#endregion

	#region Properties

	public SeriesDataProvider ModelSeries { get; }

	public partial string VariableCaption { get; set; }

	public SpeedyList<double> VariableModel { get; }

	public partial SeriesDataProvider VariableViewSeries { get; set; }

	public SeriesDataProvider ViewSeries { get; }

	#endregion

	#region Methods

	public void GrowVariableWindow()
	{
		if (_variableWindow >= MaxVariableLength)
		{
			return;
		}

		_variableWindow++;
		RefillVariableModel(_variableWindow);
		VariableCaption = $"Window: {_variableWindow}";
	}

	public void MutateFixedOnce()
	{
		// Off-dispatcher safe: ring Add bumps Version; TrackSeries CopyFrom on apply.
		ModelSeries.Add(Random.Shared.NextDouble() * 100);
	}

	public void MutateVariableValues()
	{
		if (VariableModel.Count == 0)
		{
			RefillVariableModel(_variableWindow);
			return;
		}

		for (var i = 0; i < VariableModel.Count; i++)
		{
			VariableModel[i] = Random.Shared.NextDouble() * 100;
		}

		VariableCaption = $"Window: {_variableWindow} (values)";
	}

	public void ShrinkVariableWindow()
	{
		if (_variableWindow <= MinVariableLength)
		{
			return;
		}

		_variableWindow--;
		RefillVariableModel(_variableWindow);
		VariableCaption = $"Window: {_variableWindow}";
	}

	private double[] BuildVariableSamples()
	{
		var count = VariableModel.Count;
		if (count == 0)
		{
			return [];
		}

		var values = new double[count];
		for (var i = 0; i < count; i++)
		{
			values[i] = VariableModel[i];
		}

		return values;
	}

	private void RefillVariableModel(int count)
	{
		var items = new double[count];
		for (var i = 0; i < count; i++)
		{
			items[i] = (Math.Cos(i / 3.0) * 30) + 50 + (i * 0.5);
		}

		VariableModel.Load(items);
	}

	#endregion
}