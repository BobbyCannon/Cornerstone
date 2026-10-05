#region References

extern alias A1;
extern alias A2;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using CompletionEngineTests.Models;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests.Metadata;

[TestClass]
public class MetadataConverterTests
{
	#region Fields

	private static readonly string[] _expectedPublicOrInternalProperties;
	private static readonly string[] _expectedPublicProperties;
	private static readonly Core.AssemblyMetadata.Metadata _metadata;

	#endregion

	#region Constructors

	static MetadataConverterTests()
	{
		_expectedPublicOrInternalProperties =
		[
			nameof(InternalClass.PublicProperty),
			nameof(InternalClass.InternalProperty),
			nameof(InternalClass.MixedInternalProperty)
		];
		_expectedPublicProperties =
		[
			nameof(InternalClass.PublicProperty)
		];
		var t = typeof(XamlCompletionTestBase).Assembly.GetModules()[0].FullyQualifiedName;
		_metadata = new MetadataReader(new DnlibMetadataProvider())
			.GetForTargetAssembly(new FolderAssemblyProvider(t));
	}

	#endregion

	#region Methods

	[TestMethod]
	public void AttachedPropertySetterAndGetterMixMatch()
	{
		var clrType = typeof(Grid);
		var nsName = "clr-namespace:" + clrType.Namespace + ";assembly=" + clrType.Assembly.GetName().Name;
		var ns = _metadata.Namespaces[nsName];
		Assert.IsNotNull(ns);
		ns.TryGetValue(clrType.Name, out var type);
		Assert.IsNotNull(type);

		var property = type.Properties.SingleOrDefault(p => p.Name == "Column");
		Assert.IsNotNull(property);
		Assert.IsTrue(property.IsAttached);
		Assert.AreEqual("System.Int32", property.Type?.Name);
	}

	[TestMethod]
	public void DiscoverAttachedEventIfItIsDerivedFromRoutedEvent()
	{
		var clrType = typeof(MetadataTestClass);
		var nsName = "clr-namespace:" + clrType.Namespace + ";assembly=" + typeof(MetadataTestClass).Assembly.GetName().Name;
		var ns = _metadata.Namespaces[nsName];
		var type = ns[clrType.Name];

		var attachedEvent = type.Events.Single();
		Assert.IsTrue(attachedEvent.Type.FullName == typeof(MetadataTestClass).FullName);
	}

	[TestMethod]
	public void DiscoverDoNotOverlapped()
	{
		var clrType = typeof(AttachedBehavior);
		var nsName = "clr-namespace:" + clrType.Namespace + ";assembly=" + clrType.Assembly.GetName().Name;
		var ns = _metadata.Namespaces[nsName];

		Assert.IsNotNull(ns);
		var type = ns[clrType.Name];
		Assert.IsNotNull(type);
		Assert.AreEqual(GetName(clrType), type.AssemblyQualifiedName);

		var clrTypeA1 = typeof(A1::CompletionEngineTests.Models.AttachedBehavior);
		nsName = "clr-namespace:" + clrTypeA1.Namespace + ";assembly=" + clrTypeA1.Assembly.GetName().Name;

		ns = _metadata.Namespaces[nsName];

		Assert.IsNotNull(ns);

		var typeA1 = ns[clrTypeA1.Name];

		Assert.IsNotNull(typeA1);

		Assert.AreEqual(GetName(clrTypeA1), typeA1.AssemblyQualifiedName);

		var clrTypeA2 = typeof(A2::CompletionEngineTests.Models.AttachedBehavior);
		nsName = "clr-namespace:" + clrTypeA1.Namespace + ";assembly=" + clrTypeA2.Assembly.GetName().Name;

		ns = _metadata.Namespaces[nsName];

		Assert.IsNotNull(ns);

		var typeA2 = ns[clrTypeA2.Name];

		Assert.IsNotNull(typeA2);

		Assert.AreEqual(GetName(clrTypeA2), typeA2.AssemblyQualifiedName);
	}

	[TestMethod]
	[DynamicData(nameof(GetCases))]
	public void DiscoverInternalsVisibleTo(TestScenario scenario)
	{
		Assert.IsNotNull(scenario.ClrType);
		var nsName = "clr-namespace:" + scenario.ClrType.Namespace + ";assembly=" + scenario.ClrType.Assembly.GetName().Name;
		var ns = _metadata.Namespaces[nsName];

		Assert.IsNotNull(ns);
		
		System.Diagnostics.Debug.WriteLine(nsName + "; " + scenario.ClrType.FullName);

		ns.TryGetValue(scenario.ClrType.Name, out var type);
		scenario.CheckAction(scenario.ClrType, type);
	}

	public static IEnumerable<object[]> GetCases()
	{
		// Local Assembly
		yield return
		[
			new TestScenario("Local Internal Attached Behavior",
				typeof(InternalAttachedBehavior),
				static (clrType, mdType) => { Assert.AreEqual(GetName(clrType), mdType.AssemblyQualifiedName); })
		];
		yield return
		[
			new TestScenario("Local Internal Class",
				typeof(InternalClass),
				static (clrType, mdType) =>
				{
					Assert.AreEqual(GetName(clrType), mdType.AssemblyQualifiedName);
					Assert.AreSequenceEqual(_expectedPublicOrInternalProperties, mdType.Properties.Select(p => p.Name));
				})
		];
		yield return
		[
			new TestScenario("Local Public Class with internal properties",
				typeof(PublicWithInternalPropertiesClass),
				static (clrType, mdType) =>
				{
					Assert.AreEqual(GetName(clrType), mdType.AssemblyQualifiedName);
					Assert.AreSequenceEqual(_expectedPublicOrInternalProperties, mdType.Properties.Select(p => p.Name));
				})
		];
		// TestAssembly1 with InternalsVisibleTo
		yield return
		[
			new TestScenario("InternalsVisibleTo Internal Attached Behavior",
				typeof(A1::CompletionEngineTests.Models.InternalAttachedBehavior),
				static (clrType, mdType) => { Assert.AreEqual(GetName(clrType), mdType?.AssemblyQualifiedName); })
		];
		yield return
		[
			new TestScenario("InternalsVisibleTo Internal Class",
				typeof(A1::CompletionEngineTests.Models.InternalClass),
				static (clrType, mdType) =>
				{
					Assert.AreEqual(GetName(clrType), mdType?.AssemblyQualifiedName);
					Assert.AreSequenceEqual(_expectedPublicOrInternalProperties, mdType?.Properties.Select(p => p.Name));
				})
		];
		yield return
		[
			new TestScenario("InternalsVisibleTo Public Class with internal properties",
				typeof(A1::CompletionEngineTests.Models.PublicWithInternalPropertiesClass),
				static (clrType, mdType) =>
				{
					Assert.AreEqual(GetName(clrType), mdType.AssemblyQualifiedName);
					Assert.AreSequenceEqual(_expectedPublicOrInternalProperties, mdType.Properties.Select(p => p.Name));
				})
		];
		// TestAssembly2 without InternalsVisibleTo
		yield return
		[
			new TestScenario("Not InternalsVisibleTo Internal Attached Behavior",
				Type.GetType("CompletionEngineTests.Models.InternalAttachedBehavior, TestAssembly2"),
				static (clrType, mdType) => { Assert.IsNull(mdType); })
		];
		yield return
		[
			new TestScenario("Not InternalsVisibleTo Internal Class",
				Type.GetType("CompletionEngineTests.Models.InternalAttachedBehavior, TestAssembly2"),
				static (clrType, mdType) => { Assert.IsNull(mdType); })
		];
		yield return
		[
			new TestScenario("InternalsVisibleTo Public Class with internal properties",
				Type.GetType("CompletionEngineTests.Models.PublicWithInternalPropertiesClass, TestAssembly2"),
				static (clrType, mdType) =>
				{
					Assert.AreEqual(GetName(clrType), mdType.AssemblyQualifiedName);
					Assert.AreSequenceEqual(_expectedPublicProperties, mdType.Properties.Select(p => p.Name));
				})
		];
	}

	private static string GetName(Type clrType)
	{
		return $"{clrType.FullName}, {clrType.Assembly.GetName().Name}";
	}

	#endregion

	#region Records

	public record TestScenario(string Description, Type ClrType, Action<Type, MetadataType> CheckAction)
	{
		#region Methods

		public override string ToString()
		{
			return Description;
		}

		#endregion
	}

	#endregion
}