#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Animation.Easings;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Rendering.Composition.Expressions;
using Cornerstone.Presentation.Rendering.Composition.Server;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Composition;

[TestClass]
public class CompositionAnimationTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ExpressionAnimationOperationsWorksCorrectly()
	{
		using var scope = PresentationLocator.EnterScope();
		var compositor = new Compositor(RenderLoop.FromTimer(new CompositorTestServices.ManualRenderTimer()), null);
		var target = compositor.CreateSolidColorVisual();
		target.Server.Offset = new Vector3D(100, 200, 0);

		var ani = compositor.CreateExpressionAnimation("this.Target.Offset.X * 0.5 + 10");
		var instance = ani.CreateInstance(target.Server, null);
		instance.Initialize(TimeSpan.Zero, ExpressionVariant.Create(0f),
			ServerCompositionVisual.s_IdOfRotationAngleProperty);

		var result = instance.Evaluate(TimeSpan.Zero, ExpressionVariant.Create(0f));

		CornerstoneTest.AreEqual(VariantType.Double, result.Type);
		CornerstoneTest.AreEqual(60.0, result.Double);
	}

	[PresentationTestMethod]
	public void ExpressionAnimationRequeuesTargetWhenAnotherAnimationIsInvalidatedDuringEvaluation()
	{
		using var services = new CompositorTestServices();
		var border = new Border
		{
			Width = 10,
			Height = 10
		};

		services.TopLevel.Content = border;
		services.RunJobs();

		var visual = ElementComposition.GetElementVisual(border)!;
		var opacityAnimation = visual.Compositor.CreateExpressionAnimation("this.Target.RotationAngle * 0.1");
		var rotationAnimation = visual.Compositor.CreateExpressionAnimation("this.Target.Offset.X * 0.5");

		visual.StartAnimation("Opacity", opacityAnimation);
		visual.StartAnimation("RotationAngle", rotationAnimation);

		services.RunJobs();
		visual.Offset = new Vector3D(100, 0, 0);
		services.RunJobs();

		CornerstoneTest.AreEqual(50, visual.Server.RotationAngle);
		CornerstoneTest.AreEqual(5, visual.Server.Opacity);
	}

	[PresentationTestMethod]
	public void ExpressionAnimationTracksReferenceParameter()
	{
		using var scope = PresentationLocator.EnterScope();
		var compositor = new Compositor(RenderLoop.FromTimer(new CompositorTestServices.ManualRenderTimer()), null);
		var target = compositor.CreateSolidColorVisual();
		var obj = compositor.CreateSolidColorVisual();
		obj.Server.Offset = new Vector3D(100, 200, 0);

		var ani = compositor.CreateExpressionAnimation("obj.Offset.X * 0.5 + 10");
		ani.SetReferenceParameter("obj", obj);
		var instance = ani.CreateInstance(target.Server, null);

		target.Server.Activate();

		// Invoke OnSetAnimatedValue manually to create ServerObjectAnimationInstance.
		target.Server.GetOrCreateAnimations();
		var tmp = 0f;
		target.Server.Animations!.OnSetAnimatedValue(ServerCompositionVisual.s_IdOfRotationAngleProperty, ref tmp, TimeSpan.Zero, instance);

		var initialResult = instance.Evaluate(TimeSpan.Zero, ExpressionVariant.Create(0f));
		CornerstoneTest.AreEqual(60.0, initialResult.Double);

		obj.Server.Offset = new Vector3D(200, 300, 0);
		var updatedResult = instance.Evaluate(TimeSpan.Zero, ExpressionVariant.Create(0f));
		CornerstoneTest.AreEqual(110.0, updatedResult.Double);
	}

	[PresentationTestMethod]
	public void ExpressionAnimationTracksTarget()
	{
		using var scope = PresentationLocator.EnterScope();
		var compositor = new Compositor(RenderLoop.FromTimer(new CompositorTestServices.ManualRenderTimer()), null);
		var target = compositor.CreateSolidColorVisual();

		target.Server.Offset = new Vector3D(100, 200, 0);

		var ani = compositor.CreateExpressionAnimation("this.Target.Offset.X * 0.5 + 10");
		var instance = ani.CreateInstance(target.Server, null);

		target.Server.Activate();

		// Invoke OnSetAnimatedValue manually to create ServerObjectAnimationInstance.
		target.Server.GetOrCreateAnimations();
		var tmp = 0f;
		target.Server.Animations!.OnSetAnimatedValue(ServerCompositionVisual.s_IdOfRotationAngleProperty, ref tmp, TimeSpan.Zero, instance);

		var initialResult = instance.Evaluate(TimeSpan.Zero, ExpressionVariant.Create(0f));
		CornerstoneTest.AreEqual(60, initialResult.Double);

		target.Server.Offset = new Vector3D(200, 300, 0);
		var updatedResult = instance.Evaluate(TimeSpan.Zero, ExpressionVariant.Create(0f));
		CornerstoneTest.AreEqual(110.0, updatedResult.Double);
	}

	[PresentationTestMethod]
	[DataRow("3 frames starting from 0")]
	[DataRow("1 final frame")]
	public void GenericCheck(string name)
	{
		var data = GetAnimationCases().Single(c => c.Name == name);
		using var scope = PresentationLocator.EnterScope();
		var compositor =
			new Compositor(RenderLoop.FromTimer(new CompositorTestServices.ManualRenderTimer()), null);
		var target = compositor.CreateSolidColorVisual();
		var ani = new ScalarKeyFrameAnimation(compositor);
		foreach (var frame in data.Frames)
		{
			ani.InsertKeyFrame(frame.key, frame.value, new LinearEasing());
		}
		ani.Duration = TimeSpan.FromSeconds(1);
		var instance = ani.CreateInstance(target.Server, null);
		instance.Initialize(TimeSpan.Zero, data.StartingValue, ServerCompositionVisual.s_IdOfRotationAngleProperty);
		var currentValue = ExpressionVariant.Create(data.StartingValue);
		foreach (var check in data.Checks)
		{
			currentValue = instance.Evaluate(TimeSpan.FromSeconds(check.time), currentValue);
			CornerstoneTest.AreEqual(check.value, currentValue.Double);
		}
	}

	[PresentationTestMethod]
	[DataRow("Color")]
	[DataRow("Offset")]
	public void GetCompositionPropertyReturnsRegisteredProperties(string propName)
	{
		using var scope = PresentationLocator.EnterScope();
		var compositor = new Compositor(RenderLoop.FromTimer(new CompositorTestServices.ManualRenderTimer()), null);
		var target = compositor.CreateSolidColorVisual();

		var property = target.Server.GetCompositionProperty(propName);

		CornerstoneTest.IsNotNull(property);
		CornerstoneTest.AreEqual(propName, property.Name);
		CornerstoneTest.IsNotNull(property.GetVariant);
	}

	private static IReadOnlyList<AnimationData> GetAnimationCases()
	{
		return
		[
			new("3 frames starting from 0")
			{
				Frames =
				{
					(0f, 10f),
					(0.5f, 30f),
					(1f, 20f)
				},
				Checks =
				{
					(0.25f, 20f),
					(0.5f, 30f),
					(0.75f, 25f),
					(1f, 20f)
				}
			},
			new("1 final frame")
			{
				Frames =
				{
					(1f, 10f)
				},
				Checks =
				{
					(0f, 0f),
					(0.5f, 5f),
					(1f, 10f)
				}
			}
		];
	}

	#endregion

	#region Classes

	public class AnimationData
	{
		#region Constructors

		public AnimationData(string name)
		{
			Name = name;
		}

		#endregion

		#region Properties

		public List<(float time, float value)> Checks { get; set; } = new();
		public float Duration { get; set; } = 1;
		public List<(float key, float value)> Frames { get; set; } = new();

		public string Name { get; }
		public float StartingValue { get; set; }

		#endregion

		#region Methods

		public override string ToString()
		{
			return Name;
		}

		#endregion
	}

	private class DummyDispatcher : IDispatcher
	{
		#region Methods

		public bool CheckAccess()
		{
			return true;
		}

		public void Post(Action action, DispatcherPriority priority = default)
		{
			throw new NotSupportedException();
		}

		public void VerifyAccess()
		{
		}

		#endregion
	}

	#endregion
}