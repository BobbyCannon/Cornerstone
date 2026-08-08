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
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Cornerstone.VisualStudio.Tests.Metadata;

public class MetadataConverterTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public MetadataConverterTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper;
	}

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

	[Fact]
	public void AttachedPropertySetterAndGetterMixMatch()
	{
		var clrType = typeof(Grid);
		var nsName = "clr-namespace:" + clrType.Namespace + ";assembly=" + clrType.Assembly.GetName().Name;
		var ns = _metadata.Namespaces[nsName];
		Assert.NotNull(ns);
		ns.TryGetValue(clrType.Name, out var type);
		Assert.NotNull(type);

		var property = type.Properties.SingleOrDefault(p => p.Name == "Column");
		Assert.NotNull(property);
		Assert.True(property.IsAttached);
		Assert.Equal("System.Int32", property.Type?.Name);
	}

	[Fact]
	public void DiscoverAttachedEventIfItIsDerivedFromRoutedEvent()
	{
		var clrType = typeof(MetadataTestClass);
		var nsName = "clr-namespace:" + clrType.Namespace + ";assembly=" + typeof(MetadataTestClass).Assembly.GetName().Name;
		var ns = _metadata.Namespaces[nsName];
		var type = ns[clrType.Name];

		var attachedEvent = type.Events.Single();
		Assert.True(attachedEvent.Type.FullName == typeof(MetadataTestClass).FullName);
	}

	[Fact]
	public void DiscoverDoNotOverlapped()
	{
		var clrType = typeof(AttachedBehavior);
		var nsName = "clr-namespace:" + clrType.Namespace + ";assembly=" + clrType.Assembly.GetName().Name;
		var ns = _metadata.Namespaces[nsName];

		Assert.NotNull(ns);
		var type = ns[clrType.Name];
		Assert.NotNull(type);
		Assert.Equal(GetName(clrType), type.AssemblyQualifiedName);

		var clrTypeA1 = typeof(A1::CompletionEngineTests.Models.AttachedBehavior);
		nsName = "clr-namespace:" + clrTypeA1.Namespace + ";assembly=" + clrTypeA1.Assembly.GetName().Name;

		ns = _metadata.Namespaces[nsName];

		Assert.NotNull(ns);

		var typeA1 = ns[clrTypeA1.Name];

		Assert.NotNull(typeA1);

		Assert.Equal(GetName(clrTypeA1), typeA1.AssemblyQualifiedName);

		var clrTypeA2 = typeof(A2::CompletionEngineTests.Models.AttachedBehavior);
		nsName = "clr-namespace:" + clrTypeA1.Namespace + ";assembly=" + clrTypeA2.Assembly.GetName().Name;

		ns = _metadata.Namespaces[nsName];

		Assert.NotNull(ns);

		var typeA2 = ns[clrTypeA2.Name];

		Assert.NotNull(typeA2);

		Assert.Equal(GetName(clrTypeA2), typeA2.AssemblyQualifiedName);
	}

	[Theory]
	[MemberData(nameof(GetCases))]
	public void DiscoverInternalsVisibleTo(TestScenario scenario)
	{
		Assert.NotNull(scenario.ClrType);
		var nsName = "clr-namespace:" + scenario.ClrType.Namespace + ";assembly=" + scenario.ClrType.Assembly.GetName().Name;
		var ns = _metadata.Namespaces[nsName];

		Assert.NotNull(ns);
		
		_testOutputHelper.WriteLine(nsName + "; " + scenario.ClrType.FullName);

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
				static (clrType, mdType) => { Assert.Equal(GetName(clrType), mdType.AssemblyQualifiedName); })
		];
		yield return
		[
			new TestScenario("Local Internal Class",
				typeof(InternalClass),
				static (clrType, mdType) =>
				{
					Assert.Equal(GetName(clrType), mdType.AssemblyQualifiedName);
					Assert.Equal(_expectedPublicOrInternalProperties, mdType.Properties.Select(p => p.Name));
				})
		];
		yield return
		[
			new TestScenario("Local Public Class with internal properties",
				typeof(PublicWithInternalPropertiesClass),
				static (clrType, mdType) =>
				{
					Assert.Equal(GetName(clrType), mdType.AssemblyQualifiedName);
					Assert.Equal(_expectedPublicOrInternalProperties, mdType.Properties.Select(p => p.Name));
				})
		];
		// TestAssembly1 with InternalsVisibleTo
		yield return
		[
			new TestScenario("InternalsVisibleTo Internal Attached Behavior",
				typeof(A1::CompletionEngineTests.Models.InternalAttachedBehavior),
				static (clrType, mdType) => { Assert.Equal(GetName(clrType), mdType?.AssemblyQualifiedName); })
		];
		yield return
		[
			new TestScenario("InternalsVisibleTo Internal Class",
				typeof(A1::CompletionEngineTests.Models.InternalClass),
				static (clrType, mdType) =>
				{
					Assert.Equal(GetName(clrType), mdType?.AssemblyQualifiedName);
					Assert.Equal(_expectedPublicOrInternalProperties, mdType?.Properties.Select(p => p.Name));
				})
		];
		yield return
		[
			new TestScenario("InternalsVisibleTo Public Class with internal properties",
				typeof(A1::CompletionEngineTests.Models.PublicWithInternalPropertiesClass),
				static (clrType, mdType) =>
				{
					Assert.Equal(GetName(clrType), mdType.AssemblyQualifiedName);
					Assert.Equal(_expectedPublicOrInternalProperties, mdType.Properties.Select(p => p.Name));
				})
		];
		// TestAssembly2 without InternalsVisibleTo
		yield return
		[
			new TestScenario("Not InternalsVisibleTo Internal Attached Behavior",
				Type.GetType("CompletionEngineTests.Models.InternalAttachedBehavior, TestAssembly2"),
				static (clrType, mdType) => { Assert.Null(mdType); })
		];
		yield return
		[
			new TestScenario("Not InternalsVisibleTo Internal Class",
				Type.GetType("CompletionEngineTests.Models.InternalAttachedBehavior, TestAssembly2"),
				static (clrType, mdType) => { Assert.Null(mdType); })
		];
		yield return
		[
			new TestScenario("InternalsVisibleTo Public Class with internal properties",
				Type.GetType("CompletionEngineTests.Models.PublicWithInternalPropertiesClass, TestAssembly2"),
				static (clrType, mdType) =>
				{
					Assert.Equal(GetName(clrType), mdType.AssemblyQualifiedName);
					Assert.Equal(_expectedPublicProperties, mdType.Properties.Select(p => p.Name));
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