#region References

using System.Runtime.CompilerServices;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsMetadata
{
	#region Constructors

	public PresentationObjectTestsMetadata()
	{
		// Ensure properties are registered.
		RuntimeHelpers.RunClassConstructor(typeof(Class1).TypeHandle);
		RuntimeHelpers.RunClassConstructor(typeof(Class2).TypeHandle);
		RuntimeHelpers.RunClassConstructor(typeof(Class3).TypeHandle);
	}

	#endregion

	#region Classes

	[TestClass]
	public class DirectProperty : PresentationObjectTestsMetadata
	{
		#region Methods

		[PresentationTestMethod]
		public void UnsetValueCanBeOverriddenInAddOwneredProperty()
		{
			var baseValue = Class1.DirectProperty.GetUnsetValue(typeof(Class1));
			var addOwneredValue = Class3.DirectProperty.GetUnsetValue(typeof(Class3));

			CornerstoneTest.AreEqual("foo", baseValue);
			CornerstoneTest.AreEqual("baz", addOwneredValue);
		}

		[PresentationTestMethod]
		public void UnsetValueCanBeOverriddenInDerivedClass()
		{
			var baseValue = Class1.DirectProperty.GetUnsetValue(typeof(Class1));
			var derivedValue = Class1.DirectProperty.GetUnsetValue(typeof(Class2));

			CornerstoneTest.AreEqual("foo", baseValue);
			CornerstoneTest.AreEqual("bar", derivedValue);
		}

		#endregion
	}

	[TestClass]
	public class StyledProperty : PresentationObjectTestsMetadata
	{
		#region Methods

		[PresentationTestMethod]
		public void DefaultValueCanBeOverriddenInAddOwneredProperty()
		{
			var baseValue = Class1.StyledProperty.GetDefaultValue(typeof(Class1));
			var addOwneredValue = Class1.StyledProperty.GetDefaultValue(typeof(Class3));

			CornerstoneTest.AreEqual("foo", baseValue);
			CornerstoneTest.AreEqual("baz", addOwneredValue);
		}

		[PresentationTestMethod]
		public void DefaultValueCanBeOverriddenInDerivedClass()
		{
			var baseValue = Class1.StyledProperty.GetDefaultValue(typeof(Class1));
			var derivedValue = Class1.StyledProperty.GetDefaultValue(typeof(Class2));

			CornerstoneTest.AreEqual("foo", baseValue);
			CornerstoneTest.AreEqual("bar", derivedValue);
		}

		#endregion
	}

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<Class1, string> DirectProperty =
			PresentationProperty.RegisterDirect<Class1, string>("Styled", o => o.Direct, unsetValue: "foo");

		public static readonly StyledProperty<string> StyledProperty =
			PresentationProperty.Register<Class1, string>("Styled", "foo");

		#endregion

		#region Properties

		public string Direct { get; } = null;

		#endregion
	}

	private class Class2 : Class1
	{
		#region Constructors

		static Class2()
		{
			StyledProperty.OverrideDefaultValue<Class2>("bar");
			DirectProperty.OverrideMetadata<Class2>(new DirectPropertyMetadata<string>("bar"));
		}

		#endregion
	}

	private class Class3 : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<Class3, string> DirectProperty =
			Class1.DirectProperty.AddOwner<Class3>(o => o.Direct, unsetValue: "baz");

		public static readonly StyledProperty<string> StyledProperty =
			Class1.StyledProperty.AddOwner<Class3>();

		#endregion

		#region Constructors

		static Class3()
		{
			StyledProperty.OverrideDefaultValue<Class3>("baz");
		}

		#endregion

		#region Properties

		public string Direct { get; } = null;

		#endregion
	}

	#endregion
}