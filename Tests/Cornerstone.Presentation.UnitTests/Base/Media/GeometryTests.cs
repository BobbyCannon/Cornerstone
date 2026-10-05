#region References

using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class GeometryTests
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingAffectsGeometryPropertyCausesChangedToBeRaised()
	{
		var target = new TestGeometry();
		var raised = false;

		target.Changed += (s, e) => raised = true;
		target.Foo = true;

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void ChangingAffectsGeometryPropertyCausesPlatformImplToBeUpdated()
	{
		var target = new TestGeometry();
		var platformImpl = target.PlatformImpl;

		target.Foo = true;

		CornerstoneTest.NotSame(platformImpl, target.PlatformImpl);
	}

	[PresentationTestMethod]
	public void ChangingTransformCausesChangedToBeRaised()
	{
		var transform = new RotateTransform(45);
		var target = new TestGeometry { Transform = transform };
		var raised = false;

		target.Changed += (s, e) => raised = true;
		transform.Angle = 90;

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void RemovingTransformCausesChangedToBeRaised()
	{
		var transform = new RotateTransform(45);
		var target = new TestGeometry { Transform = transform };
		var raised = false;

		target.Changed += (s, e) => raised = true;
		target.Transform = null;

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void SettingTransformCausesChangedToBeRaised()
	{
		var target = new TestGeometry();
		var raised = false;

		target.Changed += (s, e) => raised = true;
		target.Transform = new RotateTransform(45);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void TransformProducesTransformedPlatformImpl()
	{
		var target = new TestGeometry();
		var rotate = new RotateTransform(45);

		CornerstoneTest.IsFalse(target.PlatformImpl is ITransformedGeometryImpl);
		target.Transform = rotate;
		CornerstoneTest.IsTrue(target.PlatformImpl is ITransformedGeometryImpl);
		rotate.Angle = 0;
		CornerstoneTest.IsFalse(target.PlatformImpl is ITransformedGeometryImpl);
	}

	#endregion

	#region Classes

	private class TestGeometry : Geometry
	{
		#region Fields

		public static readonly StyledProperty<bool> FooProperty =
			PresentationProperty.Register<TestGeometry, bool>(nameof(Foo));

		#endregion

		#region Constructors

		static TestGeometry()
		{
			AffectsGeometry(FooProperty);
		}

		#endregion

		#region Properties

		public bool Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		#endregion

		#region Methods

		public override Geometry Clone()
		{
			throw new NotImplementedException();
		}

		private protected sealed override IGeometryImpl CreateDefiningGeometry()
		{
			return new StubGeometryImpl();
		}

		#endregion
	}

	#endregion
}