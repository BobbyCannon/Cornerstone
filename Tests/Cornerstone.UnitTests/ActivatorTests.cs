#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Collections;
using Cornerstone.Data.TypeActivators;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Client;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
public class ActivatorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	[SuppressMessage("ReSharper", "ConvertNullableToShortForm")]
	[SuppressMessage("ReSharper", "UseArrayEmptyMethod")]
	public void CreateInstance()
	{
		var scenarios = new List<(string name, object expected, Type type)>
		{
			("0", 0, typeof(int)),
			("1", new List<int>(), typeof(ICollection<int>)),
			("2", new List<int>(), typeof(IEnumerable<int>)),
			("3", new List<int>(), typeof(IList<int>)),
			("4", new List<int>(), typeof(List<int>)),
			("5", new HashSet<int>(), typeof(ISet<int>)),
			("6", new ReadOnlySet<int>(), typeof(ReadOnlySet<int>)),
			("7", new Dictionary<string, int>(), typeof(IDictionary<string, int>)),
			("8", new Collection<int>(), typeof(Collection<int>)),
			("9", Array.Empty<int>(), typeof(int[])),
			("10", Array.Empty<int?>(), typeof(int?[])),
			("11", new Nullable<int>(), typeof(int?)),
			("12", new Nullable<EnumExtensions.EnumDetails>(), typeof(EnumExtensions.EnumDetails?)),
			#if (!NET48)
			("13", new ReadOnlySet<int>(), typeof(IReadOnlySet<int>)),
			#endif
		};

		foreach (var scenario in scenarios)
		{
			$"Scenario {scenario.name}".Dump();
			var actual = scenario.type.CreateInstance();
			AreEqual(scenario.expected, actual);
		}
	}

	[TestMethod]
	public void CreateInstanceOfGeneric()
	{
		var scenarios = new List<(object expected, Type type, Type[] genericTypes)>
		{
			(new List<ClientAccount>(), typeof(IList<>), new[] { typeof(ClientAccount) })
			//	(new PartialUpdate<Account>(), typeof(PartialUpdate<>), new[] { typeof(Account) }),
			//	(new PartialUpdate<PartialUpdate<int>>(), typeof(PartialUpdate<>), new[] { typeof(PartialUpdate<int>) })
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.type.CreateInstanceOfGeneric(scenario.genericTypes);
			AreEqual(scenario.expected, actual);
		}
	}

	[TestMethod]
	public void CreateInstanceOfGenericExpectedExceptions()
	{
		ExpectedException<ArgumentException>(
			() => typeof(ClientAccount).CreateInstanceOfGeneric(),
			"The type provided is not a generic type or is a generic type definition."
		);

		ExpectedException<ArgumentException>(
			() => typeof(ClientAccount).CreateInstanceOfGeneric(1),
			"The type provided is not a generic type or is a generic type definition."
		);
	}

	[TestMethod]
	public void CreateInstanceUsingGenericType()
	{
		AreEqual(new List<ClientAccount>(), Activator.CreateInstance<IList<ClientAccount>>());
		AreEqual(new List<int> { 1, 2, 3 }, Activator.CreateInstance<IList<int>>(new[] { 1, 2, 3 }));
	}

	[TestMethod]
	public void CustomTypeActivator()
	{
		// Should fail because Activator does not know about the interface
		ExpectedException<MissingMethodException>(
			() => Activator.CreateInstance<IActivatorTest>(),
			"+IActivatorTest missing requested constructor."
		);

		// Should work now because we have a custom activator.
		Activator.RegisterTypeActivator(new ActivatorTestActivator());
		var actual = Activator.CreateInstance<IActivatorTest>();
		AreEqual(new ActivatorTest(), actual);

		Activator.ResetTypeActivators();

		// Should fail again after resetting activators
		ExpectedException<MissingMethodException>(
			() => Activator.CreateInstance<IActivatorTest>(),
			"+IActivatorTest missing requested constructor."
		);
	}

	#endregion

	#region Classes

	public class ActivatorTest : IActivatorTest
	{
	}

	public class ActivatorTestActivator : TypeActivator<IActivatorTest, ActivatorTest>
	{
	}

	#endregion

	public interface IActivatorTest
	{
	}
}