#region References

using System;
using System.Collections.Generic;
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
		var expected = new Person { FirstName = "Foo", LastName = "Bar", Address = new Address { Line1 = "Main Street", Number = 123 }, Parent = new Person { FirstName = "Hello", LastName = "World" } };
		var actual = new Person { FirstName = "Foo", LastName = "Bar", Address = new Address { Line1 = "Main Street", Number = 123 }, Parent = new Person { FirstName = "Hello", LastName = "World" } };
		IsTrue(Comparer.Compare(expected, actual));

		expected.FirstName = "foo";
		IsFalse(Comparer.Compare(expected, actual));
		IsTrue(Comparer.Compare(expected, actual,
			new ComparerOptions
			{
				IncludeExcludeOptions = new Dictionary<Type, IncludeExcludeOptions>
				{
					{ typeof(Person), IncludeExcludeOptions.FromExclusions(nameof(Person.FirstName), nameof(Person.FullName)) }
				}
			})
		);
	}

	[TestMethod]
	public void Recursion()
	{
		var actual = new Person { FirstName = "Frank" };
		var expected = new Person { FirstName = "Frank" };

		AreEqual(actual, expected);

		actual.Parent = expected;
		actual.Address = new Address { Owner = new[] { actual, expected } };
		expected.Parent = actual;
		expected.Address = new Address { Owner = new[] { expected, actual } };

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

		public string FirstName { get; set; }

		public string FullName => $"{FirstName} {LastName}";

		public string LastName { get; set; }

		public Person Parent { get; set; }

		#endregion
	}

	#endregion
}