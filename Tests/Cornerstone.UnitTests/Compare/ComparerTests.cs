#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Compare;

[TestClass]
public class ComparerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DefaultActivatorTypes()
	{
		foreach (var item in Activator.AllTypes)
		{
			item.FullName.Dump();

			var instance1 = item.CreateInstance();
			var instance2 = item.CreateInstance();
			AreEqual(instance1, instance2);
			AreEqual(instance2, instance1);

			var nInstance1 = item.ToNullableType().CreateInstance();
			var nInstance2 = item.ToNullableType().CreateInstance();
			AreEqual(nInstance1, nInstance2);
			AreEqual(nInstance2, nInstance1);
		}
	}

	[TestMethod]
	public void ExcludePropertyOnDirectObject()
	{
		var expected = new Person
		{
			FirstName = "Foo",
			LastName = "Bar",
			Address = new Address { Line1 = "Main Street", Number = 123 },
			Parent = new Person { FirstName = "Hello", LastName = "World" }
		};
		var actual = new Person
		{
			FirstName = "Foo",
			LastName = "Bar",
			Address = new Address { Line1 = "Main Street", Number = 123 },
			Parent = new Person { FirstName = "Hello", LastName = "World" }
		};
		AreEqual(CompareResult.AreEqual, Comparer.Compare(expected, actual).Result);

		expected.FirstName = "foo";
		AreEqual(CompareResult.NotEqual, Comparer.Compare(expected, actual).Result);
		AreEqual(CompareResult.AreEqual, Comparer.Compare(expected, actual,
				new ComparerSettings
				{
					TypeIncludeExcludeSettings = new Dictionary<Type, IncludeExcludeSettings>
					{
						{ typeof(Person), new[] { nameof(Person.FirstName), nameof(Person.FullName) }.ToOnlyExcludingSettings() }
					}
				}
			).Result
		);
	}

	[TestMethod]
	public void MaxDepth()
	{
		var expected = new Person
		{
			FirstName = "Foo",
			LastName = "Bar",
			Address = new Address { Line1 = "Main Street", Number = 123, Owner = [
				new Person { FirstName = "Hello", LastName = "World" }
			]}
		};
		var actual = new Person
		{
			FirstName = "Foo",
			LastName = "Bar",
			Address = new Address { Line1 = "Main Street", Number = 123, Owner = [
				new Person { FirstName = "Hello", LastName = "World" }
			]}
		};
		var settings = new ComparerSettings { MaxDepth = 1 };
		AreEqual(expected, actual, null, settings);

		actual.FirstName = "foo";
		AreEqual("FirstName\r\nFoo\r\n **** != ****\r\nfoo\r\n", Comparer.Compare(expected, actual, settings).Differences);
		
		actual.FirstName = "Foo";
		actual.LastName = "bar";
		AreEqual("FullName\r\nFoo Bar\r\n **** != ****\r\nFoo bar\r\n", Comparer.Compare(expected, actual, settings).Differences);
		
		actual.LastName = "Bar";
		settings.MaxDepth = 3;
		
		AreEqual(expected, actual, null, settings);

		actual.Address.Owner.First().FirstName = "hello";
		settings.MaxDepth = 2;
		
		AreEqual(expected, actual, null, settings);
		
		actual.Address.Owner.First().FirstName = "hello";
		settings.MaxDepth = 3;
		
		AreEqual("FirstName.Owner.Address\r\nArray index [0] does not match. Hello\r\n **** != ****\r\nhello\r\n", Comparer.Compare(expected, actual, settings).Differences);
	}

	[TestMethod]
	public void SubDictionary()
	{
		var person1 = new Person { Dates = new Dictionary<string, DateTime> { { "MinDate", DateTime.MinValue } } };
		var person2 = new Person();
		var session = Comparer.Compare(person1, person2);

		AreEqual(CompareResult.NotEqual, session.Result);
		AreEqual(
			"Dates\r\nSystem.Collections.Generic.Dictionary`2[System.String,System.DateTime]\r\n **** != ****\r\nnull\r\n",
			session.Differences.ToString()
		);
	}
	
	[TestMethod]
	public void ValueTypesComparedToNull()
	{
		AreEqual("True\r\n **** != ****\r\nnull\r\n", Comparer.Compare<bool, bool?>(true, null).Differences);
		AreEqual("null\r\n **** != ****\r\nTrue\r\n", Comparer.Compare<bool?, bool>(null, true).Differences);
		AreEqual("True\r\n **** != ****\r\nnull\r\n", Comparer.Compare<bool?, bool?>(true, null).Differences);
		AreEqual("null\r\n **** != ****\r\nTrue\r\n", Comparer.Compare<bool?, bool?>(null, true).Differences);
	}

	[TestMethod]
	public void Recursion()
	{
		var actual = new Person { FirstName = "Frank" };
		var expected = new Person { FirstName = "Frank" };

		AreEqual(actual, expected);

		actual.Parent = expected;
		actual.Address = new Address { Owner = [actual, expected] };
		expected.Parent = actual;
		expected.Address = new Address { Owner = [expected, actual] };

		AreEqual(actual, expected);
	}

	#endregion

	#region Classes

	public class Address
	{
		#region Properties

		public string Line1 { get; set; }

		public int Number { get; set; }

		public IEnumerable<Person> Owner { get; set; }

		#endregion
	}

	public class Person
	{
		#region Properties

		public Address Address { get; set; }

		public Dictionary<string, DateTime> Dates { get; set; }

		public string FirstName { get; set; }

		public string FullName => $"{FirstName} {LastName}";

		public string LastName { get; set; }

		public Person Parent { get; set; }

		#endregion
	}

	#endregion
}