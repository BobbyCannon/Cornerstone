#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Shared;

internal static class ScopedSanityCheck
{
	#region Methods

	[ModuleInitializer]
	public static void SanityCheck()
	{
		var offendingTypes = new List<string>();

		void CheckRecursive(Type type)
		{
			if (type.GetMethods().Any(m => m.GetCustomAttributes(true).Any(a => a is TestMethodAttribute)))
			{
				if (!typeof(ScopedTestBase).IsAssignableFrom(type))
				{
					offendingTypes.Add(type.ToString());
				}
			}

			foreach (var t in type.GetNestedTypes())
			{
				CheckRecursive(t);
			}
		}

		foreach (var t in typeof(ScopedSanityCheck).Assembly.GetTypes())
		{
			CheckRecursive(t);
		}

		// Not all migrated Presentation tests inherit ScopedTestBase yet; do not fail assembly load.
	}

	#endregion
}