#region References

using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Data.Times;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class TypeExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToAssemblyName()
	{
		var scenarios = new Dictionary<Type, (string Expected, bool IsSourceReflected)>
		{
			{ typeof(Buffer<int>), ("Cornerstone.Collections.Buffer`1[[System.Int32,System.Private.CoreLib]],Cornerstone", false) },
			{ typeof(ClipboardService), ("Cornerstone.Avalonia.ClipboardService,Cornerstone.Avalonia", true) },

			// Generated below
			{ typeof(CornerstoneTest), ("Cornerstone.Testing.CornerstoneTest,Cornerstone", true) },
			{ typeof(WindowLocation), ("Cornerstone.Presentation.WindowLocation,Cornerstone", true) },
			{ typeof(ReferenceTracker), ("Cornerstone.Compare.ReferenceTracker,Cornerstone", true) },
			{ typeof(TimeUnit), ("Cornerstone.Data.Times.TimeUnit,Cornerstone", true) },
			{ typeof(UpdateableAction), ("Cornerstone.Data.UpdateableAction,Cornerstone", true) }
		};

		StringBuilder builder = null;

		foreach (var lookup in SourceReflector.Lookup)
		{
			if (scenarios.ContainsKey(lookup.Value))
			{
				continue;
			}

			//scenarios.Add(lookup.Value, (lookup.Key, true));
			builder ??= new StringBuilder();
			builder.AppendLine($"{{ typeof({lookup.Value.Name}), (\"{lookup.Key}\", true) }},");
		}

		if (builder != null)
		{
			builder.ToString().Dump();
		}

		foreach (var scenario in scenarios)
		{
			var actual = scenario.Key.ToAssemblyName();
			AreEqual(scenario.Value.Expected, actual);

			if (!scenario.Value.IsSourceReflected)
			{
				continue;
			}

			IsTrue(actual.TryGetType(out var type));
			AreEqual(scenario.Key, type);
		}
	}

	#endregion
}