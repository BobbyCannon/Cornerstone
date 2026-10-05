#region References

using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class RangeBaseTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingDataContextShouldNotChangeOldDataContext()
	{
		var viewModel = new RangeTestViewModel
		{
			Minimum = -5000,
			Maximum = 5000,
			Value = 4000
		};

		var target = new TestRange
		{
			[!RangeBase.MinimumProperty] = new Binding(nameof(viewModel.Minimum)),
			[!RangeBase.MaximumProperty] = new Binding(nameof(viewModel.Maximum)),
			[!RangeBase.ValueProperty] = new Binding(nameof(viewModel.Value))
		};

		var root = new TestRoot(target);
		target.DataContext = viewModel;
		target.DataContext = null;

		CornerstoneTest.AreEqual(4000, viewModel.Value);
		CornerstoneTest.AreEqual(-5000, viewModel.Minimum);
		CornerstoneTest.AreEqual(5000, viewModel.Maximum);
	}

	[PresentationTestMethod]
	public void ChangingMaximumShouldCoerceValue()
	{
		var target = new TestRange
		{
			Minimum = 0,
			Maximum = 100,
			Value = 100
		};
		var root = new TestRoot(target);

		target.Maximum = 50;

		CornerstoneTest.AreEqual(0, target.Minimum);
		CornerstoneTest.AreEqual(50, target.Maximum);
		CornerstoneTest.AreEqual(50, target.Value);
	}

	[PresentationTestMethod]
	public void ChangingMinimumShouldCoerceValueAndMaximum()
	{
		var target = new TestRange
		{
			Minimum = 0,
			Maximum = 100,
			Value = 50
		};
		var root = new TestRoot(target);

		target.Minimum = 200;

		CornerstoneTest.AreEqual(200, target.Minimum);
		CornerstoneTest.AreEqual(200, target.Maximum);
		CornerstoneTest.AreEqual(200, target.Value);
	}

	[PresentationTestMethod]
	public void CoercionShouldBeDoneAfterInitialization()
	{
		var target = new TestRange();

		target.BeginInit();

		var root = new TestRoot(target);
		target.Minimum = 1;

		target.EndInit();

		CornerstoneTest.AreEqual(1, target.Value);
	}

	[PresentationTestMethod]
	public void CoercionShouldNotBeDoneDuringInitialization()
	{
		var target = new TestRange();

		target.BeginInit();

		var root = new TestRoot(target);
		target.Minimum = 1;
		CornerstoneTest.AreEqual(0, target.Value);

		target.Value = 50;
		target.EndInit();

		CornerstoneTest.AreEqual(50, target.Value);
	}

	[PresentationTestMethod]
	public void MaximumShouldBeCoercedToMinimum()
	{
		var target = new TestRange
		{
			Minimum = 100,
			Maximum = 50
		};
		var root = new TestRoot(target);

		CornerstoneTest.AreEqual(100, target.Minimum);
		CornerstoneTest.AreEqual(100, target.Maximum);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void SetValueShouldNotCauseStackOverflow(bool useXamlBinding)
	{
		var viewModel = new TestStackOverflowViewModel
		{
			Value = 50
		};

		Track track = null;

		var target = new TestRange
		{
			Template = new FuncControlTemplate<RangeBase>((c, scope) =>
			{
				track = new Track
				{
					Width = 100,
					Orientation = Orientation.Horizontal,
					[~~Track.MinimumProperty] = c[~~RangeBase.MinimumProperty],
					[~~Track.MaximumProperty] = c[~~RangeBase.MaximumProperty],

					Name = "PART_Track",
					Thumb = new Thumb()
				}.RegisterInNameScope(scope);

				if (useXamlBinding)
				{
					track.Bind(Track.ValueProperty, new Binding("Value")
					{
						Mode = BindingMode.TwoWay,
						Source = c,
						Priority = BindingPriority.Style
					});
				}
				else
				{
					track[~~Track.ValueProperty] = c[~~RangeBase.ValueProperty];
				}

				return track;
			}),
			Minimum = 0,
			Maximum = 100,
			DataContext = viewModel
		};

		target.Bind(TestRange.ValueProperty, new Binding("Value") { Mode = BindingMode.TwoWay });

		target.ApplyTemplate();
		track!.Measure(new Size(100, 0));
		track.Arrange(new Rect(0, 0, 100, 0));

		CornerstoneTest.AreEqual(1, viewModel.SetterInvokedCount);

		// Issues #855 and #824 were causing a StackOverflowException at this point.
		target.Value = 51.001;

		CornerstoneTest.AreEqual(2, viewModel.SetterInvokedCount);

		double expected = 51;

		CornerstoneTest.AreEqual(expected, viewModel.Value);
		CornerstoneTest.AreEqual(expected, target.Value);
		CornerstoneTest.AreEqual(expected, track.Value);
	}

	[PresentationTestMethod]
	public void ValueShouldBeCoercedToRange()
	{
		var target = new TestRange
		{
			Minimum = 0,
			Maximum = 50,
			Value = 100
		};
		var root = new TestRoot(target);

		CornerstoneTest.AreEqual(0, target.Minimum);
		CornerstoneTest.AreEqual(50, target.Maximum);
		CornerstoneTest.AreEqual(50, target.Value);
	}

	#endregion

	#region Classes

	private class RangeTestViewModel
	{
		#region Properties

		public double Maximum { get; set; }
		public double Minimum { get; set; }
		public double Value { get; set; }

		#endregion
	}

	private class TestRange : RangeBase
	{
	}

	private class TestStackOverflowViewModel : INotifyPropertyChanged
	{
		#region Constants

		public const int MaxInvokedCount = 1000;

		#endregion

		#region Fields

		private double _value;

		#endregion

		#region Properties

		public int SetterInvokedCount { get; private set; }

		public double Value
		{
			get => _value;
			set
			{
				if (_value != value)
				{
					SetterInvokedCount++;
					if (SetterInvokedCount < MaxInvokedCount)
					{
						_value = (int) value;
						if (_value > 75)
						{
							_value = 75;
						}
						if (_value < 25)
						{
							_value = 25;
						}
					}
					else
					{
						_value = value;
					}

					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
				}
			}
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	#endregion
}