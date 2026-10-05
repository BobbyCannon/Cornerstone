#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsDelay : ScopedTestBase, IDisposable
{
	#region Constants

	private const int DelayMilliseconds = 10;
	private const string InitialFooValue = "foo";

	#endregion

	#region Fields

	private readonly IDisposable _app;
	private readonly Binding _binding;
	private readonly BindingExpressionBase _bindingExpr;

	private readonly ManualTimerDispatcher _dispatcher;
	private readonly BindingTests.Source _source;
	private readonly TextBox _target;

	#endregion

	#region Constructors

	public BindingTestsDelay()
	{
		_app = UnitTestApplication.Start(new(keyboardDevice: () => new KeyboardDevice()));
		_dispatcher = new ManualTimerDispatcher();
		_ = new Dispatcher(_dispatcher);

		_source = new BindingTests.Source { Foo = InitialFooValue };
		_target = new TextBox { DataContext = _source };
		_binding = new Binding(nameof(_source.Foo))
		{
			Mode = BindingMode.TwoWay,
			Delay = DelayMilliseconds
		};

		_bindingExpr = _target.Bind(TextBox.TextProperty, _binding);

		CornerstoneTest.AreEqual(_source.Foo, _target.Text);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void DelayedBindingOneWayToSourceDataContextChangeShouldUpdateSourceImmediately()
	{
		_target.Bind(TextBlock.TextProperty, new Binding(nameof(_source.Foo))
		{
			Mode = BindingMode.OneWayToSource,
			Delay = DelayMilliseconds
		});

		_target.Text = "bar";

		var newSource = new BindingTests.Source();

		_target.DataContext = newSource;

		CornerstoneTest.AreEqual("bar", newSource.Foo);
	}

	[PresentationTestMethod]
	public void DelayedBindingShouldNotExecuteIfValueReturnsToOriginal()
	{
		_target.Text = "bar";
		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);

		SetTimeAndExecuteTimers(DelayMilliseconds / 2);

		_target.Text = InitialFooValue;

		SetTimeAndExecuteTimers(DelayMilliseconds * 2);

		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);
		CornerstoneTest.AreEqual(1, _source.FooSetCount);
	}

	[PresentationTestMethod]
	public void DelayedBindingShouldNotSetValueAfterBeingDisposed()
	{
		_target.Text = "bar";
		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);

		_bindingExpr.Dispose();

		SetTimeAndExecuteTimers(DelayMilliseconds + 1);

		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);
	}

	[PresentationTestMethod]
	public void DelayedBindingShouldRestartIfValueChangesDuringDelay()
	{
		_target.Text = "bar";
		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);

		SetTimeAndExecuteTimers(DelayMilliseconds / 2);

		_target.Text = "baz";

		SetTimeAndExecuteTimers(DelayMilliseconds + 1); // we set a new value half-way through the delay, so the delay is still in effect at this timestamp

		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);

		SetTimeAndExecuteTimers(DelayMilliseconds * 2);

		CornerstoneTest.AreEqual("baz", _source.Foo);
	}

	[PresentationTestMethod]
	public void DelayedBindingShouldSetValueOnlyAfterDelayElapsed()
	{
		_target.Text = "bar";
		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);

		SetTimeAndExecuteTimers(DelayMilliseconds / 2);
		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);

		SetTimeAndExecuteTimers(DelayMilliseconds + 1);

		CornerstoneTest.AreEqual("bar", _source.Foo);
	}

	[PresentationTestMethod]
	public void DelayedBindingShouldUpdateTargetImmediately()
	{
		_source.Foo = "bar";
		CornerstoneTest.AreEqual("bar", _target.Text);
	}

	[PresentationTestMethod]
	public void DelayedBindingUpdateSourceCallShouldUpdateSourceImmediately()
	{
		_target.Text = "bar";
		_bindingExpr.UpdateSource();

		CornerstoneTest.AreEqual("bar", _source.Foo);
	}

	[PresentationTestMethod]
	public void DelayedBindingUpdateTriggerLostFocusShouldUpdateSourceImmediately()
	{
		var secondBox = new TextBox();

		new TestRoot { Child = new Panel { Children = { _target, secondBox } } };

		_target.Bind(TextBox.TextProperty, new Binding(nameof(_source.Foo))
		{
			Mode = BindingMode.TwoWay,
			Delay = DelayMilliseconds,
			UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
		});

		CornerstoneTest.IsTrue(_target.Focus());
		_target.Text = "bar";

		CornerstoneTest.AreEqual(InitialFooValue, _source.Foo);

		CornerstoneTest.IsTrue(secondBox.Focus());
		CornerstoneTest.AreEqual("bar", _source.Foo);
	}

	public override void Dispose()
	{
		_app.Dispose();
		base.Dispose();
	}

	private void SetTimeAndExecuteTimers(long time)
	{
		_dispatcher.Now = time;
		_dispatcher.RaiseTimerEvent();
	}

	#endregion

	#region Classes

	private class ManualTimerDispatcher : IDispatcherImpl
	{
		#region Properties

		public bool CurrentThreadIsLoopThread => true;
		public long Now { get; set; }

		#endregion

		#region Methods

		public void RaiseTimerEvent()
		{
			Timer?.Invoke();
		}

		public void Signal()
		{
			Signaled?.Invoke();
		}

		public void UpdateTimer(long? dueTimeInMs)
		{
		}

		#endregion

		#region Events

		public event Action Signaled;
		public event Action Timer;

		#endregion
	}

	#endregion
}