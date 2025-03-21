#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cornerstone.Automation.Web;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class TypeExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EnumCount()
	{
		AreEqual(4, NamingConvention.CamelCase.Count());
		AreEqual(3, BrowserType.All.Count());
	}

	[TestMethod]
	public void EnumIsFlagged()
	{
		IsTrue(typeof(BrowserType).IsFlaggedEnum());
		IsFalse(typeof(NamingConvention).IsFlaggedEnum());
	}

	[TestMethod]
	public void FromNullable()
	{
		var actual = typeof(bool?).FromNullableType();
		AreEqual(typeof(bool), actual);
	}

	[TestMethod]
	public void GetDetails()
	{
		var actual = EnumExtensions.GetAllEnumDetails<BrowserType>().Values;
		IsNotNull(actual);
		var expected = new EnumExtensions.EnumDetails[]
		{
			new() { Description = "None", GroupName = "", IsFlaggedValue = false, Name = "None", ShortName = "None", Value = BrowserType.None, NumericValue = 0 },
			new() { Description = "Chrome", GroupName = "", IsFlaggedValue = true, Name = "Chrome", ShortName = "Chrome", Value = BrowserType.Chrome, NumericValue = 1 },
			new() { Description = "Edge", GroupName = "", IsFlaggedValue = true, Name = "Edge", ShortName = "Edge", Value = BrowserType.Edge, NumericValue = 2 },
			new() { Description = "Firefox", GroupName = "", IsFlaggedValue = true, Name = "Firefox", ShortName = "Firefox", Value = BrowserType.Firefox, NumericValue = 4 },
			new() { Description = "All", GroupName = "", IsFlaggedValue = false, Name = "All", ShortName = "All", Value = BrowserType.All, NumericValue = 255 }
		};
		//CSharpCodeWriter.GenerateCode(actual).Dump();
		AreEqual(expected, actual);

		actual = EnumExtensions.GetAllEnumDetails<NamingConvention>().Values;
		expected =
		[
			new() { Description = "PascalCase", GroupName = "", IsFlaggedValue = false, Name = "PascalCase", ShortName = "PascalCase", Value = NamingConvention.PascalCase, NumericValue = 0 },
			new() { Description = "CamelCase", GroupName = "", IsFlaggedValue = false, Name = "CamelCase", ShortName = "CamelCase", Value = NamingConvention.CamelCase, NumericValue = 1 },
			new() { Description = "KebabCase", GroupName = "", IsFlaggedValue = false, Name = "KebabCase", ShortName = "KebabCase", Value = NamingConvention.KebabCase, NumericValue = 2 },
			new() { Description = "SnakeCase", GroupName = "", IsFlaggedValue = false, Name = "SnakeCase", ShortName = "SnakeCase", Value = NamingConvention.SnakeCase, NumericValue = 3 }
		];
		//CSharpCodeWriter.GenerateCode(actual).Dump();
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void ImplementsType()
	{
		var scenarios = new List<(Type child, Type parent)>
		{
			(typeof(ShortGuid), typeof(IComparable<>)),
			(typeof(ShortGuid), typeof(IComparable<ShortGuid>)),
		};

		foreach (var scenario in scenarios)
		{
			$"{scenario.child.FullName} => {scenario.parent.FullName}".Dump();
			IsTrue(scenario.child.ImplementsType(scenario.parent));
			IsFalse(scenario.parent.ImplementsType(scenario.child));
		}
	}

	[TestMethod]
	public void ImplementsTypeForObject()
	{
		var scenarios = new List<(object child, Type parent)>
		{
			(new SortedDictionary<string, PartialUpdateValue>(), typeof(IDictionary<string, PartialUpdateValue>)),
			(new ShortGuid(), typeof(IComparable<>))
		};

		foreach (var scenario in scenarios)
		{
			$"{scenario.child.GetType().FullName} => {scenario.parent.FullName}".Dump();
			IsTrue(scenario.child.ImplementsType(scenario.parent));
		}
	}

	[TestMethod]
	public void ImplementsTypesForGenerics()
	{
		IsTrue(new ShortGuid().ImplementsType<IComparable>());
		IsTrue(new ReadOnlyDictionary<int, string>(new Dictionary<int, string>()).ImplementsType(typeof(IReadOnlyDictionary<,>)));
	}

	[TestMethod]
	public void IsNullable()
	{
		var nullableTypes = new[]
		{
			typeof(string),
			typeof(DBNull)
		};

		foreach (var type in nullableTypes)
		{
			IsTrue(type.IsNullable());
		}

		var nonNullableTypes = new[]
		{
			typeof(bool),
			typeof(ConsoleKey),
			typeof(DateTime),
			typeof(int),
			typeof(double),
			typeof(ComparerSettings)
		};

		foreach (var type in nonNullableTypes)
		{
			IsFalse(type.IsNullable());
		}
	}

	[TestMethod]
	public void IsSingleFlag()
	{
		IsFalse(BrowserType.None.IsSingleFlagEnum());
		IsFalse(BrowserType.All.IsSingleFlagEnum());
		IsTrue(BrowserType.Chrome.IsSingleFlagEnum());
		IsTrue(BrowserType.Edge.IsSingleFlagEnum());
		IsTrue(BrowserType.Firefox.IsSingleFlagEnum());
	}

	[TestMethod]
	public void ToNullable()
	{
		var actual = typeof(bool).ToNullableType();
		AreEqual(typeof(bool?), actual);
	}

	#endregion
}