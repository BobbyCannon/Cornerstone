#region References

using System;
using System.Collections.Generic;
using Cornerstone.Avalonia;
using Cornerstone.Compare;
using Cornerstone.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class UpdateableTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void UpdateableShouldUpdateAll()
	{
		var updateCodeGeneratedFiles = EnableFileUpdates || IsDebugging;
		var updateableType = typeof(IUpdateable);
		var assemblies = new[]
		{
			// Cornerstone
			typeof(Babel).Assembly,

			// Cornerstone.Avalonia
			typeof(CornerstoneApplication).Assembly
		};
		var exclusions = new List<Type>();
		var settings = new ComparerSettings { IgnoreMissingProperties = true };

		UpdateableShouldUpdateAll(
			updateCodeGeneratedFiles, updateableType, assemblies, exclusions, settings

			// uncomment to test a single type
			//x => x == typeof(Timer)
		);
	}

	#endregion
}