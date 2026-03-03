#region References

using System;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class DependencyProviderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CircularReferences()
	{
		var dependencyProvider = new DependencyProvider("Test");
		dependencyProvider.AddSingleton<TestClassForCircularReference>();
		dependencyProvider.AddSingleton<TestClassForCircularReferenceDetails>();

		ExpectedException<InvalidOperationException>(
			() => dependencyProvider.GetInstance<TestClassForCircularReference>(),
			"""
			System.InvalidOperationException
			Circular dependency detected:
				Cornerstone.UnitTests.Runtime.DependencyProviderTests+TestClassForCircularReference
				Cornerstone.UnitTests.Runtime.DependencyProviderTests+TestClassForCircularReference
			"""
		);
	}

	[TestMethod]
	public void ConstructorInjection()
	{
		var dependencyProvider = new DependencyProvider("Test");
		dependencyProvider.AddSingleton<IDependencyProvider>(this);
		dependencyProvider.AddSingleton<TestClassForConstructor>();

		var actual = dependencyProvider.GetInstance<TestClassForConstructor>();
		AreEqual(this, actual.Provider);
	}

	[TestMethod]
	public void PropertyInjection()
	{
		var dependencyProvider = new DependencyProvider("Test");
		dependencyProvider.AddSingleton<IDependencyProvider>(this);
		dependencyProvider.AddSingleton<TestClassForPropertyInjection>();

		var actual = dependencyProvider.GetInstance<TestClassForPropertyInjection>();
		AreEqual(this, actual.Provider);
	}

	#endregion

	#region Classes

	[SourceReflection]
	public class TestClassForCircularReference
	{
		#region Fields

		private readonly TestClassForCircularReferenceDetails _self;

		#endregion

		#region Constructors

		[DependencyInjectionConstructor]
		public TestClassForCircularReference(TestClassForCircularReferenceDetails self)
		{
			_self = self;
		}

		#endregion
	}

	[SourceReflection]
	public class TestClassForCircularReferenceDetails
	{
		#region Fields

		private readonly TestClassForCircularReference _self;

		#endregion

		#region Constructors

		[DependencyInjectionConstructor]
		public TestClassForCircularReferenceDetails(TestClassForCircularReference self)
		{
			_self = self;
		}

		#endregion
	}

	[SourceReflection]
	public class TestClassForConstructor
	{
		#region Constructors

		[DependencyInjectionConstructor]
		public TestClassForConstructor(IDependencyProvider provider)
		{
			Provider = provider;
		}

		#endregion

		#region Properties

		public IDependencyProvider Provider { get; }

		#endregion
	}

	[SourceReflection]
	public class TestClassForPropertyInjection
	{
		#region Properties

		[DependencyInjectedProperty]
		public IDependencyProvider Provider { get; private set; }

		#endregion
	}

	#endregion
}