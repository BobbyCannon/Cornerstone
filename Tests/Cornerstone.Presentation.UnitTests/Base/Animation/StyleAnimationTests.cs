#region References

using System;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Animation;

#region References

using Animation = Presentation.Animation.Animation;

#endregion

[TestClass]
public class StyleAnimationTests
{
	#region Methods

	[PresentationTestMethod]
	public void ApplicationControlThemeAppliesAnimation()
	{
		using var app = new AnimationTestApplication
		{
			Resources =
			{
				{ typeof(Button), CreateControlTheme<Button>(CreateOpacityAnimation()) }
			}
		};

		var target = new Button();
		var root = CreateRoot(target, app);

		app.Clock.Pulse(TimeSpan.Zero);
		CornerstoneTest.AreEqual(1, target.Opacity);

		app.Clock.Pulse(TimeSpan.FromSeconds(0.5));
		CornerstoneTest.AreEqual(0.5, target.Opacity);

		app.Clock.Pulse(TimeSpan.FromSeconds(1));
		CornerstoneTest.AreEqual(0, target.Opacity);
	}

	[PresentationTestMethod]
	public void ApplicationControlThemeAppliesAnimationWithBoundKeyFrameValues()
	{
		var fromBinding = new Binding("From");
		var toBinding = new Binding("To");

		using var app = new AnimationTestApplication
		{
			Resources =
			{
				{ typeof(Button), CreateControlTheme<Button>(CreateOpacityAnimation(fromBinding, toBinding)) }
			}
		};

		var target = new Button { DataContext = new KeyFrameValues(1.0, 0.0) };
		var root = CreateRoot(target, app);

		app.Clock.Pulse(TimeSpan.Zero);
		CornerstoneTest.AreEqual(1, target.Opacity);

		app.Clock.Pulse(TimeSpan.FromSeconds(0.5));
		CornerstoneTest.AreEqual(0.5, target.Opacity);

		app.Clock.Pulse(TimeSpan.FromSeconds(1));
		CornerstoneTest.AreEqual(0, target.Opacity);
	}

	private static ControlTheme CreateControlTheme<T>(Animation animation)
	{
		return new ControlTheme(typeof(T))
		{
			Animations = { animation }
		};
	}

	private static Animation CreateOpacityAnimation()
	{
		return CreateOpacityAnimation(1.0, 0.0);
	}

	private static Animation CreateOpacityAnimation(object from, object to)
	{
		return new Animation
		{
			Duration = TimeSpan.FromSeconds(1),
			FillMode = FillMode.Both,
			Children =
			{
				new KeyFrame
				{
					KeyTime = TimeSpan.FromSeconds(0),
					Setters = { new Setter(Button.OpacityProperty, from) }
				},
				new KeyFrame
				{
					KeyTime = TimeSpan.FromSeconds(1),
					Setters = { new Setter(Button.OpacityProperty, to) }
				}
			}
		};
	}

	private static object CreateRoot(Button child, Application app)
	{
		var root = new TestRoot { StylingParent = app };
		root.Child = child;
		root.LayoutManager.ExecuteInitialLayoutPass();
		return root;
	}

	#endregion

	#region Classes

	private class AnimationTestApplication : Application, IDisposable
	{
		#region Fields

		private readonly IDisposable _lifetime;

		#endregion

		#region Constructors

		public AnimationTestApplication()
		{
			var services = new TestServices(globalClock: Clock);
			_lifetime = UnitTestApplication.Start(services);
		}

		#endregion

		#region Properties

		public MockGlobalClock Clock { get; } = new();

		#endregion

		#region Methods

		public void Dispose()
		{
			_lifetime.Dispose();
		}

		#endregion
	}

	#endregion

	#region Records

	private record KeyFrameValues(double From, double To);

	#endregion
}