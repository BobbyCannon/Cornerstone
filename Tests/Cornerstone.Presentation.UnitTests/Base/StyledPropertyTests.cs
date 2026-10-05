#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class StyledPropertyTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddOwneredGetMetadataCannotBeChanged()
	{
		var p1 = new StyledProperty<string>(
			"p1",
			typeof(Class1),
			typeof(Class1),
			new StyledPropertyMetadata<string>());
		var p2 = p1.AddOwner<Class2>();
		var metadata = p2.GetMetadata<Class2>();

		Assert.Throws<InvalidOperationException>(() => metadata.Merge(new StyledPropertyMetadata<string>(), p2));
	}

	[PresentationTestMethod]
	public void AddOwneredPropertyShouldBeSame()
	{
		var p1 = new StyledProperty<string>(
			"p1",
			typeof(Class1),
			typeof(Class1),
			new StyledPropertyMetadata<string>());
		var p2 = p1.AddOwner<Class2>();

		CornerstoneTest.Same(p1, p2);
	}

	[PresentationTestMethod]
	public void AddOwneredPropertyShouldEqualOriginal()
	{
		var p1 = new StyledProperty<string>(
			"p1",
			typeof(Class1),
			typeof(Class1),
			new StyledPropertyMetadata<string>());
		var p2 = p1.AddOwner<Class2>();

		CornerstoneTest.AreEqual(p1, p2);
		CornerstoneTest.AreEqual(p1.GetHashCode(), p2.GetHashCode());
		CornerstoneTest.IsTrue(p1 == p2);
	}

	[PresentationTestMethod]
	public void DefaultGetMetadataCannotBeChanged()
	{
		var p1 = new StyledProperty<string>(
			"p1",
			typeof(Class1),
			typeof(Class1),
			new StyledPropertyMetadata<string>());
		var metadata = p1.GetMetadata<Class1>();

		Assert.Throws<InvalidOperationException>(() => metadata.Merge(new StyledPropertyMetadata<string>(), p1));
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
	}

	private class Class2 : PresentationObject
	{
	}

	#endregion
}