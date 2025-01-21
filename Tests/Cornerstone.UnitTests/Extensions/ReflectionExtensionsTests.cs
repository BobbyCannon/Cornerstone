#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Storage;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Server;

#endregion

// ReSharper disable UnusedMember.Local
#pragma warning disable CS0169 // Field is never used

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class ReflectionExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AvalonObjectPrivateHandler()
	{
		var type = typeof(AvaloniaObject);
		var fields = type.GetCachedFields().Select(x => x.Name).ToArray();
		fields.DumpJson();

		var count = 0;
		var ao = new AvaloniaObjectTest();
		((INotifyPropertyChanged) ao).PropertyChanged += (_, _) => count++;
		ao.Name = "Update";

		AreEqual(1, count);
	}

	[TestMethod]
	public void FieldTests()
	{
		var expected = new[] { "_privateField", "<PublicProperty>k__BackingField", "<PrivateProperty>k__BackingField" };
		var type = typeof(ReflectionClassTest);
		var fields = type.GetCachedFields().Select(x => x.Name).ToArray();
		AreEqual(expected, fields);

		expected = ["PublicField"];
		fields = type.GetCachedFields(ReflectionExtensions.DefaultPublicFlags).Select(x => x.Name).ToArray();
		AreEqual(expected, fields);

		var type2 = typeof(ReflectionStructTest);
		expected = ["_privateField2", "<PublicProperty2>k__BackingField", "<PrivateProperty2>k__BackingField"];
		fields = type2.GetCachedFields().Select(x => x.Name).ToArray();
		AreEqual(expected, fields);

		expected = ["PublicField2"];
		fields = type2.GetCachedFields(ReflectionExtensions.DefaultPublicFlags).Select(x => x.Name).ToArray();
		AreEqual(expected, fields);
	}

	[TestMethod]
	public void GetCodeTypeNameForMethodInfo()
	{
		var info = typeof(ReflectionExtensionsTests).GetMethod(nameof(Build), ReflectionExtensions.DefaultPrivateFlags);
		Assert.IsNotNull(info);
		CSharpCodeWriter.GetCodeTypeName(info).Dump();

		var info2 = info.MakeGenericMethod(typeof(AccountEntity), typeof(int), typeof(AddressEntity), typeof(long));
		CSharpCodeWriter.GetCodeTypeName(info2).Dump();

		var address = new AddressEntity();
		var account = new AccountEntity();
		var dictionary = (Dictionary<AccountEntity, AddressEntity>) info2.Invoke(this, [account, address]);
		Assert.IsNotNull(dictionary);
		AreEqual(1, dictionary.Count);
		AreEqual(account, dictionary.Keys.First());
		AreEqual(address, dictionary.Values.First());
		AreEqual(address, dictionary[account]);
	}

	[TestMethod]
	public void IsDelegate()
	{
		var scenarios = new[]
		{
			typeof(Action), typeof(Action<>)
		};

		foreach (var scenario in scenarios)
		{
			scenario.FullName.Dump();
			IsTrue(scenario.IsDelegate());
		}
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
		expected = ["PrivateProperty"];
		properties = type.GetCachedProperties(ReflectionExtensions.DefaultPrivateFlags).Select(x => x.Name).ToArray();
		AreEqual(expected, properties);
		property = type.GetCachedProperty(expected[0], ReflectionExtensions.DefaultPrivateFlags);
		AreEqual(expected[0], property.Name);

		// From Model
		expected = ["PrivateProperty"];
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

	private Dictionary<T1, T2> Build<T1, T1K, T2, T2K>(T1 entityT1, T2 entityT2)
		where T1 : Entity<T1K>
		where T2 : Entity<T2K>
	{
		var dictionary = new Dictionary<T1, T2> { { entityT1, entityT2 } };
		return dictionary;
	}

	#endregion

	#region Classes

	public class AvaloniaObjectTest : AvaloniaObject
	{
		#region Properties

		public string Name { get; set; }

		#endregion

		#region Methods

		public void OnPropertyChanged(string propertyName)
		{
			var t = typeof(AvaloniaObject).GetCachedField("_inpcChanged");
			var v = t.GetValue(this);
			var h = v as PropertyChangedEventHandler;
			h?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
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

	public class ReflectionClassTest2 : ReflectionClassTest
	{
		#region Properties

		public new int PublicProperty { get; set; }

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