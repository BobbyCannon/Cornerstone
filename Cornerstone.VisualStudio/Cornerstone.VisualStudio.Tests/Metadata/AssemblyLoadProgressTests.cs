#region References

using System.Collections.Generic;
using Cornerstone.VisualStudio.EditorHost;
using Cornerstone.VisualStudio.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests.Metadata;

[TestClass]
public class AssemblyLoadProgressTests
{
	#region Methods

	[TestMethod]
	public void ParallelReadKeepsTheOldestAssemblyUntilItFinishes()
	{
		var seen = new List<string>();
		AssemblyLoadProgress.Changed += Remember;
		try
		{
			AssemblyLoadProgress.Begin(3, "Reading");
			AssemblyLoadProgress.Report("A.dll", true);
			AssemblyLoadProgress.Report("B.dll", true);
			AssemblyLoadProgress.Report("C.dll", true);
			AssemblyLoadProgress.Report("A.dll", false);
			AssemblyLoadProgress.Report("B.dll", false);
			AssemblyLoadProgress.Report("C.dll", false);
		}
		finally
		{
			AssemblyLoadProgress.Changed -= Remember;
		}

		Assert.AreEqual(3, seen.Count);
		Assert.AreEqual("1 of 3 - Reading metadata of A.dll", seen[0]);
		Assert.AreEqual("2 of 3 - Reading metadata of B.dll", seen[1]);
		Assert.AreEqual("3 of 3 - Reading metadata of C.dll", seen[2]);

		void Remember(MetadataProgressMessage message)
		{
			seen.Add(MetadataProgressText.Format(message.Phase, message.AssemblyName, message.Index, message.Total));
		}
	}

	[TestMethod]
	public void SequentialPhasesNameTheCurrentFile()
	{
		var seen = new List<string>();
		AssemblyLoadProgress.Changed += Remember;
		try
		{
			AssemblyLoadProgress.Begin(2, "Checking");
			AssemblyLoadProgress.ReportAt("One.dll", 1, 2);
			AssemblyLoadProgress.ReportAt("Two.dll", 2, 2);
			AssemblyLoadProgress.Begin(1, "Writing");
			AssemblyLoadProgress.ReportAt("Two.dll", 1, 1);
		}
		finally
		{
			AssemblyLoadProgress.Changed -= Remember;
		}

		Assert.AreEqual(3, seen.Count);
		Assert.AreEqual("1 of 2 - Checking cache of One.dll", seen[0]);
		Assert.AreEqual("2 of 2 - Checking cache of Two.dll", seen[1]);
		Assert.AreEqual("1 of 1 - Writing metadata of Two.dll", seen[2]);

		void Remember(MetadataProgressMessage message)
		{
			seen.Add(MetadataProgressText.Format(message.Phase, message.AssemblyName, message.Index, message.Total));
		}
	}

	[TestMethod]
	public void FormatsConvertPhase()
	{
		Assert.AreEqual(
			"4 of 20 - Converting metadata of Cornerstone.Presentation",
			MetadataProgressText.Format("Converting", "Cornerstone.Presentation", 4, 20));
	}

	#endregion
}