#region References

using System;
using System.ComponentModel;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Leak;

[TestClass]
public class PresentationObjectTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingToAttachedPropertyWithAliveSourceDoesNotKeepTargetAlive()
	{
		var source = new StyledElement { Name = "foo" };

		WeakReference SetupBinding()
		{
			var target = new TextBlock();

			target.Bind(TextBlock.TextProperty, new Binding
			{
				Source = source,
				Path = "(Grid.Row)",
				TypeResolver = (_, name) => name == "Grid" ? typeof(Grid) : throw new NotSupportedException()
			});

			return new WeakReference(target);
		}

		var weakTarget = SetupBinding();

		CollectGarbage();
		CornerstoneTest.IsFalse(weakTarget.IsAlive);
	}

	[PresentationTestMethod]
	public void BindingToDirectPropertyDoesNotGetCollected()
	{
		var target = new Class1();

		var setupBinding = () =>
		{
			var source = new Subject<string>();
			var sub = target.Bind((PresentationProperty) Class1.FooProperty, source);
			source.OnNext("foo");
			return new WeakReference(source);
		};

		var weakSource = setupBinding();

		CollectGarbage();

		CornerstoneTest.AreEqual("foo", target.Foo);
		CornerstoneTest.IsTrue(weakSource.IsAlive);
	}

	[PresentationTestMethod]
	public void BindingToDirectPropertyGetsCollectedWhenCompleted()
	{
		var target = new Class1();

		var setupBinding = () =>
		{
			var source = new Subject<string>();
			var sub = target.Bind((PresentationProperty) Class1.FooProperty, source);
			return new WeakReference(source);
		};

		var weakSource = setupBinding();

		var completeSource = () => { ((ISubject<string>) weakSource.Target!).OnCompleted(); };

		completeSource();
		CollectGarbage();
		CornerstoneTest.IsFalse(weakSource.IsAlive);
	}

	[PresentationTestMethod]
	public void CompiledBindingStreamObservableWithAliveSourceDoesNotKeepTargetAlive()
	{
		// Issue #5872: a binding to a long-lived observable via the '^' stream operator should
		// not keep the target alive, in the same way as every other binding type above.
		var observable = new Subject<string>();
		var source = new Class3 { Observable = observable };

		WeakReference SetupBinding()
		{
			var path = new CompiledBindingPathBuilder()
				.Property(
					new ClrPropertyInfo(
						nameof(Class3.Observable),
						target => ((Class3) target).Observable,
						null,
						typeof(IObservable<string>)),
					PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
				.StreamObservable<string>()
				.Build();

			var target = new TextBlock();

			target.Bind(TextBlock.TextProperty, new CompiledBindingExtension
			{
				Source = source,
				Path = path
			});

			observable.OnNext("foo");
			CornerstoneTest.AreEqual("foo", target.Text);

			return new WeakReference(target);
		}

		var weakTarget = SetupBinding();

		CollectGarbage();
		CornerstoneTest.IsFalse(weakTarget.IsAlive);

		// Keep the source and its observable alive to simulate a resource that outlives the target.
		GC.KeepAlive(source);
		GC.KeepAlive(observable);
	}

	[PresentationTestMethod]
	public void CompiledBindingToInpcPropertyWithAliveSourceDoesNotKeepTargetAlive()
	{
		var source = new Class2 { Foo = "foo" };

		WeakReference SetupBinding()
		{
			var path = new CompiledBindingPathBuilder()
				.Property(
					new ClrPropertyInfo(
						nameof(Class2.Foo),
						target => ((Class2) target).Foo,
						(target, value) => ((Class2) target).Foo = (string) value,
						typeof(string)),
					PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
				.Build();

			var target = new TextBlock();

			target.Bind(TextBlock.TextProperty, new CompiledBindingExtension
			{
				Source = source,
				Path = path
			});

			return new WeakReference(target);
		}

		var weakTarget = SetupBinding();

		CollectGarbage();
		CornerstoneTest.IsFalse(weakTarget.IsAlive);
	}

	[PresentationTestMethod]
	public void CompiledBindingToMethodWithAliveSourceDoesNotKeepTargetAlive()
	{
		var source = new Class1();

		WeakReference SetupBinding()
		{
			var path = new CompiledBindingPathBuilder()
				.Command(
					nameof(Class1.DoSomething),
					(o, _) => ((Class1) o).DoSomething(),
					(_, _) => true,
					[])
				.Build();

			var target = new Button();

			target.Bind(Button.CommandProperty, new CompiledBindingExtension
			{
				Source = source,
				Path = path
			});

			return new WeakReference(target);
		}

		var weakTarget = SetupBinding();

		CollectGarbage();
		CornerstoneTest.IsFalse(weakTarget.IsAlive);
	}

	[PresentationTestMethod]
	public void CompiledBindingToPresentationPropertyWithAliveSourceDoesNotKeepTargetAlive()
	{
		var source = new StyledElement { Name = "foo" };

		WeakReference SetupBinding()
		{
			var path = new CompiledBindingPathBuilder()
				.Property(StyledElement.NameProperty, PropertyInfoAccessorFactory.CreatePresentationPropertyAccessor)
				.Build();

			var target = new TextBlock();

			target.Bind(TextBlock.TextProperty, new CompiledBindingExtension
			{
				Source = source,
				Path = path
			});

			return new WeakReference(target);
		}

		var weakTarget = SetupBinding();

		CollectGarbage();
		CornerstoneTest.IsFalse(weakTarget.IsAlive);
	}

	[PresentationTestMethod]
	public void StreamObservableBindingWithAliveTargetKeepsSourceAlive()
	{
		// The weak subscription introduced for #5872 must not collect the source observable
		// while the binding target is still alive: an active binding still needs its source.
		var target = new TextBlock();

		WeakReference SetupBinding()
		{
			var observable = new Subject<string>();
			var source = new Class3 { Observable = observable };

			var path = new CompiledBindingPathBuilder()
				.Property(
					new ClrPropertyInfo(
						nameof(Class3.Observable),
						o => ((Class3) o).Observable,
						null,
						typeof(IObservable<string>)),
					PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
				.StreamObservable<string>()
				.Build();

			target.Bind(TextBlock.TextProperty, new CompiledBindingExtension
			{
				Source = source,
				Path = path
			});

			observable.OnNext("foo");
			CornerstoneTest.AreEqual("foo", target.Text);

			return new WeakReference(observable);
		}

		var weakObservable = SetupBinding();

		CollectGarbage();

		// The target is still alive, so its binding must keep the source observable alive.
		CornerstoneTest.IsTrue(weakObservable.IsAlive);

		GC.KeepAlive(target);
	}

	[PresentationTestMethod]
	public void ToBindingObservableWithAliveSourceDoesNotKeepTargetAlive()
	{
		// Issue #18176 (duplicate of #5872): a binding created from an observable via
		// ToBinding() should not keep the target alive while the observable is alive.
		var observable = new Subject<string>();

		WeakReference SetupBinding()
		{
			var target = new TextBlock();

			target.Bind(TextBlock.TextProperty, observable.ToBinding());

			observable.OnNext("foo");
			CornerstoneTest.AreEqual("foo", target.Text);

			return new WeakReference(target);
		}

		var weakTarget = SetupBinding();

		CollectGarbage();
		CornerstoneTest.IsFalse(weakTarget.IsAlive);

		// Keep the observable alive to simulate a resource that outlives the target.
		GC.KeepAlive(observable);
	}

	private static void CollectGarbage()
	{
		GC.Collect();

		// Forces WeakEvent compact
		Dispatcher.UIThread.RunJobs();
		GC.Collect();
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<Class1, string> FooProperty =
			PresentationProperty.RegisterDirect<Class1, string>(
				"Foo",
				o => o.Foo,
				(o, v) => o.Foo = v,
				"unset");

		private string _foo = "initial2";

		#endregion

		#region Constructors

		static Class1()
		{
		}

		#endregion

		#region Properties

		public string Foo
		{
			get => _foo;
			set => SetAndRaise(FooProperty, ref _foo, value);
		}

		#endregion

		#region Methods

		public void DoSomething()
		{
		}

		#endregion
	}

	private sealed class Class2 : INotifyPropertyChanged
	{
		#region Fields

		private string _foo;

		#endregion

		#region Properties

		public string Foo
		{
			get => _foo;
			set
			{
				if (_foo != value)
				{
					_foo = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Methods

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	private sealed class Class3
	{
		#region Properties

		public IObservable<string> Observable { get; set; }

		#endregion
	}

	#endregion
}