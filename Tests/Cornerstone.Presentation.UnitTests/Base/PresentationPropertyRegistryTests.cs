#region References

using System.Linq;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationPropertyRegistryTests
{
	#region Constructors

	public PresentationPropertyRegistryTests()
	{
		// Ensure properties are registered.
		PresentationProperty p;
		p = Class1.FooProperty;
		p = Class2.BarProperty;
		p = AttachedOwner.AttachedProperty;
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void FindRegisteredDoesntFindNonAddOwneredAttachedProperty()
	{
		var result = PresentationPropertyRegistry.Instance.FindRegistered(typeof(Class2), "Attached");

		CornerstoneTest.IsNull(result);
	}

	[PresentationTestMethod]
	public void FindRegisteredDoesntFindNonregisteredProperty()
	{
		var result = PresentationPropertyRegistry.Instance.FindRegistered(typeof(Class1), "Bar");

		CornerstoneTest.IsNull(result);
	}

	[PresentationTestMethod]
	public void FindRegisteredFindsAddOwneredAttachedProperty()
	{
		var result = PresentationPropertyRegistry.Instance.FindRegistered(typeof(Class3), "Attached");

		CornerstoneTest.Same(AttachedOwner.AttachedProperty, result);
	}

	[PresentationTestMethod]
	public void FindRegisteredFindsProperty()
	{
		var result = PresentationPropertyRegistry.Instance.FindRegistered(typeof(Class1), "Foo");

		CornerstoneTest.AreEqual(Class1.FooProperty, result);
	}

	[PresentationTestMethod]
	public void FindRegisteredFindsUnqualifiedAttachedPropertyOnRegisteringType()
	{
		var result = PresentationPropertyRegistry.Instance.FindRegistered(typeof(AttachedOwner), "Attached");

		CornerstoneTest.Same(AttachedOwner.AttachedProperty, result);
	}

	[PresentationTestMethod]
	public void GetRegisteredAttachedReturnsRegisteredProperties()
	{
		var names = PresentationPropertyRegistry.Instance.GetRegisteredAttached(typeof(Class1))
			.Select(x => x.Name)
			.ToArray();

		CornerstoneTest.Contains(names, "Attached");
	}

	[PresentationTestMethod]
	public void GetRegisteredAttachedReturnsRegisteredPropertiesForBaseTypes()
	{
		var names = PresentationPropertyRegistry.Instance.GetRegisteredAttached(typeof(Class2))
			.Select(x => x.Name)
			.ToArray();

		CornerstoneTest.Contains(names, "Attached");
	}

	[PresentationTestMethod]
	public void GetRegisteredReturnsRegisteredProperties()
	{
		var names = PresentationPropertyRegistry.Instance.GetRegistered(typeof(Class1))
			.Select(x => x.Name)
			.ToArray();

		CornerstoneTest.AreEqual(new[] { "Baz", "Foo", "Qux" }, names);
	}

	[PresentationTestMethod]
	public void GetRegisteredReturnsRegisteredPropertiesForBaseTypes()
	{
		var names = PresentationPropertyRegistry.Instance.GetRegistered(typeof(Class2))
			.Select(x => x.Name)
			.ToArray();

		CornerstoneTest.AreEqual(new[] { "Bar", "Flob", "Fred", "Baz", "Foo", "Qux" }, names);
	}

	[PresentationTestMethod]
	public void RegisteredPropertiesCountReflectsNewlyAddedAttachedProperty()
	{
		var registry = new PresentationPropertyRegistry();
		var metadata = new StyledPropertyMetadata<int>();
		var property = new AttachedProperty<int>("test", typeof(object), typeof(object), metadata, true);
		registry.Register(typeof(object), property);
		registry.RegisterAttached(typeof(PresentationPropertyRegistryTests), property);
		property.AddOwner<Class4>();

		CornerstoneTest.AreEqual(1, registry.Properties.Count);
	}

	#endregion

	#region Classes

	private class AttachedOwner : Class1
	{
		#region Fields

		public static readonly AttachedProperty<string> AttachedProperty =
			PresentationProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached");

		#endregion
	}

	private class AttachedOwner2 : AttachedOwner
	{
	}

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> BazProperty =
			PresentationProperty.Register<Class1, string>("Baz");

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo");

		public static readonly StyledProperty<int> QuxProperty =
			PresentationProperty.Register<Class1, int>("Qux");

		#endregion
	}

	private class Class2 : Class1
	{
		#region Fields

		public static readonly StyledProperty<string> BarProperty =
			PresentationProperty.Register<Class2, string>("Bar");

		public static readonly StyledProperty<double> FlobProperty =
			PresentationProperty.Register<Class2, double>("Flob");

		public static readonly StyledProperty<double?> FredProperty =
			PresentationProperty.Register<Class2, double?>("Fred");

		#endregion
	}

	private class Class3 : Class1
	{
		#region Fields

		public static readonly AttachedProperty<string> AttachedProperty =
			AttachedOwner.AttachedProperty.AddOwner<Class3>();

		#endregion
	}

	private class Class4 : PresentationObject
	{
	}

	#endregion
}