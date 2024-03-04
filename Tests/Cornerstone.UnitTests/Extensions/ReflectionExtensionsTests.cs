#region References

using System.Linq;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

#pragma warning disable CS0169 // Field is never used
// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBePrivate.Global

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class ReflectionExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FieldTests()
	{
		var expected = new[] { "PublicField" };
		var type = typeof(ReflectionClassTest);
		var fields = type.GetCachedFields().Select(x => x.Name).ToArray();
		AreEqual(expected, fields);

		expected = new[] { "_privateField", "<PublicProperty>k__BackingField", "<PrivateProperty>k__BackingField" };
		fields = type.GetCachedFields(ReflectionExtensions.DefaultPrivateFlags).Select(x => x.Name).ToArray();
		AreEqual(expected, fields);

		var type2 = typeof(ReflectionStructTest);
		expected = new[] { "PublicField2" };
		fields = type2.GetCachedFields().Select(x => x.Name).ToArray();
		AreEqual(expected, fields);

		expected = new[] { "_privateField2", "<PublicProperty2>k__BackingField", "<PrivateProperty2>k__BackingField" };
		fields = type2.GetCachedFields(ReflectionExtensions.DefaultPrivateFlags).Select(x => x.Name).ToArray();
		AreEqual(expected, fields);
	}

	[TestMethod]
	public void PropertyTests()
	{
		// From Type
		var expected = new[] { "PublicProperty" };
		var type = typeof(ReflectionClassTest);
		var properties = type.GetCachedProperties().Select(x => x.Name).ToArray();
		AreEqual(expected, properties);
		var property = type.GetCachedProperty(nameof(ReflectionClassTest.PublicProperty));
		AreEqual(expected[0], property.Name);

		// From Model
		var model = new ReflectionClassTest();
		var modelType = model.GetType();
		properties = model.GetType().GetCachedProperties().Select(x => x.Name).ToArray();
		AreEqual(expected, properties);
		property = modelType.GetCachedProperty(nameof(ReflectionClassTest.PublicProperty));
		AreEqual(expected[0], property.Name);
		property = model.GetCachedProperty(x => x.PublicProperty);
		AreEqual(expected[0], property.Name);

		// From Type
		expected = new[] { "PrivateProperty" };
		properties = type.GetCachedProperties(ReflectionExtensions.DefaultPrivateFlags).Select(x => x.Name).ToArray();
		AreEqual(expected, properties);
		property = type.GetCachedProperty(expected[0], ReflectionExtensions.DefaultPrivateFlags);
		AreEqual(expected[0], property.Name);

		// From Model
		expected = new[] { "PrivateProperty" };
		properties = modelType.GetCachedProperties(ReflectionExtensions.DefaultPrivateFlags).Select(x => x.Name).ToArray();
		AreEqual(expected, properties);
		property = modelType.GetCachedProperty(expected[0], ReflectionExtensions.DefaultPrivateFlags);
		AreEqual(expected[0], property.Name);
	}
	
	[TestMethod]
	public void PropertyTests2()
	{
		// From Type
		var expected = new[] { "PublicProperty" };
		var type = typeof(ReflectionClassTest2);
		var properties = type.GetCachedProperties().Select(x => x.Name).ToArray();
		AreEqual(expected, properties);
		var property = type.GetCachedProperty(nameof(ReflectionClassTest2.PublicProperty));
		AreEqual(expected[0], property.Name);
		AreEqual(typeof(int), property.PropertyType);

		// From Model
		var model = new ReflectionClassTest2();
		var modelType = model.GetType();
		properties = model.GetType().GetCachedProperties().Select(x => x.Name).ToArray();
		AreEqual(expected, properties);
		property = modelType.GetCachedProperty(nameof(ReflectionClassTest2.PublicProperty));
		AreEqual(expected[0], property.Name);
		AreEqual(typeof(int), property.PropertyType);
		property = model.GetCachedProperty(x => x.PublicProperty);
		AreEqual(expected[0], property.Name);
		AreEqual(typeof(int), property.PropertyType);
	}

	#endregion

	#region Classes

	public class ReflectionClassTest2 : ReflectionClassTest
	{
		public new int PublicProperty { get; set; }
	}

	public class ReflectionClassTest
	{
		#region Fields

		public int PublicField;

		private string _privateField;

		#endregion

		#region Properties

		public double PublicProperty { get; set; }

		private double PrivateProperty { get; set; }

		#endregion

		#region Methods

		public void MethodOne()
		{
		}

		#endregion
	}

	#endregion

	#region Structures

	public struct ReflectionStructTest
	{
		#region Fields

		public int PublicField2;

		private string _privateField2;

		#endregion

		#region Properties

		public double PublicProperty2 { get; set; }

		private double PrivateProperty2 { get; set; }

		#endregion
	}

	#endregion
}